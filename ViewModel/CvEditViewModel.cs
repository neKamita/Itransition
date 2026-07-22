using System.ComponentModel.DataAnnotations;

namespace Itransition.ViewModel;

public sealed class CvEditViewModel
{
    public Guid Id { get; set; }

    [Required]
    public string Status { get; set; } = "New";

    public string CandidateName { get; set; } = string.Empty;

    public string PositionTitle { get; set; } = string.Empty;

    public uint Version { get; set; }
}
