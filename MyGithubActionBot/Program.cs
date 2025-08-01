﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LibGit2Sharp;
using Newtonsoft.Json;

public class Program
{
	public const string DeletedPublicMethodRegex = @"^\-\s*public\s+(?:(?:static|async|virtual|override|sealed|abstract)\s+)*(?:\w+(?:<[^>]+>)?|\([^)]+\))\s+(\w+)\s*\(";
	public const string PackageReferenceRegex = @"<PackageReference\s+Include=""([^""]+)""\s+Version=""([^""]+)""\s*/?>";
	public const string ProjectReferenceRegex = @"<ProjectReference\s+Include=""([^""]+)""\s*/?>";
	public const string FrameworkReferenceRegex = @"<FrameworkReference\s+Include=""([^""]+)""\s*/?>";
	public const string ReferenceRegex = @"<Reference\s+Include=""([^""]+)""\s*(?:Version=""([^""]*)"")?\s*/?>";

	public static async Task Main(string[] args)
	{
		var changedFiles = GetChangedFiles();

		var changedFilesMessage = "Changed Files:\n" + string.Join("\n", changedFiles);
		Console.WriteLine(changedFilesMessage);

		// Analyze test methods
		var testAnalysis = AnalyzeTestMethods(changedFiles);
		var testAnalysisMessage = $"Added Tests: {testAnalysis.Added}\nDeleted Tests: {testAnalysis.Deleted}";
		Console.WriteLine(testAnalysisMessage);

		// Analyze deleted public methods
		var deletedPublicMethods = AnalyzeDeletedPublicMethods(changedFiles);
		var deletedMethodsMessage = "Deleted Public Methods:\n" + string.Join("\n", deletedPublicMethods);
		Console.WriteLine(deletedMethodsMessage);

		// Analyze project references
		var referenceAnalysis = AnalyzeProjectReferences(changedFiles);
		var referenceAnalysisMessage = FormatReferenceAnalysis(referenceAnalysis);
		Console.WriteLine(referenceAnalysisMessage);

		// Combine messages
		var fullMessage = $"{testAnalysisMessage}\n\n{deletedMethodsMessage}\n\n{referenceAnalysisMessage}";

		// Post to PR conversation
		await PostToPullRequest(fullMessage);
	}

	public static List<dynamic> GetChangedFiles()
	{
		string workspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE") ?? "";
		var changedFiles = new List<dynamic>();

		Console.WriteLine($"Workspace directory: {workspace}");

		try
		{
			string prFilesJson = Environment.GetEnvironmentVariable("GITHUB_EVENT_PATH") ?? string.Empty;
			if (string.IsNullOrWhiteSpace(prFilesJson))
			{
				Console.WriteLine("GITHUB_EVENT_PATH is not set or empty.");
				return changedFiles;
			}

			var eventData = File.ReadAllText(prFilesJson);
			dynamic? prEvent = JsonConvert.DeserializeObject(eventData);

			if (prEvent?.pull_request != null)
			{
				string? repository = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");
				string? pullRequestNumber = prEvent.pull_request.number?.ToString();

				if (!string.IsNullOrEmpty(repository) && !string.IsNullOrEmpty(pullRequestNumber))
				{
					string filesUrl = $"https://api.github.com/repos/{repository}/pulls/{pullRequestNumber}/files";
					Console.WriteLine($"Pull request files URL: {filesUrl}");

					using (var client = new HttpClient())
					{
						client.DefaultRequestHeaders.Add("Authorization", $"Bearer {Environment.GetEnvironmentVariable("GITHUB_TOKEN")}");
						client.DefaultRequestHeaders.Add("User-Agent", "MattsPullRequestHelper");

						var response = client.GetAsync(filesUrl).Result;
						if (response.IsSuccessStatusCode)
						{
							var files = JsonConvert.DeserializeObject<List<dynamic>>(response.Content.ReadAsStringAsync().Result);

							if (files is not null)
							{
								foreach (var file in files)
								{
									string fileName = file.filename?.ToString() ?? string.Empty;
									Console.WriteLine($"Changed file: {fileName}");

									if (fileName.EndsWith(".cs") || fileName.EndsWith(".csproj"))
									{
										changedFiles.Add(file);
									}
								}
							}
							else
							{
								Console.WriteLine("No files found in the pull request.");
							}
						}
						else
						{
							Console.WriteLine($"Failed to fetch pull request files. Status: {response.StatusCode}, Message: {response.Content.ReadAsStringAsync().Result}");
						}
					}
				}
				else
				{
					Console.WriteLine("Repository or pull request number is missing.");
				}
			}
			else
			{
				Console.WriteLine("Pull request data not found in the event payload.");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error fetching changed files: {ex.Message}");
		}

		return changedFiles;
	}

	public static (int Added, int Deleted) AnalyzeTestLines(string[] lines)
	{
		int added = 0, deleted = 0;

		if (lines is not null)
		{
			foreach (var line in lines)
			{
				if (line is null) continue;

				if (line.TrimStart('+', '-', ' ', '\t').StartsWith("//")) continue; // Ignore commented lines

				List<string> testAttributes = new List<string> { "[TestMethod]", "[DataRow(", "[Fact]", "[InlineData(", "[Test]", "[TestCase(" };

				if (testAttributes.Any(attr => line.Contains(attr)))
				{
					if (line.StartsWith("+"))
					{
						Console.WriteLine($"Added test method: {line}");
						added++;
					}
					else if (line.StartsWith("-"))
					{
						Console.WriteLine($"Deleted test method: {line}");
						deleted++;
					}
				}
			}
		}

		return (added, deleted);
	}

	public static (int Added, int Deleted) AnalyzeTestMethods(List<dynamic> changedFiles)
	{
		int totalAdded = 0, totalDeleted = 0;

		foreach (var file in changedFiles)
		{
			string filename = file.filename?.ToString() ?? string.Empty;

			// Only analyze C# files for test methods
			if (!filename.EndsWith(".cs"))
				continue;

			Console.WriteLine($"Analyzing file: {filename}");

			var patch = file.patch?.ToString() ?? string.Empty; // Handle possible null
			var lines = patch.Split('\n');
			var result = AnalyzeTestLines(lines); // Use explicit method call
			totalAdded += result.Item1;
			totalDeleted += result.Item2;
		}

		return (totalAdded, totalDeleted);
	}

	public static List<string> AnalyzeDeletedPublicMethods(List<dynamic> changedFiles)
	{
		var deletedMethods = new List<string>();

		foreach (var file in changedFiles)
		{
			var diff = file.patch?.ToString() ?? string.Empty;
			var filename = file.filename?.ToString() ?? string.Empty;

			// Only analyze C# files for deleted public methods
			if (!filename.EndsWith(".cs"))
				continue;

			Console.WriteLine($"Analyzing diff for file: {filename}");

			foreach (var line in diff.Split('\n'))
			{
				if (line.StartsWith("-") && line.Contains("public") && line.Contains("("))
				{
					var match = Regex.Match(line, DeletedPublicMethodRegex);
					if (match.Success)
					{
						deletedMethods.Add($"- {filename} : {match.Groups[1].Value}");
					}
				}
			}
		}

		if (deletedMethods.Count == 0)
		{
			deletedMethods.Add("* none *");
		}

		return deletedMethods;
	}

	public static ReferenceAnalysisResult AnalyzeProjectReferences(List<dynamic> changedFiles)
	{
		var result = new ReferenceAnalysisResult();
		var referencesByKey = new Dictionary<string, ReferenceChange>();

		foreach (var file in changedFiles)
		{
			string filename = file.filename?.ToString() ?? string.Empty;

			if (!filename.EndsWith(".csproj"))
				continue;

			Console.WriteLine($"Analyzing project references in file: {filename}");

			var patch = file.patch?.ToString() ?? string.Empty;
			var lines = patch.Split('\n');

			foreach (var line in lines)
			{
				if (line.StartsWith("+") || line.StartsWith("-"))
				{
					var trimmedLine = line.Substring(1).Trim();
					ProcessReferenceLine(trimmedLine, line.StartsWith("+"), referencesByKey);
				}
			}
		}

		result.Changes = referencesByKey.Values.ToList();
		return result;
	}

	private static void ProcessReferenceLine(string line, bool isAddition, Dictionary<string, ReferenceChange> referencesByKey)
	{
		// Try PackageReference first
		var packageMatch = Regex.Match(line, PackageReferenceRegex);
		if (packageMatch.Success)
		{
			ProcessReferenceMatch(packageMatch.Groups[1].Value, packageMatch.Groups[2].Value, "PackageReference", isAddition, referencesByKey);
			return;
		}

		// Try ProjectReference
		var projectMatch = Regex.Match(line, ProjectReferenceRegex);
		if (projectMatch.Success)
		{
			ProcessReferenceMatch(projectMatch.Groups[1].Value, "", "ProjectReference", isAddition, referencesByKey);
			return;
		}

		// Try FrameworkReference
		var frameworkMatch = Regex.Match(line, FrameworkReferenceRegex);
		if (frameworkMatch.Success)
		{
			ProcessReferenceMatch(frameworkMatch.Groups[1].Value, "", "FrameworkReference", isAddition, referencesByKey);
			return;
		}

		// Try Reference
		var referenceMatch = Regex.Match(line, ReferenceRegex);
		if (referenceMatch.Success)
		{
			string version = referenceMatch.Groups.Count > 2 ? referenceMatch.Groups[2].Value : "";
			ProcessReferenceMatch(referenceMatch.Groups[1].Value, version, "Reference", isAddition, referencesByKey);
			return;
		}
	}

	private static void ProcessReferenceMatch(string name, string version, string type, bool isAddition, Dictionary<string, ReferenceChange> referencesByKey)
	{
		var key = $"{type}:{name}";

		// For references without version (ProjectReference, FrameworkReference), use a special marker
		if (string.IsNullOrEmpty(version) && (type == "ProjectReference" || type == "FrameworkReference"))
		{
			version = "[NoVersion]";
		}

		if (isAddition)
		{
			// Reference added or updated
			if (referencesByKey.ContainsKey(key))
			{
				// This is an update - we already saw the removal
				referencesByKey[key].NewVersion = version;
			}
			else
			{
				// This is a new reference
				referencesByKey[key] = new ReferenceChange
				{
					Name = name,
					NewVersion = version,
					Type = type
				};
			}
		}
		else
		{
			// Reference removed or being updated
			if (referencesByKey.ContainsKey(key))
			{
				// This is an update - we already saw the addition
				referencesByKey[key].OldVersion = version;
			}
			else
			{
				// This is a removal
				referencesByKey[key] = new ReferenceChange
				{
					Name = name,
					OldVersion = version,
					NewVersion = "", // Empty indicates removal
					Type = type
				};
			}
		}
	}

	public static string FormatReferenceAnalysis(ReferenceAnalysisResult analysis)
	{
		if (analysis.Changes.Count == 0)
		{
			return $"Project References:{Environment.NewLine}* no reference changes detected *";
		}

		var lines = new List<string> { "Project References:" };

		var newReferences = analysis.Changes.Where(c => c.IsNew).ToList();
		var updatedReferences = analysis.Changes.Where(c => c.IsUpdated).ToList();
		var removedReferences = analysis.Changes.Where(c => c.IsRemoved).ToList();

		if (newReferences.Count > 0)
		{
			lines.Add("New references:");
			foreach (var reference in newReferences)
			{
				var version = !string.IsNullOrEmpty(reference.NewVersion) && reference.NewVersion != "[NoVersion]" ? $" (version {reference.NewVersion})" : "";
				lines.Add($"- {reference.Name}{version} [{reference.Type}]");
			}
		}

		if (updatedReferences.Count > 0)
		{
			if (newReferences.Count > 0)
				lines.Add("");
			lines.Add("Updated references:");
			foreach (var reference in updatedReferences)
			{
				var oldVer = reference.OldVersion == "[NoVersion]" ? "no version" : reference.OldVersion;
				var newVer = reference.NewVersion == "[NoVersion]" ? "no version" : reference.NewVersion;
				lines.Add($"- {reference.Name} (version {oldVer} -> {newVer}) [{reference.Type}]");
			}
		}

		if (removedReferences.Count > 0)
		{
			if (newReferences.Count > 0 || updatedReferences.Count > 0)
				lines.Add("");
			lines.Add("Removed references:");
			foreach (var reference in removedReferences)
			{
				var version = !string.IsNullOrEmpty(reference.OldVersion) && reference.OldVersion != "[NoVersion]" ? $" (version {reference.OldVersion})" : "";
				lines.Add($"- {reference.Name}{version} [{reference.Type}]");
			}
		}

		if (newReferences.Count == 0 && updatedReferences.Count == 0 && removedReferences.Count == 0)
		{
			lines.Clear();
			lines.Add($"Project References:{Environment.NewLine}* no reference changes detected *");
		}

		return string.Join(Environment.NewLine, lines);
	}

	public static async Task PostToPullRequest(string message)
	{
		var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
		var repository = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");
		var pullRequestNumber = Environment.GetEnvironmentVariable("GITHUB_PULL_REQUEST_NUMBER");

		var missingVariables = new List<string>();
		if (string.IsNullOrEmpty(githubToken)) missingVariables.Add("GITHUB_TOKEN");
		if (string.IsNullOrEmpty(repository)) missingVariables.Add("GITHUB_REPOSITORY");
		if (string.IsNullOrEmpty(pullRequestNumber)) missingVariables.Add("GITHUB_PULL_REQUEST_NUMBER");

		if (missingVariables.Any())
		{
			Console.WriteLine($"Missing required environment variables: {string.Join(", ", missingVariables)}");
			return;
		}

		var apiUrl = $"https://api.github.com/repos/{repository}/issues/{pullRequestNumber}/comments";

		using (var client = new HttpClient())
		{
			client.DefaultRequestHeaders.Add("Authorization", $"Bearer {githubToken}");
			client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
			client.DefaultRequestHeaders.Add("User-Agent", "MattsPullRequestHelper");

			var contentBody = $"{{\"body\": \"<b>PullRequestHelper:</b><br /><br />{message.Replace("\n", "<br />")}\"}}";

			var content = new StringContent(contentBody, Encoding.UTF8, "application/json");

			var response = await client.PostAsync(apiUrl, content);
			if (response.IsSuccessStatusCode)
			{
				Console.WriteLine("Successfully posted to PR conversation.");
			}
			else
			{
				Console.WriteLine($"Failed to post to PR conversation. Status: {response.StatusCode}, Message: {await response.Content.ReadAsStringAsync()}");
			}
		}
	}

	public static void PlaceholderMethodA()
	{
		// Placeholder for testing deleting public methods
	}

	public static void PlaceholderMethodB()
	{
		// Placeholder for testing deleting public methods
	}

	public static void PlaceholderMethodC()
	{
		// Placeholder for testing deleting public methods
	}

	public static void PlaceholderMethodD()
	{
		// Placeholder for testing deleting public methods
	}

	public static void PlaceholderMethodE()
	{
		// Placeholder for testing deleting public methods
	}

	public static void PlaceholderMethodF()
	{
		// Placeholder for testing deleting public methods
	}
}
