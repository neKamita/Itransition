namespace Itransition.Services;

public sealed class CloudUploadOptions
{
    public const string SectionName = "CloudUpload";

    public string CloudName { get; init; } = string.Empty;
    public string UploadPreset { get; init; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(CloudName)
        && !string.IsNullOrWhiteSpace(UploadPreset);
}
