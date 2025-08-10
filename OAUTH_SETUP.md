# GitHub OAuth App Setup for Pull Request Helper Desktop

This document provides clear instructions for setting up GitHub OAuth authentication using Device Flow for the Pull Request Helper desktop application.

## Device Flow Authentication

The Pull Request Helper desktop app uses **GitHub Device Flow** for secure authentication. This is the recommended OAuth method for desktop applications as it doesn't require client secrets and provides excellent security.

### How Device Flow Works in the App

1. **Click "Login to GitHub"** in the desktop app
2. **Copy the device code** shown in the app
3. **Browser opens** to GitHub's device authorization page
4. **Enter the device code** on the GitHub page
5. **Authorize the application** by clicking "Authorize" on GitHub
6. **App automatically receives token** and completes authentication

### Setting Up the GitHub OAuth App

To configure the Pull Request Helper for your use, you need to create a GitHub OAuth App with Device Flow support.

#### Step 1: Create a New OAuth App

1. Go to your GitHub account settings
2. Navigate to **Developer settings** → **OAuth Apps**
3. Click **"New OAuth App"**

#### Step 2: Configure OAuth App Settings

Fill in the OAuth app registration form:

- **Application name**: `Pull Request Helper Desktop` (or your preferred name)
- **Homepage URL**: `https://github.com/mrlacey/MattsPullRequestHelper`
- **Application description**: `Desktop app for analyzing GitHub pull requests`
- **Authorization callback URL**: `http://localhost` (required field, but not used by device flow)

#### Step 3: Enable Device Flow

After creating the OAuth app:

1. Go to your OAuth app settings
2. Scroll down to **"Device flow"** section
3. **Check the box** for **"Enable Device Flow"**
4. Click **"Update application"**

#### Step 4: Configure the Desktop App

1. Copy your **Client ID** from the OAuth app settings
2. Open `PullRequestHelper.Desktop/Services/GitHubOAuthService.cs`
3. Replace `REPLACE_WITH_YOUR_CLIENT_ID` with your actual Client ID:

```csharp
private const string ClientId = "Ov23liu5TKJuLRiJ2tGS"; // Replace with your Client ID
```

4. Rebuild the application

### Required OAuth App Configuration

**Critical Settings:**
- ✅ **Device Flow**: Must be **ENABLED**
- ✅ **Client ID**: Must be configured in the app
- ❌ **Client Secret**: Not needed for device flow
- ❌ **Callback URL**: Not used (but required field - use `http://localhost`)

### Security Features

- **No client secrets**: Device flow eliminates the need for client secrets
- **User verification**: Users manually enter codes, preventing automated attacks
- **Secure token storage**: Access tokens are stored securely on the user's device
- **Manual authorization**: Each authentication requires explicit user consent
- **Limited scope**: Only requests necessary `repo` permissions

### Troubleshooting Device Flow

#### Common Issues

**"Device flow is not enabled for this OAuth app"**
- Ensure Device Flow is enabled in your OAuth app settings
- Check that you're using the correct Client ID

**"Invalid device code" error**
- Device codes expire quickly (usually 15 minutes)
- Click "Login to GitHub" again to get a new code

**Browser doesn't open automatically**
- Manually navigate to `https://github.com/login/device`
- Enter the device code shown in the app

**"Access denied" or authorization fails**
- Make sure you're logged into the correct GitHub account
- Check that your GitHub account has access to the repositories you want to analyze

**Code entry issues**
- Device codes are case-sensitive
- Copy the exact code shown in the app
- Use dashes if shown in the code

#### Permission Requirements

For the app to analyze pull requests, your GitHub account needs:
- **Read access** to the repository containing the PR
- **Repository scope** granted during OAuth authorization
- For **private repositories**: your account must have access to the specific repository
- For **organization repositories**: you may need to authorize the OAuth app for the organization

### Advanced Configuration

#### For Organization Administrators

If you're using this app in an organization context:

1. **OAuth app approval**: Admins may need to approve the OAuth app for organization use
2. **Repository access policies**: Ensure the OAuth app can access necessary repositories
3. **Third-party application policies**: Check if your organization allows third-party OAuth apps

#### For Developers

If you want to create your own OAuth app:

1. **Fork the repository** and create your own OAuth app
2. **Enable Device Flow** in your OAuth app settings
3. **Update the Client ID** in `GitHubOAuthService.cs`
4. **Configure device flow settings** as needed for your environment
5. **Rebuild the application** with your OAuth settings

### Device Flow vs Traditional OAuth

**Device Flow Advantages:**
- ✅ No client secret required
- ✅ Better security for desktop apps
- ✅ No local web server needed
- ✅ Manual user verification
- ✅ Designed for native applications

**Traditional OAuth drawbacks:**
- ❌ Requires client secret management
- ❌ Needs local callback server
- ❌ Security risks for desktop apps
- ❌ Complex redirect handling

### Privacy and Security

- **Local processing**: All PR analysis is performed locally on your machine
- **No data collection**: The app only communicates with GitHub's API
- **Token security**: Tokens are stored using platform-appropriate security measures
- **Minimal permissions**: The app only requests necessary repository read permissions
- **Manual authorization**: Each authentication requires explicit user consent

### Example OAuth App Configuration

Here's what your GitHub OAuth app settings should look like:

```
Application name: Pull Request Helper Desktop
Homepage URL: https://github.com/mrlacey/MattsPullRequestHelper
Application description: Desktop app for analyzing GitHub pull requests
Authorization callback URL: http://localhost
Device flow: ✅ Enabled
```

For production use, replace the Client ID in the source code and rebuild the application.