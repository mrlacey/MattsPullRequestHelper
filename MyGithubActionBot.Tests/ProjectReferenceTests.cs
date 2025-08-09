using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using PullRequestHelper.Core.Models;
using Xunit;

namespace MyGithubActionBot.Tests
{
	public class ProjectReferenceTests
	{
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

		[Theory]
		[InlineData(@"<ProjectReference Include=""..\Other\Other.csproj"" />", true, @"..\Other\Other.csproj")]
		[InlineData(@"<ProjectReference Include=""MyProject.csproj"" />", true, "MyProject.csproj")]
		[InlineData(@"<ProjectReference Include=""../AnotherProject/AnotherProject.csproj""/>", true, "../AnotherProject/AnotherProject.csproj")]
		[InlineData(@"<PackageReference Include=""Newtonsoft.Json"" Version=""13.0.3"" />", false, "")]
		[InlineData(@"<Reference Include=""System"" />", false, "")]
		public void TestProjectReferenceRegex_ValidValues(string input, bool isMatchExpected, string expectedProjectPath)
		{
			var regex = new Regex(Program.ProjectReferenceRegex);

			var match = regex.Match(input);

			Assert.Equal(isMatchExpected, match.Success);
			if (isMatchExpected)
			{
				Assert.Equal(expectedProjectPath, match.Groups[1].Value);
			}
		}

		[Theory]
		[InlineData(@"<FrameworkReference Include=""Microsoft.AspNetCore.App"" />", true, "Microsoft.AspNetCore.App")]
		[InlineData(@"<FrameworkReference Include=""Microsoft.WindowsDesktop.App"" />", true, "Microsoft.WindowsDesktop.App")]
		[InlineData(@"<PackageReference Include=""Newtonsoft.Json"" Version=""13.0.3"" />", false, "")]
		public void TestFrameworkReferenceRegex_ValidValues(string input, bool isMatchExpected, string expectedFramework)
		{
			var regex = new Regex(Program.FrameworkReferenceRegex);

			var match = regex.Match(input);

			Assert.Equal(isMatchExpected, match.Success);
			if (isMatchExpected)
			{
				Assert.Equal(expectedFramework, match.Groups[1].Value);
			}
		}

		[Theory]
		[InlineData(@"<Reference Include=""System"" />", true, "System", "")]
		[InlineData(@"<Reference Include=""MyLibrary"" Version=""1.0.0"" />", true, "MyLibrary", "1.0.0")]
		[InlineData(@"<PackageReference Include=""Newtonsoft.Json"" Version=""13.0.3"" />", false, "", "")]
		public void TestReferenceRegex_ValidValues(string input, bool isMatchExpected, string expectedReference, string expectedVersion)
		{
			var regex = new Regex(Program.ReferenceRegex);

			var match = regex.Match(input);

			Assert.Equal(isMatchExpected, match.Success);
			if (isMatchExpected)
			{
				Assert.Equal(expectedReference, match.Groups[1].Value);
				if (match.Groups.Count > 2)
				{
					Assert.Equal(expectedVersion, match.Groups[2].Value);
				}
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
			var changedFiles = new List<dynamic> { mockFile! };

			var result = Program.AnalyzeProjectReferences(changedFiles);

			Assert.Single(result.Changes);
			Assert.Equal("Microsoft.Extensions.Logging", result.Changes[0].Name);
			Assert.Equal("8.0.0", result.Changes[0].NewVersion);
			Assert.Equal("PackageReference", result.Changes[0].Type);
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
			var changedFiles = new List<dynamic> { mockFile! };

			var result = Program.AnalyzeProjectReferences(changedFiles);

			Assert.Single(result.Changes);
			Assert.Equal("Newtonsoft.Json", result.Changes[0].Name);
			Assert.Equal("13.0.2", result.Changes[0].OldVersion);
			Assert.Equal("13.0.3", result.Changes[0].NewVersion);
			Assert.Equal("PackageReference", result.Changes[0].Type);
			Assert.False(result.Changes[0].IsNew);
			Assert.True(result.Changes[0].IsUpdated);
		}

		[Fact]
		public void AnalyzeProjectReferences_ShouldDetectRemovedPackageReferences()
		{
			// Create mock changed file with removed package reference
			var mockFileJson = @"{
				""filename"": ""TestProject.csproj"",
				""patch"": ""@@ -10,7 +10,6 @@\n   <ItemGroup>\n     <PackageReference Include=\""LibGit2Sharp\"" Version=\""0.31.0\"" />\n-    <PackageReference Include=\""Newtonsoft.Json\"" Version=\""13.0.3\"" />\n   </ItemGroup>\n\n </Project>""
			}";

			var mockFile = JsonConvert.DeserializeObject(mockFileJson);
			var changedFiles = new List<dynamic> { mockFile! };

			var result = Program.AnalyzeProjectReferences(changedFiles);

			Assert.Single(result.Changes);
			Assert.Equal("Newtonsoft.Json", result.Changes[0].Name);
			Assert.Equal("13.0.3", result.Changes[0].OldVersion);
			Assert.Equal("", result.Changes[0].NewVersion);
			Assert.Equal("PackageReference", result.Changes[0].Type);
			Assert.False(result.Changes[0].IsNew);
			Assert.True(result.Changes[0].IsRemoved);
		}

		[Fact]
		public void AnalyzeProjectReferences_ShouldDetectNewProjectReferences()
		{
			// Create mock changed file with new project reference
			var mockFileJson = @"{
				""filename"": ""TestProject.csproj"",
				""patch"": ""@@ -10,6 +10,7 @@\n   <ItemGroup>\n     <PackageReference Include=\""LibGit2Sharp\"" Version=\""0.31.0\"" />\n+    <ProjectReference Include=\""..\\OtherProject\\OtherProject.csproj\"" />\n   </ItemGroup>\n\n </Project>""
			}";

			var mockFile = JsonConvert.DeserializeObject(mockFileJson);
			var changedFiles = new List<dynamic> { mockFile! };

			var result = Program.AnalyzeProjectReferences(changedFiles);

			Assert.Single(result.Changes);
			Assert.Equal(@"..\OtherProject\OtherProject.csproj", result.Changes[0].Name);
			Assert.Equal("[NoVersion]", result.Changes[0].NewVersion);
			Assert.Equal("ProjectReference", result.Changes[0].Type);
			Assert.True(result.Changes[0].IsNew);
		}

		[Fact]
		public void AnalyzeProjectReferences_ShouldDetectNewFrameworkReferences()
		{
			// Create mock changed file with new framework reference
			var mockFileJson = @"{
				""filename"": ""TestProject.csproj"",
				""patch"": ""@@ -10,6 +10,7 @@\n   <ItemGroup>\n     <PackageReference Include=\""LibGit2Sharp\"" Version=\""0.31.0\"" />\n+    <FrameworkReference Include=\""Microsoft.AspNetCore.App\"" />\n   </ItemGroup>\n\n </Project>""
			}";

			var mockFile = JsonConvert.DeserializeObject(mockFileJson);
			var changedFiles = new List<dynamic> { mockFile! };

			var result = Program.AnalyzeProjectReferences(changedFiles);

			Assert.Single(result.Changes);
			Assert.Equal("Microsoft.AspNetCore.App", result.Changes[0].Name);
			Assert.Equal("[NoVersion]", result.Changes[0].NewVersion);
			Assert.Equal("FrameworkReference", result.Changes[0].Type);
			Assert.True(result.Changes[0].IsNew);
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
			var changedFiles = new List<dynamic> { mockFile! };

			var result = Program.AnalyzeProjectReferences(changedFiles);

			Assert.Empty(result.Changes);
		}

		[Fact]
		public void FormatReferenceAnalysis_ShouldReturnNoReferencesMessage_WhenEmpty()
		{
			var analysis = new ReferenceAnalysisResult();

			var result = Program.FormatReferenceAnalysis(analysis);

			Assert.Equal($"Project References:{Environment.NewLine}* no reference changes detected *", result);
		}

		[Fact]
		public void FormatReferenceAnalysis_ShouldFormatNewReferences()
		{
			var analysis = new ReferenceAnalysisResult
			{
				Changes = new List<ReferenceChange>
				{
					new ReferenceChange
					{
						Name = "Microsoft.Extensions.Logging",
						NewVersion = "8.0.0",
						Type = "PackageReference"
					}
				}
			};

			var result = Program.FormatReferenceAnalysis(analysis);

			var expected = @"Project References:
New references:
- Microsoft.Extensions.Logging (version 8.0.0) [PackageReference]";

			Assert.Equal(expected, result);
		}

		[Fact]
		public void FormatReferenceAnalysis_ShouldFormatUpdatedReferences()
		{
			var analysis = new ReferenceAnalysisResult
			{
				Changes = new List<ReferenceChange>
				{
					new ReferenceChange
					{
						Name = "Newtonsoft.Json",
						OldVersion = "13.0.2",
						NewVersion = "13.0.3",
						Type = "PackageReference"
					}
				}
			};

			var result = Program.FormatReferenceAnalysis(analysis);

			var expected = @"Project References:
Updated references:
- Newtonsoft.Json (version 13.0.2 -> 13.0.3) [PackageReference]";

			Assert.Equal(expected, result);
		}

		[Fact]
		public void FormatReferenceAnalysis_ShouldFormatRemovedReferences()
		{
			var analysis = new ReferenceAnalysisResult
			{
				Changes = new List<ReferenceChange>
				{
					new ReferenceChange
					{
						Name = "Newtonsoft.Json",
						OldVersion = "13.0.3",
						NewVersion = "",
						Type = "PackageReference"
					}
				}
			};

			var result = Program.FormatReferenceAnalysis(analysis);

			var expected = @"Project References:
Removed references:
- Newtonsoft.Json (version 13.0.3) [PackageReference]";

			Assert.Equal(expected, result);
		}

		[Fact]
		public void FormatReferenceAnalysis_ShouldFormatMixedReferences()
		{
			var analysis = new ReferenceAnalysisResult
			{
				Changes = new List<ReferenceChange>
				{
					new ReferenceChange
					{
						Name = "Microsoft.Extensions.Logging",
						NewVersion = "8.0.0",
						Type = "PackageReference"
					},
					new ReferenceChange
					{
						Name = "Newtonsoft.Json",
						OldVersion = "13.0.2",
						NewVersion = "13.0.3",
						Type = "PackageReference"
					},
					new ReferenceChange
					{
						Name = @"..\OtherProject\OtherProject.csproj",
						NewVersion = "[NoVersion]",
						Type = "ProjectReference"
					},
					new ReferenceChange
					{
						Name = "OldLibrary",
						OldVersion = "1.0.0",
						NewVersion = "",
						Type = "PackageReference"
					}
				}
			};

			var result = Program.FormatReferenceAnalysis(analysis);

			var expected = @"Project References:
New references:
- Microsoft.Extensions.Logging (version 8.0.0) [PackageReference]
- ..\OtherProject\OtherProject.csproj [ProjectReference]

Updated references:
- Newtonsoft.Json (version 13.0.2 -> 13.0.3) [PackageReference]

Removed references:
- OldLibrary (version 1.0.0) [PackageReference]";

			Assert.Equal(expected, result);
		}

		[Fact]
		public void AnalyzeTestMethods_ShouldIgnoreNonCsFiles()
		{
			// Create mock changed files with both .cs and .csproj files
			var mockCsFileJson = @"{
				""filename"": ""TestClass.cs"",
				""patch"": ""@@ -1,4 +1,6 @@\n using System;\n+using Xunit;\n\n public class TestClass\n {\n+    [Fact]\n+    public void NewTest() { }\n }""
			}";

			var mockCsprojFileJson = @"{
				""filename"": ""TestProject.csproj"",
				""patch"": ""@@ -10,6 +10,7 @@\n   <ItemGroup>\n     <PackageReference Include=\""LibGit2Sharp\"" Version=\""0.31.0\"" />\n+    <PackageReference Include=\""Microsoft.Extensions.Logging\"" Version=\""8.0.0\"" />\n   </ItemGroup>\n\n </Project>""
			}";

			var mockCsFile = JsonConvert.DeserializeObject(mockCsFileJson);
			var mockCsprojFile = JsonConvert.DeserializeObject(mockCsprojFileJson);
			var changedFiles = new List<dynamic> { mockCsFile!, mockCsprojFile! };

			var result = Program.AnalyzeTestMethods(changedFiles);

			// Should only count tests from .cs files, ignoring .csproj files
			Assert.Equal(1, result.Added);
			Assert.Equal(0, result.Deleted);
		}

		[Fact]
		public void AnalyzeDeletedPublicMethods_ShouldIgnoreNonCsFiles()
		{
			// Create mock changed files with both .cs and .csproj files
			var mockCsFileJson = @"{
				""filename"": ""TestClass.cs"",
				""patch"": ""@@ -1,6 +1,4 @@\n using System;\n\n public class TestClass\n {\n-    public void DeletedMethod() { }\n }""
			}";

			var mockCsprojFileJson = @"{
				""filename"": ""TestProject.csproj"",
				""patch"": ""@@ -10,7 +10,6 @@\n   <ItemGroup>\n     <PackageReference Include=\""LibGit2Sharp\"" Version=\""0.31.0\"" />\n-    <PackageReference Include=\""Microsoft.Extensions.Logging\"" Version=\""8.0.0\"" />\n   </ItemGroup>\n\n </Project>""
			}";

			var mockCsFile = JsonConvert.DeserializeObject(mockCsFileJson);
			var mockCsprojFile = JsonConvert.DeserializeObject(mockCsprojFileJson);
			var changedFiles = new List<dynamic> { mockCsFile!, mockCsprojFile! };

			var result = Program.AnalyzeDeletedPublicMethods(changedFiles);

			// Should only find deleted methods from .cs files, ignoring .csproj files
			Assert.Single(result);
			Assert.Contains("DeletedMethod", result[0]);
		}
	}
}
