using System;
using System.IO;
using System.Text;

namespace PullRequestHelper.Desktop.Services
{
	public class TokenStorage
	{
		private static readonly string TokenFilePath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			"PullRequestHelper",
			"token.dat"
		);

		public void SaveToken(string token)
		{
			try
			{
				var directory = Path.GetDirectoryName(TokenFilePath);
				if (directory != null && !Directory.Exists(directory))
				{
					Directory.CreateDirectory(directory);
				}

				// Simple encoding (not fully secure, but better than plain text)
				// In a production app, you'd want to use proper encryption or OS keychain
				var encodedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(token));
				File.WriteAllText(TokenFilePath, encodedToken);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to save token: {ex.Message}");
				throw;
			}
		}

		public string? LoadToken()
		{
			try
			{
				if (!File.Exists(TokenFilePath))
					return null;

				var encodedToken = File.ReadAllText(TokenFilePath);
				var bytes = Convert.FromBase64String(encodedToken);
				return Encoding.UTF8.GetString(bytes);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to load token: {ex.Message}");
				return null;
			}
		}

		public void DeleteToken()
		{
			try
			{
				if (File.Exists(TokenFilePath))
				{
					File.Delete(TokenFilePath);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to delete token: {ex.Message}");
			}
		}
	}
}
