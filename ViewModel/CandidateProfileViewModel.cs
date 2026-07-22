namespace Itransition.ViewModel;

public class CandidateProfileViewModel
{
    public Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? Location { get; set; }
    public string? PersonalPhotoUrl { get; set; }
    public byte[]? RowVersion { get; set; }
}
