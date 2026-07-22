namespace Itransition.Models.Profiles;
using System.ComponentModel.DataAnnotations;
using Itransition.Models;
using Itransition.Models.Attributes;
using Itransition.Models.Cvs;
using System.ComponentModel.DataAnnotations.Schema;

public class CandidateProfile
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }
    [Timestamp]
    public uint Version { get; set; }

    public List<UserAttributeValue> AttributeValues { get; set; } = new();

    public List<ProjectProfile> Projects {get; set;} = new();
    public List<Cv> Cvs { get; set; } = new();

    public required ApplicationUser User { get; set; }

    [NotMapped]
    public string FirstName => GetBuiltInValue(BuiltInAttributeKeys.FirstName) ?? string.Empty;

    [NotMapped]
    public string LastName => GetBuiltInValue(BuiltInAttributeKeys.LastName) ?? string.Empty;

    [NotMapped]
    public string? Location => GetBuiltInValue(BuiltInAttributeKeys.Location);

    [NotMapped]
    public string? PersonalPhotoUrl => GetBuiltInValue(BuiltInAttributeKeys.PersonalPhoto);

    public UserAttributeValue? FindBuiltInValue(string key)
    {
        return AttributeValues.FirstOrDefault(value =>
            string.Equals(value.AttributeDefinition?.BuiltInKey, key, StringComparison.Ordinal));
    }

    private string? GetBuiltInValue(string key)
    {
        return FindBuiltInValue(key)?.Value;
    }
}
