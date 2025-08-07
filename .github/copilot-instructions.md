# MattsPullRequestHelper - AI Agent Instructions

## Project Overview
This is a GitHub Action (composite action) that analyzes C# pull requests and posts automated code review comments. The main application (`MyGithubActionBot`) fetches PR file changes via GitHub API, analyzes them using regex patterns, and posts structured feedback about tests, public method deletions, and dependency changes.

**Key Architecture:**
- `Program.cs`: Single-file console application with static analysis methods
- `ReferenceChange.cs`: Models dependency changes with computed properties (`IsNew`, `IsRemoved`, `IsUpdated`)
- `action.yml`: Composite GitHub Action that builds and runs the bot
- Tests use xUnit with extensive `[Theory]`/`[InlineData]` patterns for regex validation

## Core Analysis Patterns

**File Change Analysis:** The bot processes GitHub API's PR files endpoint, filtering for `.cs` and `.csproj` files, then analyzes git diff patches line-by-line.

**Regex-Driven Detection:** Five main regex constants drive analysis:
- `DeletedPublicMethodRegex`: Detects `- public` method deletions in diffs
- `PackageReferenceRegex`: Matches `<PackageReference Include="..." Version="..." />`
- `ProjectReferenceRegex`, `FrameworkReferenceRegex`, `ReferenceRegex`: Handle other MSBuild references

**Test Detection:** Looks for test attribute patterns (`[TestMethod]`, `[Fact]`, `[DataRow(`, etc.) in added/removed lines.

## Development Workflows

**Build & Test:**
```bash
dotnet restore
dotnet build --configuration Release
dotnet test --verbosity normal --logger "console;verbosity=detailed"
```

**GitHub Action Testing:** Use test branches and PRs to validate the action behavior. The action requires `GITHUB_TOKEN`, `GITHUB_WORKSPACE`, and `GITHUB_PULL_REQUEST_NUMBER` environment variables.

**Formatting:** Project enforces strict formatting via `dotnet format --verify-no-changes` in CI. Use tabs for indentation (see `.editorconfig`).

## Project-Specific Conventions

**Error Handling:** Use `Environment.Exit(1)` or `Environment.Exit(2)` for different failure scenarios to fail GitHub Actions gracefully.

**Reference Analysis:** The `ProcessReferenceMatch` method handles version-less references (ProjectReference, FrameworkReference) by using `"[NoVersion]"` as a special marker, then formats them as `"no version"` in output.

**Test Patterns:** Extensive use of `[Theory]` with `[InlineData]` for testing regex patterns. Tests never use "Arrange", "Act", "Assert" comments. Regex tests validate both positive and negative cases.

**Message Formatting:** HTML formatting in GitHub comments uses `<br />` tags and bold headers like `<b>PullRequestHelper:</b>`.

## Key Integration Points

**GitHub API:** Uses `HttpClient` with Bearer token auth to fetch PR files and post comments. API calls include proper User-Agent headers (`"MattsPullRequestHelper"`).

**LibGit2Sharp:** Currently imported but not actively used in main analysis flow (analysis uses GitHub API diffs instead).

**Environment Variables:** Three required vars for GitHub Actions context - always validate all are present before proceeding.

## Code Style Rules

- Don't delete comments as part of changes
- Format code after modifications using `dotnet format`
- Don't include "Arrange", "Act", "Assert" comments in tests
- Don't alter method accessibility when making changes
- Use tab indentation (configured in `.editorconfig`)
- Target .NET 9.0 with nullable reference types enabled
