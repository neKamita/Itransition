namespace Itransition.Models.Cvs;
using System.ComponentModel.DataAnnotations;
using Itransition.Models.Attributes;
using Itransition.Models.Profiles;
using Itransition.Models.Positions;

public class Cv
{
    public Guid Id { get; set; }
    public required Guid CandidateProfileId { get; set; }
    public required CandidateProfile CandidateProfile { get; set; }

    public required Guid PositionId { get; set; }
    public required Position Position { get; set; }

    public required string Status { get; set; }

    public int LikesCount { get; set; } = 0;
    public int DislikesCount { get; set; } = 0;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

    public List<UserAttributeValue> UserAttributeValues { get; set; } = new();

    [Timestamp]
    public byte[]? RowVersion { get; set; }


}
