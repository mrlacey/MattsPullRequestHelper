using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PullRequestHelper.Desktop.Services
{
	public class GitHubOAuthService
	{
		private const string ClientId = "Ov23liSYS1ygVD9r7e5x"; // GitHub OAuth App Client ID
		private const string RedirectUri = "http://localhost:8080/auth/callback";
		private const string AuthorizeUrl = "https://github.com/login/oauth/authorize";
		private const string TokenUrl = "https://github.com/login/oauth/access_token";

		private readonly HttpClient _httpClient;
		private HttpListener? _httpListener;

		public GitHubOAuthService()
		{
			_httpClient = new HttpClient();
			_httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
			_httpClient.DefaultRequestHeaders.Add("User-Agent", "PullRequestHelper-Desktop");
		}

		public async Task<string?> AuthenticateAsync()
		{
			try
			{
				// Start local HTTP server to handle callback
				_httpListener = new HttpListener();
				_httpListener.Prefixes.Add(RedirectUri + "/");
				_httpListener.Start();

				// Generate state parameter for security
				var state = Guid.NewGuid().ToString();

				// Build authorization URL
				var authUrl = $"{AuthorizeUrl}?client_id={ClientId}&redirect_uri={Uri.EscapeDataString(RedirectUri)}&scope=repo&state={state}";

				// Open browser to authorization URL
				OpenBrowser(authUrl);

				// Wait for callback
				var context = await _httpListener.GetContextAsync();
				var request = context.Request;
				var response = context.Response;

				// Extract authorization code from callback
				var query = request.Url?.Query;
				if (string.IsNullOrEmpty(query))
				{
					await SendResponse(response, "Error: No query parameters received");
					return null;
				}

				var queryParams = ParseQueryString(query);
				var code = queryParams.GetValueOrDefault("code");
				var receivedState = queryParams.GetValueOrDefault("state");
				var error = queryParams.GetValueOrDefault("error");

				if (!string.IsNullOrEmpty(error))
				{
					await SendResponse(response, $"Authentication error: {error}");
					return null;
				}

				if (receivedState != state)
				{
					await SendResponse(response, "Error: Invalid state parameter");
					return null;
				}

				if (string.IsNullOrEmpty(code))
				{
					await SendResponse(response, "Error: No authorization code received");
					return null;
				}

				// Exchange code for access token
				var accessToken = await ExchangeCodeForTokenAsync(code);

				if (!string.IsNullOrEmpty(accessToken))
				{
					await SendResponse(response, "Authentication successful! You can close this window.");
					return accessToken;
				}
				else
				{
					await SendResponse(response, "Error: Failed to obtain access token");
					return null;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"OAuth authentication error: {ex.Message}");
				return null;
			}
			finally
			{
				_httpListener?.Stop();
				_httpListener?.Close();
			}
		}

		private async Task<string?> ExchangeCodeForTokenAsync(string code)
		{
			try
			{
				var tokenRequest = new
				{
					client_id = ClientId,
					code = code,
					redirect_uri = RedirectUri
				};

				var json = JsonSerializer.Serialize(tokenRequest);
				var content = new StringContent(json, Encoding.UTF8, "application/json");

				var response = await _httpClient.PostAsync(TokenUrl, content);
				var responseContent = await response.Content.ReadAsStringAsync();

				if (response.IsSuccessStatusCode)
				{
					var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
					if (tokenResponse.TryGetProperty("access_token", out var accessTokenElement))
					{
						return accessTokenElement.GetString();
					}
				}

				Console.WriteLine($"Token exchange failed: {responseContent}");
				return null;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Token exchange error: {ex.Message}");
				return null;
			}
		}

		private static void OpenBrowser(string url)
		{
			try
			{
				if (OperatingSystem.IsWindows())
				{
					Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
				}
				else if (OperatingSystem.IsLinux())
				{
					Process.Start("xdg-open", url);
				}
				else if (OperatingSystem.IsMacOS())
				{
					Process.Start("open", url);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to open browser: {ex.Message}");
			}
		}

		private static async Task SendResponse(HttpListenerResponse response, string message)
		{
			try
			{
				var html = $@"
<!DOCTYPE html>
<html>
<head>
	<title>Pull Request Helper - Authentication</title>
	<style>
		body {{ font-family: Arial, sans-serif; text-align: center; padding: 50px; }}
		.success {{ color: green; }}
		.error {{ color: red; }}
	</style>
</head>
<body>
	<h1>Pull Request Helper</h1>
	<p class=""{(message.Contains("successful") ? "success" : "error")}"">{message}</p>
</body>
</html>";

				var buffer = Encoding.UTF8.GetBytes(html);
				response.ContentLength64 = buffer.Length;
				response.ContentType = "text/html";
				await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
				response.OutputStream.Close();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to send response: {ex.Message}");
			}
		}

		private static Dictionary<string, string> ParseQueryString(string query)
		{
			var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			if (string.IsNullOrEmpty(query))
				return result;

			// Remove leading '?' if present
			if (query.StartsWith("?"))
				query = query.Substring(1);

			var pairs = query.Split('&');
			foreach (var pair in pairs)
			{
				var keyValue = pair.Split('=', 2);
				if (keyValue.Length == 2)
				{
					var key = Uri.UnescapeDataString(keyValue[0]);
					var value = Uri.UnescapeDataString(keyValue[1]);
					result[key] = value;
				}
			}

			return result;
		}

		public void Dispose()
		{
			_httpClient?.Dispose();
			_httpListener?.Stop();
			_httpListener?.Close();
		}
	}
}
