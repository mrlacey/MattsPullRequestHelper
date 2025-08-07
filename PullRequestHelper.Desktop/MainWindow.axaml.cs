using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using PullRequestHelper.Core;
using PullRequestHelper.Desktop.Services;

namespace PullRequestHelper.Desktop
{
	public partial class MainWindow : Window
	{
		private readonly PullRequestAnalyzer _analyzer;
		private readonly TokenStorage _tokenStorage;
		private string? _githubToken;
		
		public MainWindow()
		{
			InitializeComponent();
			_analyzer = new PullRequestAnalyzer();
			_tokenStorage = new TokenStorage();
			
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
				// Login - for now, show a simple input dialog
				// TODO: Implement proper OAuth flow
				await ShowTokenInputDialog();
			}
		}

		private async Task ShowTokenInputDialog()
		{
			var tokenDialog = new Window
			{
				Title = "GitHub Personal Access Token",
				Width = 500,
				Height = 200,
				WindowStartupLocation = WindowStartupLocation.CenterOwner
			};

			var stackPanel = new StackPanel { Margin = new Avalonia.Thickness(20) };
			
			stackPanel.Children.Add(new TextBlock 
			{ 
				Text = "Enter your GitHub Personal Access Token:",
				Margin = new Avalonia.Thickness(0, 0, 0, 10)
			});
			
			var tokenTextBox = new TextBox 
			{ 
				Watermark = "ghp_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
				Margin = new Avalonia.Thickness(0, 0, 0, 15),
				PasswordChar = '*'
			};
			stackPanel.Children.Add(tokenTextBox);

			var buttonPanel = new StackPanel 
			{ 
				Orientation = Avalonia.Layout.Orientation.Horizontal,
				HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
				Spacing = 10
			};

			var okButton = new Button { Content = "OK", Padding = new Avalonia.Thickness(15, 5) };
			var cancelButton = new Button { Content = "Cancel", Padding = new Avalonia.Thickness(15, 5) };

			buttonPanel.Children.Add(okButton);
			buttonPanel.Children.Add(cancelButton);
			stackPanel.Children.Add(buttonPanel);

			tokenDialog.Content = stackPanel;

			bool? result = null;
			okButton.Click += (s, e) => { result = true; tokenDialog.Close(); };
			cancelButton.Click += (s, e) => { result = false; tokenDialog.Close(); };

			await tokenDialog.ShowDialog(this);

			if (result == true && !string.IsNullOrWhiteSpace(tokenTextBox.Text))
			{
				_githubToken = tokenTextBox.Text.Trim();
				
				// Save token securely
				try
				{
					_tokenStorage.SaveToken(_githubToken);
					UpdateAuthenticationStatus(true);
					if (OutputTextBox != null)
					{
						OutputTextBox.Text = "Authenticated successfully. Enter a PR URL and click 'Analyze PR' to get started.";
					}
				}
				catch (Exception ex)
				{
					if (OutputTextBox != null)
					{
						OutputTextBox.Text = $"Error saving token: {ex.Message}";
					}
					_githubToken = null;
					UpdateAuthenticationStatus(false);
				}
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