using Itransition.Data;
using Itransition.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Itransition.Services;

public sealed class AdministratorBulkActionService
{
    public static readonly IReadOnlySet<string> SupportedRoles =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "Administrator",
            "Recruiter",
            "Candidate"
        };

    private readonly ApplicationDbContext _context;

    public AdministratorBulkActionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdministratorBulkActionResult> ExecuteAsync(
        string action,
        IReadOnlyCollection<string> requestedUserIds,
        string? roleName,
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentUserId);

        var userIds = requestedUserIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (userIds.Count == 0)
        {
            throw new AdministratorBulkActionException("Select at least one user.");
        }

        var users = await _context.Users
            .Where(user => userIds.Contains(user.Id))
            .ToListAsync(cancellationToken);
        if (users.Count != userIds.Count)
        {
            throw new AdministratorBulkActionException("One or more selected users no longer exist.");
        }

        IDbContextTransaction? transaction = null;
        if (_context.Database.IsRelational())
        {
            transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        await using (transaction)
        {
            var result = action switch
            {
                "AssignRole" or "RemoveRole" => await ChangeRoleAsync(
                    action,
                    users,
                    roleName,
                    currentUserId,
                    cancellationToken),
                "Block" or "Unblock" or "Delete" => await ChangeAccountStateAsync(
                    action,
                    users,
                    currentUserId,
                    cancellationToken),
                _ => throw new AdministratorBulkActionException("Unsupported administrator action.")
            };

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }
            }
            catch (DbUpdateConcurrencyException exception)
            {
                throw new AdministratorBulkActionException(
                    "One or more selected users changed. Reload the list and try again.",
                    exception);
            }
            catch (DbUpdateException exception)
            {
                throw new AdministratorBulkActionException(
                    "The requested account changes could not be saved atomically.",
                    exception);
            }

            return result;
        }
    }

    private async Task<AdministratorBulkActionResult> ChangeRoleAsync(
        string action,
        IReadOnlyCollection<ApplicationUser> users,
        string? roleName,
        string currentUserId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(roleName) || !SupportedRoles.Contains(roleName))
        {
            throw new AdministratorBulkActionException("Select a supported role.");
        }

        var role = await _context.Roles
            .SingleOrDefaultAsync(item => item.Name == roleName, cancellationToken)
            ?? throw new AdministratorBulkActionException($"Role '{roleName}' is not configured.");
        var selectedIds = users.Select(user => user.Id).ToList();
        var assignments = await _context.UserRoles
            .Where(item => item.RoleId == role.Id && selectedIds.Contains(item.UserId))
            .ToListAsync(cancellationToken);
        var assignedIds = assignments.Select(item => item.UserId).ToHashSet(StringComparer.Ordinal);

        List<ApplicationUser> changedUsers;
        if (action == "AssignRole")
        {
            changedUsers = users.Where(user => !assignedIds.Contains(user.Id)).ToList();
            _context.UserRoles.AddRange(changedUsers.Select(user => new IdentityUserRole<string>
            {
                UserId = user.Id,
                RoleId = role.Id
            }));
        }
        else
        {
            changedUsers = users.Where(user => assignedIds.Contains(user.Id)).ToList();
            if (roleName == "Administrator")
            {
                var activeAdministratorIds = await GetActiveAdministratorIdsAsync(cancellationToken);
                if (AdministratorSafetyPolicy.ShouldBlockAdministratorRoleRemoval(
                    activeAdministratorIds,
                    changedUsers.Select(user => user.Id),
                    currentUserId))
                {
                    throw new AdministratorBulkActionException(
                        "At least one active administrator account must remain.");
                }
            }

            _context.UserRoles.RemoveRange(assignments);
        }

        RotateAuthenticationState(changedUsers);
        return new AdministratorBulkActionResult(
            changedUsers.Count,
            changedUsers.Any(user => user.Id == currentUserId),
            action == "RemoveRole"
                && roleName == "Administrator"
                && changedUsers.Any(user => user.Id == currentUserId));
    }

    private async Task<AdministratorBulkActionResult> ChangeAccountStateAsync(
        string action,
        IReadOnlyCollection<ApplicationUser> users,
        string currentUserId,
        CancellationToken cancellationToken)
    {
        var changedUsers = users.Where(user => user.Id != currentUserId).ToList();
        if (action is "Block" or "Delete")
        {
            var activeAdministratorIds = await GetActiveAdministratorIdsAsync(cancellationToken);
            if (AdministratorSafetyPolicy.WouldRemoveLastActiveAdministrator(
                activeAdministratorIds,
                changedUsers.Select(user => user.Id)))
            {
                throw new AdministratorBulkActionException(
                    "At least one active administrator account must remain.");
            }
        }

        if (action == "Delete")
        {
            _context.Users.RemoveRange(changedUsers);
        }
        else
        {
            foreach (var user in changedUsers)
            {
                user.LockoutEnd = action == "Block" ? DateTimeOffset.MaxValue : null;
            }

            RotateAuthenticationState(changedUsers);
        }

        return new AdministratorBulkActionResult(changedUsers.Count, false, false);
    }

    private async Task<IReadOnlyList<string>> GetActiveAdministratorIdsAsync(
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return await (
                from userRole in _context.UserRoles
                join role in _context.Roles on userRole.RoleId equals role.Id
                join user in _context.Users on userRole.UserId equals user.Id
                where role.Name == "Administrator"
                    && (user.LockoutEnd == null || user.LockoutEnd <= now)
                select user.Id)
            .ToListAsync(cancellationToken);
    }

    private static void RotateAuthenticationState(IEnumerable<ApplicationUser> users)
    {
        foreach (var user in users)
        {
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.ConcurrencyStamp = Guid.NewGuid().ToString();
        }
    }
}

public sealed record AdministratorBulkActionResult(
    int ChangedUsers,
    bool CurrentUserChanged,
    bool CurrentAdministratorRoleRemoved);

public sealed class AdministratorBulkActionException : Exception
{
    public AdministratorBulkActionException(string message)
        : base(message)
    {
    }

    public AdministratorBulkActionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
