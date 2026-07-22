namespace Itransition.Models.Positions;
using System.ComponentModel.DataAnnotations;
using NpgsqlTypes;

public class Position
{
    public Guid Id { get; set; }
    [Display(Name = "Job Title")]
    public required string Title { get; set; }
    public string? Description { get; set; }

    [Display(Name = "Technology Tags (comma separated)")]
    public string? Tags { get; set; }

    [Display(Name = "Company")]
    public required string Company { get; set; }

    [Display(Name = "Level")]
    public required string Level { get; set; }

    [Display(Name = "Max Projects / CV")]
    [Range(0, 100, ErrorMessage = "Max projects must be between 0 and 100.")]
    public required int MaxProjectInCv { get; set; }

    [Display(Name = "Visibility")]
    public required bool IsPublic { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Display(Name = "Last Updated")]
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

    public List<PositionAttribute> PositionRequiredAttributes { get; set; } = new();
    public List<PositionAccessRule> PositionAccessRules { get; set; } = new();

    public NpgsqlTsVector SearchVector { get; set; } = null!;

    [Timestamp]
    public uint Version { get; set; }
}
