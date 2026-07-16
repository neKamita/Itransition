namespace Itransition.Models.Profiles;


public class ProjectTechnologyTag
{
    public Guid Id { get; set; }
    public Guid ProjectProfileId { get; set; }
    public required ProjectProfile ProjectProfile { get; set; }
    public required string TagName { get; set; }
}
