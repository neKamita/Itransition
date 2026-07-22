namespace Itransition.Models.Cvs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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

    public List<CvLike> Likes { get; set; } = new();

    [NotMapped]
    public int LikesCount => Likes.Count;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public uint Version { get; set; }


}
