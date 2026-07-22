namespace Itransition.ViewModel;

using System.ComponentModel.DataAnnotations;

public class CandidateProfileViewModel
{
    public Guid Id { get; set; }
    [Required]
    [MaxLength(100)]
    public required string FirstName { get; set; }

    [Required]
    [MaxLength(100)]
    public required string LastName { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    [Url]
    [MaxLength(2_000)]
    public string? PersonalPhotoUrl { get; set; }
    public uint Version { get; set; }
    public uint FirstNameVersion { get; set; }
    public uint LastNameVersion { get; set; }
    public uint LocationVersion { get; set; }
    public uint PersonalPhotoVersion { get; set; }
}
