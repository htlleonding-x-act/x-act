using Microsoft.EntityFrameworkCore;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Repositories;

public interface IUserAuthIdentityRepository
{
    public UserAuthIdentity AddAuthIdentity(int userId, string providerSubject);

    public ValueTask<UserAuthIdentity?> GetBySubjectAsync(string providerSubject, bool tracking);

    public ValueTask<IReadOnlyCollection<UserAuthIdentity>> GetByUserIdAsync(int userId, bool tracking);
}

internal sealed class UserAuthIdentityRepository(DbSet<UserAuthIdentity> authIdentitySet, IClock clock) : IUserAuthIdentityRepository
{
    private IQueryable<UserAuthIdentity> AuthIdentities => authIdentitySet;
    private IQueryable<UserAuthIdentity> AuthIdentitiesNoTracking => AuthIdentities.AsNoTracking();

    public UserAuthIdentity AddAuthIdentity(int userId, string providerSubject)
    {
        var authIdentity = new UserAuthIdentity
        {
            UserId = userId,
            ProviderSubject = providerSubject,
            CreatedAt = clock.GetCurrentInstant(),
        };

        authIdentitySet.Add(authIdentity);

        return authIdentity;
    }

    public async ValueTask<UserAuthIdentity?> GetBySubjectAsync(string providerSubject, bool tracking)
    {
        IQueryable<UserAuthIdentity> source = tracking ? AuthIdentities : AuthIdentitiesNoTracking;

        return await source.FirstOrDefaultAsync(a => a.ProviderSubject == providerSubject);
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
