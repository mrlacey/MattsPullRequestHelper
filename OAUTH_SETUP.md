# GitHub OAuth App Setup for Pull Request Helper Desktop

This document provides clear instructions for setting up GitHub OAuth authentication for the Pull Request Helper desktop application.

## OAuth Authentication

The Pull Request Helper desktop app uses **GitHub OAuth** for secure authentication. This provides a better user experience than Personal Access Tokens as users don't need to manually generate and manage tokens.

### How OAuth Works in the App

1. **Click "Login to GitHub"** in the desktop app
2. **Browser opens** to GitHub's authorization page
3. **Authorize the application** by clicking "Authorize" on GitHub
4. **Automatic redirect** back to the app with secure token
5. **Token stored securely** for future use

### OAuth App Configuration

The app is configured with the following OAuth settings:

- **Client ID**: `Ov23liSYS1ygVD9r7e5x`
- **Authorization callback URL**: `http://localhost:8080/auth/callback`
- **Required scopes**: `repo` (access to repositories for reading PR data)

### Security Features

- **Local callback server**: The app starts a temporary local server to receive the OAuth callback
- **State parameter**: Prevents CSRF attacks during the OAuth flow
- **Secure token storage**: Access tokens are base64 encoded and stored in the user's AppData directory
- **No client secrets**: The app uses GitHub's recommended OAuth flow for desktop applications

### Troubleshooting OAuth

#### Common Issues

**Browser doesn't open automatically**
- Manually navigate to the authorization URL shown in the app
- Ensure your default browser is set correctly

**"Access denied" or authorization fails**
- Make sure you're logged into the correct GitHub account
- Check that your GitHub account has access to the repositories you want to analyze

**Callback fails or times out**
- Ensure port 8080 is not blocked by your firewall
- Try closing and reopening the app to restart the OAuth flow

**Token expires or becomes invalid**
- Click "Logout" in the app and then "Login to GitHub" again
- The app will automatically refresh your authentication

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

If you want to customize the OAuth configuration:

1. **Fork the repository** and create your own OAuth app
2. **Update the Client ID** in `GitHubOAuthService.cs`
3. **Configure callback URLs** as needed for your environment
4. **Rebuild the application** with your OAuth settings

### Privacy and Security

- **Local processing**: All PR analysis is performed locally on your machine
- **No data collection**: The app only communicates with GitHub's API
- **Token security**: Tokens are stored using platform-appropriate security measures
- **Minimal permissions**: The app only requests necessary repository read permissions

### Comparison with Personal Access Tokens

**OAuth Advantages:**
- ✅ Automatic token management
- ✅ Better security (no manual token handling)
- ✅ Easier user experience
- ✅ Automatic expiration and refresh

**Personal Access Token drawbacks:**
- ❌ Manual token generation required
- ❌ User must remember to regenerate expired tokens  
- ❌ Risk of accidentally sharing tokens
- ❌ No automatic refresh capabilities

For these reasons, the Pull Request Helper desktop app uses OAuth as the primary authentication method.