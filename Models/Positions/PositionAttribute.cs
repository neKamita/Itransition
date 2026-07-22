namespace Itransition.Models.Positions;
using System.ComponentModel.DataAnnotations;
using Itransition.Models.Attributes;

public class PositionAttribute
{
    public Guid Id { get; set; }
    public Guid PositionId { get; set; }
    public required Position Position { get; set; }
    public required Guid AttributeDefinitionId { get; set; }
    public required AttributeDefinition AttributeDefinition { get; set; }
    [Range(0, 1000)]
    public int OrderIndex { get; set; }

    [Timestamp]
    public uint Version { get; set; }
}
