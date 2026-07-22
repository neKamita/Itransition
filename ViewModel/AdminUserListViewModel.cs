namespace Itransition.ViewModel;

public class AdminUserListViewModel
{
    public required string Id { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public IReadOnlyList<string> RoleNames { get; set; } = [];
    public required bool IsBlocked { get; set; }

}
