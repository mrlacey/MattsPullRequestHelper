# Matt's Pull Request Helper

This project provides automated pull request analysis for C# projects, available both as a GitHub Action and a standalone desktop application.

A simple GitHub Action and desktop app to help when reviewing a Pull Request.

The aim is to help make reviewing code as easy as possible and help avoid missing something important. These are things to look for as part of a review but there isn't existing tooling that can do this automatically.

## Components

### GitHub Action Bot
The GitHub Action analyzes pull requests and posts automated comments about:
- Test method changes (added/removed)
- Deleted public methods
- Project reference changes (packages, projects, frameworks)

### Desktop Application
A standalone Avalonia-based desktop app that provides the same analysis functionality without requiring GitHub Action installation. Perfect for quick analysis of any public or private pull request.

## Current functionality

- Report the number of tests added. (If low or none, may need to ask why or it could indicate the PR isn't ready for review.)
- Report the number of tests deleted. (If there are any, it should raise questions.)
- Report any public methods that have been removed. (Depending on the project this may need to be documented or avoided.)
- Report any changes in project references and dependencies.

Written in C# and intended for reviewing C# files.

## Usage

### GitHub Action
Add this to your `.github/workflows/` directory:

```yaml
name: PR Analysis
on:
  pull_request:
    types: [opened, synchronize]

jobs:
  analyze:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: mrlacey/MattsPullRequestHelper@main
        with:
          github-token: ${{ secrets.GITHUB_TOKEN }}
```

### Desktop Application

1. **Download and Run**
   - Download the latest release from the [Releases page](../../releases)
   - Run `PullRequestHelper.Desktop.exe` (Windows) or equivalent for your platform

2. **Setup Authentication**
   - Click "Login to GitHub"
   - Enter your GitHub Personal Access Token (see [OAuth Setup Guide](OAUTH_SETUP.md))
   - Token is securely stored for future use

3. **Analyze Pull Requests**
   - Paste any GitHub PR URL (e.g., `https://github.com/owner/repo/pull/123`)
   - Click "Analyze PR"
   - View results and copy to clipboard as needed

## Features

### Analysis Capabilities
- **Test Detection**: Identifies added/removed test methods using common test frameworks (xUnit, NUnit, MSTest)
- **Public Method Analysis**: Detects deleted public methods that could be breaking changes
- **Reference Tracking**: Monitors changes to NuGet packages, project references, and framework references
- **Cross-platform**: Works on Windows, macOS, and Linux

### Security
- GitHub tokens are stored securely using OS-level protection when available
- No data is transmitted except directly to GitHub's API
- All analysis is performed locally

## Architecture

The solution is organized into three main components:

- **`PullRequestHelper.Core`**: Shared library containing all analysis logic
- **`MyGithubActionBot`**: Console application for GitHub Actions
- **`PullRequestHelper.Desktop`**: Avalonia-based GUI application

This architecture ensures the same analysis logic is used in both the GitHub Action and desktop app, maintaining consistency.

## Development

### Prerequisites
- .NET 8.0 SDK
- Git

### Building
```bash
git clone https://github.com/mrlacey/MattsPullRequestHelper.git
cd MattsPullRequestHelper
dotnet restore
dotnet build
```

### Testing
```bash
dotnet test
```

### Running Desktop App
```bash
cd PullRequestHelper.Desktop
dotnet run
```

## Authentication Setup

See the [OAuth Setup Guide](OAUTH_SETUP.md) for detailed instructions on setting up GitHub authentication for the desktop application.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
