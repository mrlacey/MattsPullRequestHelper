using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PullRequestHelper.Desktop.Services
{
	public class DeviceFlowResponse
	{
		public string device_code { get; set; } = "";
		public string user_code { get; set; } = "";
		public string verification_uri { get; set; } = "";
		public int expires_in { get; set; }
		public int interval { get; set; }
	}

	public class TokenResponse
	{
		public string? access_token { get; set; }
		public string? error { get; set; }
		public string? error_description { get; set; }
	}

	public class GitHubOAuthService
	{
		private const string ClientId = "REPLACE_WITH_YOUR_CLIENT_ID";
		private const string Scope = "repo";
		private readonly HttpClient _httpClient;

		public GitHubOAuthService()
		{
			_httpClient = new HttpClient();
			_httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
			_httpClient.DefaultRequestHeaders.Add("User-Agent", "MattsPullRequestHelper");
		}

		public async Task<DeviceFlowResponse> StartDeviceFlow()
		{
			var request = new
			{
				client_id = ClientId,
				scope = Scope
			};

			var json = JsonConvert.SerializeObject(request);
			var content = new StringContent(json, Encoding.UTF8, "application/json");

			var response = await _httpClient.PostAsync("https://github.com/login/device/code", content);
			if (response.IsSuccessStatusCode)
			{
				var responseJson = await response.Content.ReadAsStringAsync();
				var result = JsonConvert.DeserializeObject<DeviceFlowResponse>(responseJson);
				return result ?? throw new Exception("Failed to deserialize device flow response");
			}

			throw new Exception($"Failed to start device flow: {response.StatusCode}");
		}

		public async Task<string> PollForToken(string deviceCode, int interval, CancellationToken cancellationToken = default)
		{
			var request = new
			{
				client_id = ClientId,
				device_code = deviceCode,
				grant_type = "urn:ietf:params:oauth:grant-type:device_code"
			};

			var json = JsonConvert.SerializeObject(request);

			while (!cancellationToken.IsCancellationRequested)
			{
				await Task.Delay(interval * 1000, cancellationToken);

				var content = new StringContent(json, Encoding.UTF8, "application/json");
				var response = await _httpClient.PostAsync("https://github.com/login/oauth/access_token", content);

				if (response.IsSuccessStatusCode)
				{
					var responseJson = await response.Content.ReadAsStringAsync();
					var result = JsonConvert.DeserializeObject<TokenResponse>(responseJson);

					if (!string.IsNullOrEmpty(result?.access_token))
					{
						return result.access_token;
					}

					// Handle specific errors
					if (result?.error == "authorization_pending")
					{
						// Continue polling
						continue;
					}
					else if (result?.error == "slow_down")
					{
						// Increase interval and continue
						await Task.Delay(5000, cancellationToken);
						continue;
					}
					else if (result?.error == "expired_token")
					{
						throw new Exception("The device code has expired. Please try again.");
					}
					else if (result?.error == "access_denied")
					{
						throw new Exception("Access was denied by the user.");
					}
					else
					{
						throw new Exception($"OAuth error: {result?.error} - {result?.error_description}");
					}
				}
				else
				{
					throw new Exception($"Failed to poll for token: {response.StatusCode}");
				}
			}

			throw new OperationCanceledException("Token polling was cancelled.");
		}

		public void Dispose()
		{
			_httpClient?.Dispose();
		}
	}
}
