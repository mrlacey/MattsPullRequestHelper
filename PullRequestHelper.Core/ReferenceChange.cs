namespace PullRequestHelper.Core.Models;

public class ReferenceChange
{
	public string Name { get; set; } = string.Empty;
	public string? OldVersion { get; set; }
	public string NewVersion { get; set; } = string.Empty;
	public string Type { get; set; } = string.Empty; // PackageReference, ProjectReference, Reference, etc.
	public bool IsNew => string.IsNullOrEmpty(OldVersion) && !string.IsNullOrEmpty(NewVersion);
	public bool IsRemoved => !string.IsNullOrEmpty(OldVersion) && string.IsNullOrEmpty(NewVersion);
	public bool IsUpdated => !string.IsNullOrEmpty(OldVersion) && !string.IsNullOrEmpty(NewVersion);
}
