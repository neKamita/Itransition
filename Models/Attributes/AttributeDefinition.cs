using System.ComponentModel.DataAnnotations;

namespace Itransition.Models.Attributes;

public class AttributeDefinition
{
    public required Guid Id { get; set; }
    [Required(ErrorMessage = "Please enter a name")]
    [MaxLength(50, ErrorMessage = "Name cannot exceed 100 characters")]
    public required string Name { get; set; }

    public Guid CategoryId { get; set; }
    public AttributeCategory Category { get; set; } = null!;

    [MaxLength(500)]
    public string? Description { get; set; }
    public AttributeDataType DataType { get; set; }

    public bool IsBuiltIn { get; set; }

    [MaxLength(50)]
    public string? BuiltInKey { get; set; }

    public DateTime? LastUsedAt { get; set; }

    [Timestamp]
    public uint Version { get; set; }

    public List<AttributeOption> Options { get; set; } = new();
}

public static class BuiltInAttributeKeys
{
    public const string FirstName = "first_name";
    public const string LastName = "last_name";
    public const string Location = "location";
    public const string PersonalPhoto = "personal_photo";

    public static readonly Guid FirstNameId = Guid.Parse("b1000000-0000-0000-0000-000000000001");
    public static readonly Guid LastNameId = Guid.Parse("b1000000-0000-0000-0000-000000000002");
    public static readonly Guid LocationId = Guid.Parse("b1000000-0000-0000-0000-000000000003");
    public static readonly Guid PersonalPhotoId = Guid.Parse("b1000000-0000-0000-0000-000000000004");
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
