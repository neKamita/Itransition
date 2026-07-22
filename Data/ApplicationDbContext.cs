using Itransition.Models;
using Itransition.Models.Attributes;
using Itransition.Models.Cvs;
using Itransition.Models.Positions;
using Itransition.Models.Profiles;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Itransition.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<CandidateProfile> CandidateProfiles { get; set; }
    public DbSet<AttributeDefinition> AttributeDefinitions { get; set; }
    public DbSet<AttributeCategory> AttributeCategories { get; set; }
    public DbSet<AttributeOption> AttributeOptions { get; set; }
    public DbSet<UserAttributeValue> UserAttributeValues { get; set; }
    public DbSet<Position> Positions { get; set; }
    public DbSet<PositionAttribute> PositionAttributes { get; set; }
    public DbSet<PositionAccessRule> PositionAccessRules { get; set; }
    public DbSet<ProjectProfile> ProjectProfiles { get; set; }
    public DbSet<ProjectTechnologyTag> ProjectTechnologyTags { get; set; }
    public DbSet<Cv> Cvs { get; set; }
    public DbSet<CvLike> CvLikes { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<CandidateProfile>()
            .HasIndex(profile => profile.UserId)
            .IsUnique();

        builder.Entity<CandidateProfile>()
            .Property(profile => profile.Version)
            .IsRowVersion();

        builder.Entity<AttributeCategory>()
            .HasIndex(category => category.Name)
            .IsUnique();

        builder.Entity<AttributeCategory>().HasData(AttributeCategoryIds.SeedData);

        builder.Entity<AttributeDefinition>()
            .HasIndex(attribute => attribute.Name)
            .IsUnique();

        builder.Entity<AttributeDefinition>()
            .HasIndex(attribute => attribute.BuiltInKey)
            .IsUnique()
            .HasFilter("\"BuiltInKey\" IS NOT NULL");

        builder.Entity<AttributeDefinition>()
            .HasOne(attribute => attribute.Category)
            .WithMany(category => category.Attributes)
            .HasForeignKey(attribute => attribute.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<AttributeDefinition>()
            .Property(attribute => attribute.Version)
            .IsRowVersion();

        builder.Entity<AttributeDefinition>().HasData(
            BuiltInDefinition(
                BuiltInAttributeKeys.FirstNameId,
                "First Name",
                BuiltInAttributeKeys.FirstName,
                AttributeDataType.String),
            BuiltInDefinition(
                BuiltInAttributeKeys.LastNameId,
                "Last Name",
                BuiltInAttributeKeys.LastName,
                AttributeDataType.String),
            BuiltInDefinition(
                BuiltInAttributeKeys.LocationId,
                "Location",
                BuiltInAttributeKeys.Location,
                AttributeDataType.String),
            BuiltInDefinition(
                BuiltInAttributeKeys.PersonalPhotoId,
                "Personal Photo",
                BuiltInAttributeKeys.PersonalPhoto,
                AttributeDataType.ImageReference));

        builder.Entity<AttributeOption>()
            .HasIndex(option => new { option.AttributeDefinitionId, option.Value })
            .IsUnique();

        builder.Entity<AttributeOption>()
            .Property(option => option.Version)
            .IsRowVersion();

        builder.Entity<UserAttributeValue>()
            .HasIndex(value => new { value.CandidateProfileId, value.AttributeDefinitionId })
            .IsUnique();

        builder.Entity<UserAttributeValue>()
            .Property(value => value.Version)
            .IsRowVersion();

        builder.Entity<UserAttributeValue>()
            .HasGeneratedTsVectorColumn(
                value => value.SearchVector,
                "english",
                value => new { value.Value })
            .HasIndex(value => value.SearchVector)
            .HasMethod("GIN");

        builder.Entity<Cv>()
            .HasIndex(cv => new { cv.CandidateProfileId, cv.PositionId })
            .IsUnique();

        builder.Entity<Cv>()
            .Property(cv => cv.Version)
            .IsRowVersion();

        builder.Entity<PositionAttribute>()
            .HasIndex(attribute => new { attribute.PositionId, attribute.AttributeDefinitionId })
            .IsUnique();

        builder.Entity<PositionAttribute>()
            .Property(attribute => attribute.Version)
            .IsRowVersion();

        builder.Entity<PositionAccessRule>()
            .HasIndex(rule => new
            {
                rule.PositionId,
                rule.AttributeDefinitionId,
                rule.Operator,
                rule.TargetValue
            })
            .IsUnique();

        builder.Entity<PositionAccessRule>()
            .Property(rule => rule.Version)
            .IsRowVersion();

        builder.Entity<Position>()
            .Property(position => position.Version)
            .IsRowVersion();

        builder.Entity<Position>()
            .HasGeneratedTsVectorColumn(
                position => position.SearchVector,
                "english",
                position => new
                {
                    position.Title,
                    position.Description,
                    position.Company,
                    position.Level,
                    position.Tags
                })
            .HasIndex(position => position.SearchVector)
            .HasMethod("GIN");

        builder.Entity<ProjectProfile>()
            .Property(project => project.Version)
            .IsRowVersion();

        builder.Entity<CvLike>(entity =>
        {
            entity.HasKey(like => new { like.CvId, like.RecruiterId });
            entity.HasOne(like => like.Cv)
                .WithMany(cv => cv.Likes)
                .HasForeignKey(like => like.CvId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(like => like.Recruiter)
                .WithMany()
                .HasForeignKey(like => like.RecruiterId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static AttributeDefinition BuiltInDefinition(
        Guid id,
        string name,
        string key,
        AttributeDataType dataType)
    {
        return new AttributeDefinition
        {
            Id = id,
            Name = name,
            CategoryId = AttributeCategoryIds.PersonalInformation,
            Category = null!,
            Description = "Required profile field shared with position templates.",
            DataType = dataType,
            IsBuiltIn = true,
            BuiltInKey = key
        };
    }
}
