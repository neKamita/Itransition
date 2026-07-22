namespace Itransition.Models.Profiles;
using System.ComponentModel.DataAnnotations;
using Itransition.Models;
using Itransition.Models.Attributes;
using Itransition.Models.Cvs;

public class CandidateProfile
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Location { get; set; }
    public string? PersonalPhotoUrl { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public List<UserAttributeValue> AttributeValues { get; set; } = new();

    public List<ProjectProfile> Projects {get; set;} = new();
    public List<Cv> Cvs { get; set; } = new();

    public required ApplicationUser User { get; set; }
}
