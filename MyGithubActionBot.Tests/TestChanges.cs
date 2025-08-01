using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Xunit;

namespace MyGithubActionBot.Tests
{
	public class TestChanges
	{
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
	}
}
