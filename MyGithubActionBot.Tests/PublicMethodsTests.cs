using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Xunit;

namespace MyGithubActionBot.Tests
{
    public class PublicMethodsTests
    {
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
    }
}
