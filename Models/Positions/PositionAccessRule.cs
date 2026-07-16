namespace Itransition.Models.Positions;

using Itransition.Models.Attributes;

public class PositionAccessRule
{
    public Guid Id { get; set; }
    public Guid PositionId { get; set; }
    public required Position Position { get; set; }
    public required Guid AttributeDefinitionId { get; set; }
    public required AttributeDefinition AttributeDefinition { get; set; }
    public required string Operator { get; set; }
    public required string TargetValue { get; set; }
}
