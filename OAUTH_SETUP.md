# GitHub OAuth App Setup for Pull Request Helper Desktop

This document provides clear instructions for setting up GitHub OAuth authentication for the Pull Request Helper desktop application.

## Current Authentication Method

Currently, the Pull Request Helper desktop app uses **Personal Access Tokens (PATs)** for authentication, which is simpler to set up than OAuth but requires manual token generation.

### Setting up a Personal Access Token

1. **Go to GitHub Settings**
   - Navigate to https://github.com/settings/tokens
   - Or: GitHub.com → Profile Icon → Settings → Developer settings → Personal access tokens → Tokens (classic)

2. **Generate New Token**
   - Click "Generate new token" → "Generate new token (classic)"
   - Add a note (e.g., "Pull Request Helper Desktop")
   - Set expiration as needed (30 days, 60 days, 90 days, or No expiration)

3. **Select Required Permissions**
   The following scopes are required for the app to function:
   - `repo` - Full control of private repositories (needed to read PR data)
   - `public_repo` - Access public repositories (if you only analyze public repos)

4. **Generate and Copy Token**
   - Click "Generate token"
   - **IMPORTANT**: Copy the token immediately - you won't be able to see it again
   - The token should look like: `ghp_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`

5. **Use Token in Application**
   - Open the Pull Request Helper desktop app
   - Click "Login to GitHub"
   - Paste your token in the dialog
   - The token will be securely stored for future use

## Future OAuth Implementation

For a full OAuth implementation (not currently implemented), the following would be required:

### GitHub OAuth App Registration

1. **Create OAuth App**
   - Go to https://github.com/settings/applications/new
   - Fill in application details:
     - Application name: "Pull Request Helper Desktop"
     - Homepage URL: Your application's homepage or repository URL
     - Authorization callback URL: `http://localhost:8080/auth/callback` (or your preferred local callback)

2. **OAuth App Configuration**
   - Note down the Client ID and Client Secret
   - Keep the Client Secret secure and never commit it to version control

3. **Required OAuth Scopes**
   - `repo` - Access to private repositories
   - `public_repo` - Access to public repositories

### Implementation Requirements

To implement OAuth (future enhancement):

1. **Add OAuth Dependencies**
   ```xml
   <PackageReference Include="Microsoft.AspNetCore.Authentication.OAuth" Version="8.0.0" />
   ```

2. **Local HTTP Server**
   - Start a local HTTP server to handle the OAuth callback
   - Listen on a specific port (e.g., 8080)

3. **OAuth Flow**
   - Redirect user to GitHub authorization URL
   - Handle callback with authorization code
   - Exchange code for access token
   - Store token securely

4. **Configuration**
   - Store Client ID in app configuration
   - Client Secret should be handled securely (potentially through user input or secure configuration)

## Security Considerations

### Current Implementation (PAT)
- Tokens are base64 encoded before storage (not encryption, but better than plain text)
- Tokens are stored in user's AppData directory
- On Windows, could be enhanced with DPAPI encryption
- On macOS/Linux, could be enhanced with Keychain/Keyring integration

### Future OAuth Implementation
- Access tokens should be stored using OS-level secure storage
- Refresh tokens should be used when available
- Client secrets should never be embedded in the application

## Troubleshooting

### Token Issues
- **Invalid token**: Ensure the token has correct permissions (`repo` scope)
- **Token expired**: Generate a new token following the steps above
- **Access denied**: Verify the token has access to the specific repository

### Permission Issues
- Ensure the token has `repo` scope for private repositories
- For organization repositories, the token may need additional organization permissions

### Storage Issues
- Token storage location: `%APPDATA%/PullRequestHelper/token.dat` (Windows) or equivalent on other platforms
- To reset: Delete the token file or use the "Logout" button in the app