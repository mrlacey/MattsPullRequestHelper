using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LibGit2Sharp;

public class Program
{
    public static async Task Main(string[] args)
    {
        var changedFiles = GetChangedFiles();

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

    public static List<string> GetChangedFiles()
    {
        string workspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE") ?? "";
        var changedFiles = new List<string>();

        try
        {
            using (var repo = new Repository(workspace))
            {
                var headCommit = repo.Head.Tip;
                var parentCommit = headCommit.Parents.FirstOrDefault();

                if (parentCommit != null)
                {
                    var diff = repo.Diff.Compare<TreeChanges>(parentCommit.Tree, headCommit.Tree);
                    foreach (var change in diff)
                    {
                        if (change.Path.EndsWith(".cs"))
                        {
                            changedFiles.Add(change.Path);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching changed files: {ex.Message}");
        }

        return changedFiles;
    }

    public static (int Added, int Deleted, int Changed) AnalyzeTestMethods(List<string> changedFiles)
    {
        int added = 0, deleted = 0, changed = 0;

        foreach (var file in changedFiles.Where(f => f.EndsWith(".cs")))
        {
            Console.WriteLine($"Analyzing file: {file}");
            var lines = File.ReadAllLines(file);
            foreach (var line in lines)
            {
                Console.WriteLine($"Line: {line}");
                if (line.Contains("[TestMethod]"))
                {
                    added++;
                }
            }
        }

        return (added, deleted, changed);
    }

    public static List<string> AnalyzeDeletedPublicMethods(List<string> changedFiles)
    {
        var deletedMethods = new List<string>();

        foreach (var file in changedFiles.Where(f => f.EndsWith(".cs")))
        {
            var lines = File.ReadAllLines(file);
            foreach (var line in lines)
            {
                if (line.Contains("public") && line.Contains("-"))
                {
                    var match = Regex.Match(line, @"public\s+\w+\s+(\w+)\s*\(");
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
            client.DefaultRequestHeaders.Add("User-Agent", "MattsPullRequestHelper");

            var content = new StringContent($"{{\"body\": \"{message}\"}}", Encoding.UTF8, "application/json");

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

    public static void PlaceholderMethod1()
    {
        // Placeholder for testing deleting public methods
    }

    public static void PlaceholderMethod2()
    {
        // Placeholder for testing deleting public methods
    }

    public static void PlaceholderMethod3()
    {
        // Placeholder for testing deleting public methods
    }

    public static void PlaceholderMethod4()
    {
        // Placeholder for testing deleting public methods
    }

    public static void PlaceholderMethod5()
    {
        // Placeholder for testing deleting public methods
    }

    public static void PlaceholderMethod6()
    {
        // Placeholder for testing deleting public methods
    }

    public static void PlaceholderMethod7()
    {
        // Placeholder for testing deleting public methods
    }

    public static void PlaceholderMethod8()
    {
        // Placeholder for testing deleting public methods
    }

    public static void PlaceholderMethod9()
    {
        // Placeholder for testing deleting public methods
    }

    public static void PlaceholderMethod10()
    {
        // Placeholder for testing deleting public methods
    }
}
