using System.ComponentModel.DataAnnotations;

namespace Itransition.Models.Attributes;

public sealed class AttributeCategory
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Name { get; set; }

    public int SortOrder { get; set; }

    public List<AttributeDefinition> Attributes { get; set; } = [];
}

public static class AttributeCategoryIds
{
    public static readonly Guid PersonalInformation = Guid.Parse("c1000000-0000-0000-0000-000000000001");
    public static readonly Guid Certification = Guid.Parse("c1000000-0000-0000-0000-000000000002");
    public static readonly Guid DomainKnowledge = Guid.Parse("c1000000-0000-0000-0000-000000000003");
    public static readonly Guid SoftSkills = Guid.Parse("c1000000-0000-0000-0000-000000000004");
    public static readonly Guid Language = Guid.Parse("c1000000-0000-0000-0000-000000000005");
    public static readonly Guid Professional = Guid.Parse("c1000000-0000-0000-0000-000000000006");
    public static readonly Guid Technical = Guid.Parse("c1000000-0000-0000-0000-000000000007");
    public static readonly Guid Other = Guid.Parse("c1000000-0000-0000-0000-000000000008");

    public static IReadOnlyList<AttributeCategory> SeedData =>
    [
        New(PersonalInformation, "Personal Information", 10),
        New(Certification, "Certification", 20),
        New(DomainKnowledge, "Domain Knowledge", 30),
        New(SoftSkills, "Soft Skills", 40),
        New(Language, "Language", 50),
        New(Professional, "Professional", 60),
        New(Technical, "Technical", 70),
        New(Other, "Other", 999)
    ];

    private static AttributeCategory New(Guid id, string name, int sortOrder)
    {
        return new AttributeCategory { Id = id, Name = name, SortOrder = sortOrder };
    }
}
