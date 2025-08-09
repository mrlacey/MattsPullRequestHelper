namespace PullRequestHelper.Core.Models;

public class ProjectReference
{
	public string Name { get; set; } = string.Empty;
	public string Version { get; set; } = string.Empty;
	public string Type { get; set; } = string.Empty; // PackageReference, ProjectReference, Reference, etc.
}
