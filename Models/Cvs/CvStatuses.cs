namespace Itransition.Models.Cvs;

public static class CvStatuses
{
    public const string New = "New";
    public const string Draft = "Draft";
    public const string Published = "Published";

    public static bool IsEditable(string? status)
    {
        return string.Equals(status, New, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, Draft, StringComparison.OrdinalIgnoreCase);
    }
}
