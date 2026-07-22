using Itransition.Models;

namespace Itransition.Models.Cvs;

public sealed class CvLike
{
    public Guid CvId { get; set; }
    public required Cv Cv { get; set; }

    public required string RecruiterId { get; set; }
    public required ApplicationUser Recruiter { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
