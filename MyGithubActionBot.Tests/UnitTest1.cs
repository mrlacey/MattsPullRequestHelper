using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Xunit;

namespace MyGithubActionBot.Tests
{
    public class BotTests
    {
        // TODO: Review this test as it currently seems pointless
        [Fact]
        public void Test_GetChangedFiles_ReturnsExpectedFiles()
        {
            // Arrange
            Environment.SetEnvironmentVariable("GITHUB_WORKSPACE", "path/to/repo");
            // Simulate repository setup and commits here if needed

            // Act
            var changedFiles = Program.GetChangedFiles();

            // Assert
            Assert.NotNull(changedFiles);
            Assert.All(changedFiles, file => Assert.EndsWith(".cs", file));
        }

        //[Fact]
        // public void Test_AnalyzeTestMethods_CorrectlyCountsTests()
        // {
        //     // Arrange
        //     var tempFile = Path.Combine(Path.GetTempPath(), "TestFile1.cs");
        //     File.WriteAllText(tempFile, "[TestMethod] public void Test1() { }");
        //     var changedFiles = new List<string> { tempFile };

        //     // Act
        //     var result = Program.AnalyzeTestMethods(changedFiles);

        //     // Assert
        //     Assert.Equal(1, result.Added);
        //     Assert.Equal(0, result.Deleted);
        //     Assert.Equal(0, result.Changed);

        //     // Cleanup
        //     File.Delete(tempFile);
        // }

        //[Fact]
        // public void Test_AnalyzeDeletedPublicMethods_IdentifiesDeletedMethods()
        // {
        //     // Arrange
        //     var tempFile = Path.Combine(Path.GetTempPath(), "CodeFile1.cs");
        //     File.WriteAllText(tempFile, "- public void DeletedMethod() { }");
        //     var changedFiles = new List<string> { tempFile };

        //     // Act
        //     var deletedMethods = Program.AnalyzeDeletedPublicMethods(changedFiles);

        //     // Assert
        //     Assert.Single(deletedMethods);
        //     Assert.Equal("DeletedMethod", deletedMethods[0]);

        //     // Cleanup
        //     File.Delete(tempFile);
        // }

        [Theory]
        [InlineData("- public void MyMethod()", true, "MyMethod")]
        [InlineData("- public static void MyMethod2()", true, "MyMethod2")]
        [InlineData("- public async void MyMethod3()", true, "MyMethod3")]
        [InlineData("- public static async MyMethod4()", true, "MyMethod4")]
        [InlineData("- public static virtual async MyMethod5()", true, "MyMethod5")]
        [InlineData("- public List<string> GetStrings(List<dynamic> changedFiles)", true, "GetStrings")]
        [InlineData("- public static (int Added, int Deleted, int Changed) AnalyzeTestMethods(List<dynamic> changedFiles)", true, "AnalyzeTestMethods")]
        [InlineData("public void MyMethod()", false, "")]
        [InlineData("    void MyMethod()", false, "")]
        [InlineData("    private MyMethod()", false, "")]
        [InlineData("+ public void MyMethod()", false, "")]
        [InlineData("+ public static void MyMethod2()", false, "")]
        [InlineData("+ public async void MyMethod3()", false, "")]
        [InlineData("+ public static async MyMethod4()", false, "")]
        [InlineData("- private void MyMethod()", false, "")]
        [InlineData("- private static void MyMethod2()", false, "")]
        [InlineData("- private async void MyMethod3()", false, "")]
        [InlineData("- private static async MyMethod4()", false, "")]
        [InlineData("- protected void MyMethod()", false, "")]
        [InlineData("- protected static void MyMethod2()", false, "")]
        [InlineData("- protected async void MyMethod3()", false, "")]
        [InlineData("- protected static async MyMethod4()", false, "")]
        [InlineData("  - public void MyMethod()", false, "")]
        [InlineData(" - public static void MyMethod2()", false, "")]
        [InlineData("   - public async void MyMethod3()", false, "")]
        [InlineData("  - public static async MyMethod4()", false, "")]
        public void TestDeletedPublicMethodRegex_ValidValues(string input, bool isMatchExpected, string expectedMethodName)
        {
            var regex = new Regex(Program.DeletedPublicMethodRegex);

            var match = regex.Match(input);

            Assert.Equal(isMatchExpected, match.Success);

            Assert.Equal(expectedMethodName, match.Groups[1].Value);
        }

        [Fact]
        public void AnalyzeTestLines_ShouldCorrectlyCountAddedAndDeletedTests()
        {
            string[] linesAddingMethods = [
            
                "+ [TestMethod] public void AddedTest() { }",
                "+ [TestMethod]",
                "+\t[TestMethod]",
            ];
            string[] linesDeletingMethods = [
            
                "- [TestMethod] public void DeletedTest() { }",
                "- [TestMethod]",
            ];
            string[] miscOtherLine = [
            
                "+ // [TestMethod] public void CommentedOutAddedTest() { }",
                "- // [TestMethod] public void CommentedOutDeletedTest() { }",
                "+ public void NonTestMethod() { }",
                "- public void AnotherNonTestMethod() { }"
            ];

            var allLines = linesAddingMethods.Concat(linesDeletingMethods).Concat(miscOtherLine).ToArray();

            var result = Program.AnalyzeTestLines(allLines);

            Assert.Equal(linesAddingMethods.Length, result.Item1); // Added
            Assert.Equal(linesDeletingMethods.Length, result.Item2); // Deleted
        }

        [Theory]
        [InlineData(@"<PackageReference Include=""Newtonsoft.Json"" Version=""13.0.3"" />", true, "Newtonsoft.Json", "13.0.3")]
        [InlineData(@"<PackageReference Include=""Microsoft.Extensions.Logging"" Version=""8.0.0"" />", true, "Microsoft.Extensions.Logging", "8.0.0")]
        [InlineData(@"<PackageReference Include=""xunit"" Version=""2.9.2"" />", true, "xunit", "2.9.2")]
        [InlineData(@"<PackageReference Include=""LibGit2Sharp"" Version=""0.31.0""/>", true, "LibGit2Sharp", "0.31.0")] // Without space before />
        [InlineData(@"<ProjectReference Include=""..\Other\Other.csproj"" />", false, "", "")]
        [InlineData(@"<Reference Include=""System"" />", false, "", "")]
        public void TestPackageReferenceRegex_ValidValues(string input, bool isMatchExpected, string expectedPackageName, string expectedVersion)
        {
            var regex = new Regex(Program.PackageReferenceRegex);

            var match = regex.Match(input);

            Assert.Equal(isMatchExpected, match.Success);
            if (isMatchExpected)
            {
                Assert.Equal(expectedPackageName, match.Groups[1].Value);
                Assert.Equal(expectedVersion, match.Groups[2].Value);
            }
        }

        [Fact]
        public void AnalyzeProjectReferences_ShouldDetectNewPackageReferences()
        {
            // Create mock changed file with new package reference
            var mockFileJson = @"{
                ""filename"": ""TestProject.csproj"",
                ""patch"": ""@@ -10,6 +10,7 @@\n   <ItemGroup>\n     <PackageReference Include=\""LibGit2Sharp\"" Version=\""0.31.0\"" />\n     <PackageReference Include=\""Newtonsoft.Json\"" Version=\""13.0.3\"" />\n+    <PackageReference Include=\""Microsoft.Extensions.Logging\"" Version=\""8.0.0\"" />\n   </ItemGroup>\n\n </Project>""
            }";

            var mockFile = JsonConvert.DeserializeObject(mockFileJson);
            var changedFiles = new List<dynamic> { mockFile };

            var result = Program.AnalyzeProjectReferences(changedFiles);

            Assert.Single(result.Changes);
            Assert.Equal("Microsoft.Extensions.Logging", result.Changes[0].Name);
            Assert.Equal("8.0.0", result.Changes[0].NewVersion);
            Assert.True(result.Changes[0].IsNew);
        }

        [Fact]
        public void AnalyzeProjectReferences_ShouldDetectUpdatedPackageReferences()
        {
            // Create mock changed file with updated package reference
            var mockFileJson = @"{
                ""filename"": ""TestProject.csproj"",
                ""patch"": ""@@ -10,7 +10,7 @@\n   <ItemGroup>\n     <PackageReference Include=\""LibGit2Sharp\"" Version=\""0.31.0\"" />\n-    <PackageReference Include=\""Newtonsoft.Json\"" Version=\""13.0.2\"" />\n+    <PackageReference Include=\""Newtonsoft.Json\"" Version=\""13.0.3\"" />\n   </ItemGroup>\n\n </Project>""
            }";

            var mockFile = JsonConvert.DeserializeObject(mockFileJson);
            var changedFiles = new List<dynamic> { mockFile };

            var result = Program.AnalyzeProjectReferences(changedFiles);

            Assert.Single(result.Changes);
            Assert.Equal("Newtonsoft.Json", result.Changes[0].Name);
            Assert.Equal("13.0.2", result.Changes[0].OldVersion);
            Assert.Equal("13.0.3", result.Changes[0].NewVersion);
            Assert.False(result.Changes[0].IsNew);
        }

        [Fact]
        public void AnalyzeProjectReferences_ShouldIgnoreNonCsprojFiles()
        {
            // Create mock changed file that's not a .csproj file
            var mockFileJson = @"{
                ""filename"": ""TestClass.cs"",
                ""patch"": ""@@ -1,4 +1,5 @@\n using System;\n+using Microsoft.Extensions.Logging;\n\n public class TestClass\n {""
            }";

            var mockFile = JsonConvert.DeserializeObject(mockFileJson);
            var changedFiles = new List<dynamic> { mockFile };

            var result = Program.AnalyzeProjectReferences(changedFiles);

            Assert.Empty(result.Changes);
        }

        [Fact]
        public void FormatReferenceAnalysis_ShouldReturnNoReferencesMessage_WhenEmpty()
        {
            var analysis = new ReferenceAnalysisResult();

            var result = Program.FormatReferenceAnalysis(analysis);

            Assert.Equal("Project References:\n* no new references added *", result);
        }

        [Fact]
        public void FormatReferenceAnalysis_ShouldFormatNewReferences()
        {
            var analysis = new ReferenceAnalysisResult
            {
                Changes = new List<PackageReferenceChange>
                {
                    new PackageReferenceChange
                    {
                        Name = "Microsoft.Extensions.Logging",
                        NewVersion = "8.0.0"
                    }
                }
            };

            var result = Program.FormatReferenceAnalysis(analysis);

            var expected = @"Project References:
New references:
- Microsoft.Extensions.Logging (version 8.0.0)";

            Assert.Equal(expected, result);
        }

        [Fact]
        public void FormatReferenceAnalysis_ShouldFormatUpdatedReferences()
        {
            var analysis = new ReferenceAnalysisResult
            {
                Changes = new List<PackageReferenceChange>
                {
                    new PackageReferenceChange
                    {
                        Name = "Newtonsoft.Json",
                        OldVersion = "13.0.2",
                        NewVersion = "13.0.3"
                    }
                }
            };

            var result = Program.FormatReferenceAnalysis(analysis);

            var expected = @"Project References:
Updated references:
- Newtonsoft.Json (version 13.0.2 -> 13.0.3)";

            Assert.Equal(expected, result);
        }
    }
}
