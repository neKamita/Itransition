using System.ComponentModel.DataAnnotations;

namespace Itransition.Models.Attributes;

public class AttributeDefinition
{
    public required Guid Id { get; set; }
    [Required(ErrorMessage = "Please enter a name")]
    [MaxLength(50, ErrorMessage = "Name cannot exceed 100 characters")]
    public required string Name { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Category { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
    public AttributeDataType DataType { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public List<AttributeOption> Options { get; set; } = new();
}


public enum AttributeDataType{

    String,
    MarkdownText,
    ImageReference,
    Numeric,
    Date,
    Period,
    Boolean,
    Dropdown
}
