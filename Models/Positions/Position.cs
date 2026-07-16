namespace Itransition.Models.Positions;
using System.ComponentModel.DataAnnotations;

public class Position
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required string Company { get; set; }
    public required string Level { get; set; }
    public required int MaxProjectInCv { get; set; }
    public required bool IsPublic { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    public List<PositionAttribute> PositionRequiredAttributes { get; set; } = new();
    public List<PositionAccessRule> PositionAccessRules { get; set; } = new();

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
