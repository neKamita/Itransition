namespace Itransition.Models.Profiles;
using System.ComponentModel.DataAnnotations;


public class ProjectProfile
{
    public Guid Id { get; set; }
    public required Guid CandidateProfileId { get; set; }
    public required CandidateProfile CandidateProfile { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public List<ProjectTechnologyTag> TechnologyTags { get; set; } = new();

    [Timestamp]
    public uint Version { get; set; }



}
