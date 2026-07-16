using Itransition.Models;
using Itransition.Models.Attributes;
using Itransition.Models.Profiles;
using Itransition.Models.Positions;
using Itransition.Models.Cvs;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Itransition.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) :  base(options)
    {
    }
    public DbSet<CandidateProfile> CandidateProfiles { get; set; }
    public DbSet<AttributeDefinition> AttributeDefinitions { get; set; }
    public DbSet<AttributeOption> AttributeOptions { get; set; }
    public DbSet<UserAttributeValue> UserAttributeValues { get; set; }
    public DbSet<Position> Positions { get; set; }
    public DbSet<PositionAttribute> PositionAttributes { get; set; }
    public DbSet<PositionAccessRule> PositionAccessRules { get; set; }
    public DbSet<ProjectProfile> ProjectProfiles { get; set; }
    public DbSet<ProjectTechnologyTag> ProjectTechnologyTags { get; set; }
    public DbSet<Cv> Cvs { get; set; }




}
