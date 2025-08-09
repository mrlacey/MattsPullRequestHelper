using System.Text;

namespace PullRequestHelper.Core;

public class GitHubService
{
	public async Task PostToPullRequestAsync(string repository, int pullRequestNumber, string message, string githubToken)
	{
		var apiUrl = $"https://api.github.com/repos/{repository}/issues/{pullRequestNumber}/comments";

		using (var client = new HttpClient())
		{
			client.DefaultRequestHeaders.Add("Authorization", $"Bearer {githubToken}");
			client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
			client.DefaultRequestHeaders.Add("User-Agent", "MattsPullRequestHelper");

			var contentBody = $"{{\"body\": \"<b>PullRequestHelper:</b><br /><br />{message.Replace("\n", "<br />")}\"}}";

			var content = new StringContent(contentBody, Encoding.UTF8, "application/json");

			var response = await client.PostAsync(apiUrl, content);

			if (response.IsSuccessStatusCode)
			{
				Console.WriteLine("Successfully posted to PR conversation.");
			}
			else
			{
				throw new Exception($"Failed to post to PR conversation. Status: {response.StatusCode}, Message: {await response.Content.ReadAsStringAsync()}");
			}
		}
	}
}