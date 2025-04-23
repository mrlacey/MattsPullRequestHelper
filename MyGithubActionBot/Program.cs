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

public class Program
{
    public static async Task Main(string[] args)
    {
        var changedFiles = GetChangedFiles();

        var changedFilesMessage = "Changed Files:\n" + string.Join("\n", changedFiles);
        Console.WriteLine(changedFilesMessage);

        // Analyze test methods
        var testAnalysis = AnalyzeTestMethods(changedFiles);
        var testAnalysisMessage = $"Added Tests: {testAnalysis.Added}\nDeleted Tests: {testAnalysis.Deleted}\nChanged Tests: {testAnalysis.Changed}";
        Console.WriteLine(testAnalysisMessage);

        // Analyze deleted public methods
        var deletedPublicMethods = AnalyzeDeletedPublicMethods(changedFiles);
        var deletedMethodsMessage = "Deleted Public Methods:\n" + string.Join("\n", deletedPublicMethods);
        Console.WriteLine(deletedMethodsMessage);

        // Combine messages
        var fullMessage = $"{testAnalysisMessage}\n\n{deletedMethodsMessage}";

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
            dynamic prEvent = JsonConvert.DeserializeObject(eventData);

            if (prEvent.pull_request != null)
            {
                string repository = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");
                string pullRequestNumber = prEvent.pull_request.number;

                if (!string.IsNullOrEmpty(repository) && pullRequestNumber != null)
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
                            foreach (var file in files)
                            {
                                string fileName = file.filename;
                                string patch = file.patch;
                                Console.WriteLine($"Changed file: {fileName}");
                                Console.WriteLine($"Diff: {patch}");

                                if (fileName.EndsWith(".cs"))
                                {
                                    changedFiles.Add(file);
                                }
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

    public static (int Added, int Deleted, int Changed) AnalyzeTestMethods(List<dynamic> changedFiles)
    {
        int added = 0, deleted = 0, changed = 0;

        foreach (var file in changedFiles)
        {
            Console.WriteLine($"Analyzing file: {file.filename}");
            //var lines = File.ReadAllLines(file);
            foreach (var line in file.patch.Split('\n'))
            {
                if (line.StartsWith("+") && line.Contains("[TestMethod]"))
                {
                    added++;
                }
                else if (line.StartsWith("-") && line.Contains("[TestMethod]"))
                {
                    deleted++;
                }
                // else if (line.StartsWith(" ") && line.Contains("[TestMethod]"))
                // {
                //     changed++;
                // }
            }
        }

        return (added, deleted, changed);
    }

    public static List<string> AnalyzeDeletedPublicMethods(List<dynamic> changedFiles)
    {
        var deletedMethods = new List<string>();

        foreach (var file in changedFiles)
        {
            //var lines = File.ReadAllLines(file);

            foreach (var line in file.patch.Split('\n'))
            {
                // Check for deleted public methods in the diff
                if (line.StartsWith("-") && line.Contains("public") && line.Contains("("))
                {
                    var match = Regex.Match(line, @"-\s*public\s+\w+\s+(\w+)\s*\(");
                    if (match.Success)
                    {
                        deletedMethods.Add(match.Groups[1].Value);
                    }
                }
            }
        }

        return deletedMethods;
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

            var contentBody = $"{{\"body\": \"{message.Replace("\n", "<br />")}\"}}";

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

    public static void PlaceholderMethodG()
    {
        // Placeholder for testing deleting public methods
    }

    public static void PlaceholderMethodH()
    {
        // Placeholder for testing deleting public methods
    }

    public static void PlaceholderMethodI()
    {
        // Placeholder for testing deleting public methods
    }

    public static void PlaceholderMethodJ()
    {
        // Placeholder for testing deleting public methods
    }

    public static void PlaceholderMethodK()
    {
        // Placeholder for testing deleting public methods
    }

    public static void PlaceholderMethodL()
    {
        // Placeholder for testing deleting public methods
    }

    public static void PlaceholderMethodM()
    {
        // Placeholder for testing deleting public methods
    }

    public static void PlaceholderMethodN()
    {
        // Placeholder for testing deleting public methods
    }

    public static void PlaceholderMethodO()
    {
        // Placeholder for testing deleting public methods
    }
}
