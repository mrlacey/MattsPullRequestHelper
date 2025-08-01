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
			Assert.All(changedFiles, file =>
			{
				string filename = file.filename?.ToString() ?? string.Empty;
				Assert.True(filename.EndsWith(".cs") || filename.EndsWith(".csproj"),
					$"File {filename} should end with .cs or .csproj");
			});
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
	}
}
