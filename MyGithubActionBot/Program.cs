using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LibGit2Sharp;

public class Program
{
    public static void Main(string[] args)
    {
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
}
