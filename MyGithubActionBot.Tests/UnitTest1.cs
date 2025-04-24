using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
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
        [InlineData("public void MyMethod()", false, "")]
        public void TestDeletedPublicMethodRegex_ValidValues(string input, bool isMatchExpected, string expectedMethodName)
        {
            var regex = new Regex(Program.DeletedPublicMethodRegex);

            var match = regex.Match(input);

            Assert.Equal(isMatchExpected, match.Success);

            Assert.Equal(expectedMethodName, match.Groups[1].Value);
        }
    }
}
