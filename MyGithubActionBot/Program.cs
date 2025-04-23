using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

class Program
{
    static void Main(string[] args)
    {
        // Simulate receiving a list of changed files in a PR
        var changedFiles = GetChangedFiles();

        // Analyze test methods
        var testAnalysis = AnalyzeTestMethods(changedFiles);
        Console.WriteLine($"Added Tests: {testAnalysis.Added}");
        Console.WriteLine($"Deleted Tests: {testAnalysis.Deleted}");
        Console.WriteLine($"Changed Tests: {testAnalysis.Changed}");

        // Analyze deleted public methods
        var deletedPublicMethods = AnalyzeDeletedPublicMethods(changedFiles);
        Console.WriteLine("Deleted Public Methods:");
        foreach (var method in deletedPublicMethods)
        {
            Console.WriteLine(method);
        }
    }

    static List<string> GetChangedFiles()
    {
        // Use environment variables to get the GitHub workspace and PR diff
        string workspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE") ?? "";
        string diffFilePath = Path.Combine(workspace, "pr_diff.txt");

        // Simulate fetching the diff (replace this with actual Git commands in production)
        if (!File.Exists(diffFilePath))
        {
            Console.WriteLine("Error: PR diff file not found.");
            return new List<string>();
        }

        var changedFiles = new List<string>();
        var diffLines = File.ReadAllLines(diffFilePath);

        foreach (var line in diffLines)
        {
            if (line.StartsWith("diff --git a/") && line.EndsWith(".cs"))
            {
                var filePath = line.Split(' ')[2].Substring(2); // Extract file path
                changedFiles.Add(filePath);
            }
        }

        return changedFiles;
    }

    static (int Added, int Deleted, int Changed) AnalyzeTestMethods(List<string> changedFiles)
    {
        int added = 0, deleted = 0, changed = 0;

        foreach (var file in changedFiles.Where(f => f.EndsWith(".cs")))
        {
            var lines = File.ReadAllLines(file);
            foreach (var line in lines)
            {
                if (line.Contains("[TestMethod]") && line.Contains("+")) added++;
                else if (line.Contains("[TestMethod]") && line.Contains("-")) deleted++;
                else if (line.Contains("[TestMethod]") && line.Contains("~")) changed++;
            }
        }

        return (added, deleted, changed);
    }

    static List<string> AnalyzeDeletedPublicMethods(List<string> changedFiles)
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
}
