namespace Itransition.Models.Cvs;
using System.ComponentModel.DataAnnotations;
using Itransition.Models.Attributes;
using Itransition.Models.Profiles;
using NpgsqlTypes;

public class UserAttributeValue
{
    public Guid Id { get; set; }
    public Guid CandidateProfileId { get; set; }
    public required CandidateProfile CandidateProfile { get; set; }
    public Guid AttributeDefinitionId { get; set; }
    public required AttributeDefinition AttributeDefinition { get; set; }
    public string? Value { get; set; }

    public NpgsqlTsVector SearchVector { get; set; } = null!;

    [Timestamp]
    public uint Version { get; set; }
}
