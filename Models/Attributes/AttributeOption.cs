using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Itransition.Models.Attributes;

public class AttributeOption
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Value { get; set; }

    public Guid AttributeDefinitionId { get; set; }

    [JsonIgnore]
    public AttributeDefinition? AttributeDefinition { get; set; }
}
