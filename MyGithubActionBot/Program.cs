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

public class PackageReference
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}

public class PackageReferenceChange
{
    public string Name { get; set; } = string.Empty;
    public string? OldVersion { get; set; }
    public string NewVersion { get; set; } = string.Empty;
    public bool IsNew => string.IsNullOrEmpty(OldVersion);
}

public class ReferenceAnalysisResult
{
    public List<PackageReferenceChange> Changes { get; set; } = new List<PackageReferenceChange>();
}

public class Program
{
    public const string DeletedPublicMethodRegex = @"^\-\s*public\s+(?:(?:static|async|virtual|override|sealed|abstract)\s+)*(?:\w+(?:<[^>]+>)?|\([^)]+\))\s+(\w+)\s*\(";
    public const string PackageReferenceRegex = @"<PackageReference\s+Include=""([^""]+)""\s+Version=""([^""]+)""\s*/?>";

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
        var packagesByName = new Dictionary<string, PackageReferenceChange>();

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
                    var match = Regex.Match(trimmedLine, PackageReferenceRegex);
                    
                    if (match.Success)
                    {
                        var packageName = match.Groups[1].Value;
                        var version = match.Groups[2].Value;
                        
                        if (line.StartsWith("+"))
                        {
                            // Package reference added or updated
                            if (packagesByName.ContainsKey(packageName))
                            {
                                // This is an update - we already saw the removal
                                packagesByName[packageName].NewVersion = version;
                            }
                            else
                            {
                                // This is a new package
                                packagesByName[packageName] = new PackageReferenceChange
                                {
                                    Name = packageName,
                                    NewVersion = version
                                };
                            }
                        }
                        else if (line.StartsWith("-"))
                        {
                            // Package reference removed or being updated
                            if (packagesByName.ContainsKey(packageName))
                            {
                                // This is an update - we already saw the addition
                                packagesByName[packageName].OldVersion = version;
                            }
                            else
                            {
                                // This might be part of an update, create entry
                                packagesByName[packageName] = new PackageReferenceChange
                                {
                                    Name = packageName,
                                    OldVersion = version,
                                    NewVersion = "" // Will be filled if we see the + line
                                };
                            }
                        }
                    }
                }
            }
        }

        // Filter out removals (where we only saw - but no +)
        result.Changes = packagesByName.Values
            .Where(change => !string.IsNullOrEmpty(change.NewVersion))
            .ToList();

        return result;
    }

    public static string FormatReferenceAnalysis(ReferenceAnalysisResult analysis)
    {
        if (analysis.Changes.Count == 0)
        {
            return "Project References:\n* no new references added *";
        }

        var lines = new List<string> { "Project References:" };

        var newReferences = analysis.Changes.Where(c => c.IsNew).ToList();
        var updatedReferences = analysis.Changes.Where(c => !c.IsNew).ToList();

        if (newReferences.Count > 0)
        {
            lines.Add("New references:");
            foreach (var reference in newReferences)
            {
                lines.Add($"- {reference.Name} (version {reference.NewVersion})");
            }
        }

        if (updatedReferences.Count > 0)
        {
            if (newReferences.Count > 0)
                lines.Add("");
            lines.Add("Updated references:");
            foreach (var reference in updatedReferences)
            {
                lines.Add($"- {reference.Name} (version {reference.OldVersion} -> {reference.NewVersion})");
            }
        }

        if (newReferences.Count == 0 && updatedReferences.Count == 0)
        {
            lines.Clear();
            lines.Add("Project References:\n* no new references added *");
        }

        return string.Join("\n", lines);
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
