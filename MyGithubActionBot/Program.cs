using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LibGit2Sharp;
using Newtonsoft.Json;
using PullRequestHelper.Core;
using PullRequestHelper.Core.Models;

public class Program
{
	public const string DeletedPublicMethodRegex = PullRequestAnalyzer.DeletedPublicMethodRegex;
	public const string PackageReferenceRegex = PullRequestAnalyzer.PackageReferenceRegex;
	public const string ProjectReferenceRegex = PullRequestAnalyzer.ProjectReferenceRegex;
	public const string FrameworkReferenceRegex = PullRequestAnalyzer.FrameworkReferenceRegex;
	public const string ReferenceRegex = PullRequestAnalyzer.ReferenceRegex;

	public static async Task Main(string[] args)
	{
		var analyzer = new PullRequestAnalyzer();
		var changedFiles = GetChangedFiles();

		var changedFilesMessage = "Changed Files:\n" + string.Join("\n", changedFiles);
		Console.WriteLine(changedFilesMessage);

		// Analyze test methods
		var testAnalysis = analyzer.AnalyzeTestMethods(changedFiles);
		var testAnalysisMessage = $"Added Tests: {testAnalysis.Added}\nDeleted Tests: {testAnalysis.Deleted}";
		Console.WriteLine(testAnalysisMessage);

		// Analyze deleted public methods
		var deletedPublicMethods = analyzer.AnalyzeDeletedPublicMethods(changedFiles);
		var deletedMethodsMessage = "Deleted Public Methods:\n" + string.Join("\n", deletedPublicMethods);
		Console.WriteLine(deletedMethodsMessage);

		// Analyze project references
		var referenceAnalysis = analyzer.AnalyzeProjectReferences(changedFiles);
		var referenceAnalysisMessage = analyzer.FormatReferenceAnalysis(referenceAnalysis);
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
							Environment.Exit(2); // This will cause the GitHub Action to fail
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
		var analyzer = new PullRequestAnalyzer();
		return analyzer.AnalyzeTestLines(lines);
	}

	public static (int Added, int Deleted) AnalyzeTestMethods(List<dynamic> changedFiles)
	{
		var analyzer = new PullRequestAnalyzer();
		return analyzer.AnalyzeTestMethods(changedFiles);
	}

	public static List<string> AnalyzeDeletedPublicMethods(List<dynamic> changedFiles)
	{
		var analyzer = new PullRequestAnalyzer();
		return analyzer.AnalyzeDeletedPublicMethods(changedFiles);
	}

	public static ReferenceAnalysisResult AnalyzeProjectReferences(List<dynamic> changedFiles)
	{
		var analyzer = new PullRequestAnalyzer();
		return analyzer.AnalyzeProjectReferences(changedFiles);
	}

	public static string FormatReferenceAnalysis(ReferenceAnalysisResult analysis)
	{
		var analyzer = new PullRequestAnalyzer();
		return analyzer.FormatReferenceAnalysis(analysis);
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
			Environment.Exit(3);
			return;
		}

		try
		{
			var githubService = new GitHubService();
			await githubService.PostToPullRequestAsync(repository!, int.Parse(pullRequestNumber!), message, githubToken!);
			Console.WriteLine("Successfully posted to PR conversation.");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Failed to post to PR conversation: {ex.Message}");
			Environment.Exit(1);
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
}
