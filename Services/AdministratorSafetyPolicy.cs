namespace Itransition.Services;

public static class AdministratorSafetyPolicy
{
    public static bool ShouldBlockAdministratorRoleRemoval(
        IEnumerable<string> activeAdministratorIds,
        IEnumerable<string> affectedUserIds,
        string currentUserId)
    {
        _ = currentUserId;
        return WouldRemoveLastActiveAdministrator(activeAdministratorIds, affectedUserIds);
    }

    public static bool WouldRemoveLastActiveAdministrator(
        IEnumerable<string> activeAdministratorIds,
        IEnumerable<string> affectedUserIds)
    {
        var activeIds = activeAdministratorIds.ToHashSet(StringComparer.Ordinal);
        if (activeIds.Count == 0)
        {
            return true;
        }

        activeIds.ExceptWith(affectedUserIds);
        return activeIds.Count == 0;
    }
}
