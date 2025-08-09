using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PullRequestHelper.Desktop.Services
{
	public class GitHubOAuthService
	{
		private const string ClientId = "REPLACE THIS";
		private const string ClientSecret = "REPLACE THIS";
		private const string RedirectUri = "http://localhost:8080/auth/callback";
		private const string Scope = "repo";

		private string? _currentState;

		public string GetAuthorizationUrl()
		{
			_currentState = Guid.NewGuid().ToString("N");
			return $"https://github.com/login/oauth/authorize?" +
				   $"client_id={ClientId}&" +
				   $"redirect_uri={Uri.EscapeDataString(RedirectUri)}&" +
				   $"scope={Scope}&" +
				   $"state={_currentState}";
		}

		public bool ValidateState(string receivedState)
		{
			return !string.IsNullOrEmpty(_currentState) &&
				   _currentState.Equals(receivedState, StringComparison.Ordinal);
		}

		public async Task<string> ExchangeCodeForToken(string code)
		{
			using var client = new HttpClient();

			var request = new
			{
				client_id = ClientId,
				client_secret = ClientSecret,
				code = code,
				redirect_uri = RedirectUri
			};

			var json = JsonConvert.SerializeObject(request);
			var content = new StringContent(json, Encoding.UTF8, "application/json");

			client.DefaultRequestHeaders.Add("Accept", "application/json");
			client.DefaultRequestHeaders.Add("User-Agent", "MattsPullRequestHelper");

			var response = await client.PostAsync("https://github.com/login/oauth/access_token", content);
			if (response.IsSuccessStatusCode)
			{
				var responseJson = await response.Content.ReadAsStringAsync();
				dynamic result = JsonConvert.DeserializeObject(responseJson);
				return result.access_token;
			}

			throw new Exception($"Failed to exchange code for token: {response.StatusCode}");
		}
	}

	public class OAuthCallbackListener
	{
		private HttpListener _listener;
		private TaskCompletionSource<(string code, string state)> _callbackReceived;
		
		public async Task<(string code, string state)> WaitForCallback()
		{
			_listener = new HttpListener();
			_listener.Prefixes.Add("http://localhost:8080/");
			_listener.Start();
			
			_callbackReceived = new TaskCompletionSource<(string, string)>();
			
			_ = Task.Run(async () =>
			{
				try
				{
					var context = await _listener.GetContextAsync();
					var code = context.Request.QueryString["code"];
					var state = context.Request.QueryString["state"];
					var error = context.Request.QueryString["error"];
					
					if (!string.IsNullOrEmpty(error))
					{
						var errorResponse = Encoding.UTF8.GetBytes(
							$"<html><body><h1>Authorization failed!</h1><p>Error: {error}</p></body></html>");
						context.Response.ContentLength64 = errorResponse.Length;
						await context.Response.OutputStream.WriteAsync(errorResponse, 0, errorResponse.Length);
						context.Response.Close();
						
						_callbackReceived.SetException(new Exception($"OAuth error: {error}"));
						return;
					}
					
					var successResponse = Encoding.UTF8.GetBytes(
						"<html><body><h1>Authorization successful!</h1><p>You can close this window.</p></body></html>");
					context.Response.ContentLength64 = successResponse.Length;
					await context.Response.OutputStream.WriteAsync(successResponse, 0, successResponse.Length);
					context.Response.Close();
					
					_callbackReceived.SetResult((code, state));
				}
				catch (Exception ex)
				{
					_callbackReceived.SetException(ex);
				}
			});
			
			return await _callbackReceived.Task;
		}
		
		public void Stop()
		{
			_listener?.Stop();
		}
	}
}
