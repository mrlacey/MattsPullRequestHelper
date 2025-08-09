using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PullRequestHelper.Core;
using PullRequestHelper.Desktop.Services;

namespace PullRequestHelper.Desktop
{
	public partial class MainWindow : Window
	{
		private readonly PullRequestAnalyzer _analyzer;
		private readonly TokenStorage _tokenStorage;
		private readonly GitHubOAuthService _oauthService;
		private string? _githubToken;
		private CancellationTokenSource? _authCancellationTokenSource;

		public MainWindow()
		{
			InitializeComponent();
			_analyzer = new PullRequestAnalyzer();
			_tokenStorage = new TokenStorage();
			_oauthService = new GitHubOAuthService();

			// Try to load saved token
			LoadSavedToken();
		}

		private void LoadSavedToken()
		{
			try
			{
				_githubToken = _tokenStorage.LoadToken();
				UpdateAuthenticationStatus(_githubToken != null);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error loading saved token: {ex.Message}");
				UpdateAuthenticationStatus(false);
			}
		}

		private void UpdateAuthenticationStatus(bool isAuthenticated)
		{
			if (AuthStatusText != null && AuthButton != null && AnalyzeButton != null)
			{
				if (isAuthenticated)
				{
					AuthStatusText.Text = "Authentication: Logged in";
					AuthButton.Content = "Logout";
					AnalyzeButton.IsEnabled = true;
				}
				else
				{
					AuthStatusText.Text = "Authentication: Not logged in";
					AuthButton.Content = "Login to GitHub";
					AnalyzeButton.IsEnabled = false;
					_githubToken = null;
				}
			}
		}

		private async void OnAuthButtonClick(object? sender, RoutedEventArgs e)
		{
			if (_githubToken != null)
			{
				// Logout
				_githubToken = null;
				_tokenStorage.DeleteToken();
				UpdateAuthenticationStatus(false);
				if (OutputTextBox != null)
				{
					OutputTextBox.Text = "Logged out. Analysis results will appear here...";
				}
				if (CopyButton != null)
				{
					CopyButton.IsEnabled = false;
				}
			}
			else
			{
				// Login using Device Flow
				await AuthenticateWithDeviceFlow();
			}
		}

		private async Task AuthenticateWithDeviceFlow()
		{
			try
			{
				// Cancel any existing authentication
				_authCancellationTokenSource?.Cancel();
				_authCancellationTokenSource = new CancellationTokenSource();

				AuthButton.IsEnabled = false;
				OutputTextBox.Text = "Starting GitHub Device Flow authentication...";

				// Start device flow
				var deviceFlow = await _oauthService.StartDeviceFlow();

				// Show instructions to user
				OutputTextBox.Text = $"Please follow these steps to authenticate:\n\n" +
									$"1. Go to: {deviceFlow.verification_uri}\n" +
									$"2. Enter this code: {deviceFlow.user_code}\n" +
									$"3. Click 'Authorize' to grant access\n\n" +
									$"Waiting for authorization...";

				// Automatically open the verification URL in browser
				try
				{
					Process.Start(new ProcessStartInfo(deviceFlow.verification_uri) { UseShellExecute = true });
				}
				catch
				{
					// If we can't open browser automatically, instructions are already shown
				}

				// Poll for token
				var token = await _oauthService.PollForToken(
					deviceFlow.device_code, 
					deviceFlow.interval, 
					_authCancellationTokenSource.Token);

				// Save token and update UI
				_tokenStorage.SaveToken(token);
				_githubToken = token;
				UpdateAuthenticationStatus(true);
				OutputTextBox.Text = "Successfully authenticated! You can now analyze pull requests.";
			}
			catch (OperationCanceledException)
			{
				OutputTextBox.Text = "Authentication was cancelled.";
				UpdateAuthenticationStatus(false);
			}
			catch (Exception ex)
			{
				if (OutputTextBox != null)
				{
					OutputTextBox.Text = $"Authentication error: {ex.Message}";
				}
				UpdateAuthenticationStatus(false);
			}
			finally
			{
				AuthButton.IsEnabled = true;
			}
		}

		private async void OnAnalyzeButtonClick(object? sender, RoutedEventArgs e)
		{
			if (PrUrlTextBox?.Text == null || string.IsNullOrWhiteSpace(PrUrlTextBox.Text))
			{
				if (OutputTextBox != null)
				{
					OutputTextBox.Text = "Please enter a valid GitHub PR URL.";
				}
				return;
			}

			if (_githubToken == null)
			{
				if (OutputTextBox != null)
				{
					OutputTextBox.Text = "Please authenticate first.";
				}
				return;
			}

			// Parse PR URL
			var prInfo = ParsePullRequestUrl(PrUrlTextBox.Text);
			if (prInfo == null)
			{
				if (OutputTextBox != null)
				{
					OutputTextBox.Text = "Invalid GitHub PR URL format. Expected: https://github.com/owner/repo/pull/123";
				}
				return;
			}

			// Show loading state
			if (AnalyzeButton != null)
			{
				AnalyzeButton.IsEnabled = false;
				AnalyzeButton.Content = "Analyzing...";
			}
			if (OutputTextBox != null)
			{
				OutputTextBox.Text = "Analyzing pull request, please wait...";
			}

			try
			{
				var result = await _analyzer.AnalyzePullRequestAsync(
					prInfo.Value.Owner,
					prInfo.Value.Repository,
					prInfo.Value.PullRequestNumber,
					_githubToken);

				if (OutputTextBox != null)
				{
					OutputTextBox.Text = result;
				}
				if (CopyButton != null)
				{
					CopyButton.IsEnabled = true;
				}
			}
			catch (Exception ex)
			{
				if (OutputTextBox != null)
				{
					OutputTextBox.Text = $"Error analyzing PR: {ex.Message}";
				}
			}
			finally
			{
				if (AnalyzeButton != null)
				{
					AnalyzeButton.IsEnabled = true;
					AnalyzeButton.Content = "Analyze PR";
				}
			}
		}

		private async void OnCopyButtonClick(object? sender, RoutedEventArgs e)
		{
			if (OutputTextBox?.Text != null)
			{
				var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
				if (clipboard != null)
				{
					await clipboard.SetTextAsync(OutputTextBox.Text);

					// Temporarily show feedback
					if (CopyButton != null)
					{
						var originalContent = CopyButton.Content;
						CopyButton.Content = "Copied!";

						// Reset after 2 seconds
						await Task.Delay(2000);
						CopyButton.Content = originalContent;
					}
				}
			}
		}

		private (string Owner, string Repository, int PullRequestNumber)? ParsePullRequestUrl(string url)
		{
			// Match GitHub PR URLs like: https://github.com/owner/repo/pull/123
			var match = Regex.Match(url, @"^https://github\.com/([^/]+)/([^/]+)/pull/(\d+)(?:/.*)?$", RegexOptions.IgnoreCase);

			if (match.Success && int.TryParse(match.Groups[3].Value, out int prNumber))
			{
				return (match.Groups[1].Value, match.Groups[2].Value, prNumber);
			}

			return null;
		}
	}
}