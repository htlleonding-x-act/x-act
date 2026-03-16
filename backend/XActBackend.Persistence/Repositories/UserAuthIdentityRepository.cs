using Microsoft.EntityFrameworkCore;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Repositories;

/// <summary>
///     Repository for <see cref="UserAuthIdentity"/> entities.
/// </summary>
public interface IUserAuthIdentityRepository
{
    /// <summary>
    ///     Add a new authentication identity for a user.
    /// </summary>
    /// <param name="userId">The id of the user</param>
    /// <param name="provider">The authentication provider</param>
    /// <param name="providerSubject">The provider subject identifier</param>
    /// <param name="passwordHash">Optional password hash</param>
    /// <returns>The created authentication identity entity</returns>
    public UserAuthIdentity AddAuthIdentity(int userId, AuthProvider provider, string providerSubject, string? passwordHash);

    /// <summary>
    ///     Get an authentication identity by provider and provider subject.
    /// </summary>
    /// <param name="provider">The authentication provider</param>
    /// <param name="providerSubject">The provider subject identifier</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>The authentication identity, if found</returns>
    public ValueTask<UserAuthIdentity?> GetByProviderAsync(AuthProvider provider, string providerSubject, bool tracking);

    /// <summary>
    ///     Get all authentication identities for a user.
    /// </summary>
    /// <param name="userId">The id of the user</param>
    /// <param name="tracking">Flag indicating if entities should be tracked by the context</param>
    /// <returns>All authentication identities for the user</returns>
    public ValueTask<IReadOnlyCollection<UserAuthIdentity>> GetByUserIdAsync(int userId, bool tracking);
}

internal sealed class UserAuthIdentityRepository(DbSet<UserAuthIdentity> authIdentitySet) : IUserAuthIdentityRepository
{
    private IQueryable<UserAuthIdentity> AuthIdentities => authIdentitySet;
    private IQueryable<UserAuthIdentity> AuthIdentitiesNoTracking => AuthIdentities.AsNoTracking();

    public UserAuthIdentity AddAuthIdentity(int userId, AuthProvider provider, string providerSubject, string? passwordHash)
    {
        var authIdentity = new UserAuthIdentity
        {
            UserId = userId,
            Provider = provider,
            ProviderSubject = providerSubject,
            PasswordHash = passwordHash,
            CreatedAt = SystemClock.Instance.GetCurrentInstant(),
        };

        authIdentitySet.Add(authIdentity);

        return authIdentity;
    }

    public async ValueTask<UserAuthIdentity?> GetByProviderAsync(AuthProvider provider, string providerSubject, bool tracking)
    {
        IQueryable<UserAuthIdentity> source = tracking ? AuthIdentities : AuthIdentitiesNoTracking;

        return await source.FirstOrDefaultAsync(a => a.Provider == provider && a.ProviderSubject == providerSubject);
    }

    public async ValueTask<IReadOnlyCollection<UserAuthIdentity>> GetByUserIdAsync(int userId, bool tracking)
    {
        IQueryable<UserAuthIdentity> source = tracking ? AuthIdentities : AuthIdentitiesNoTracking;

        List<UserAuthIdentity> authIdentities = await source
            .Where(a => a.UserId == userId)
            .ToListAsync();

        return authIdentities;
    }
}
