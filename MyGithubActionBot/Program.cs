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
        // Placeholder: Replace with logic to fetch changed files from the PR diff
        return new List<string> { "ExampleTestFile.cs", "ExampleCodeFile.cs" };
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
