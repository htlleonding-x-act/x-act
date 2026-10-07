using Microsoft.EntityFrameworkCore;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Repositories;

public interface IUserRepository
{
    public User AddUser(string username, string? email, AccountType accountType, bool isGuest, string? id = null);

    public ValueTask<IReadOnlyCollection<User>> GetAllUsersAsync(bool tracking);

    public ValueTask<User?> GetUserByIdAsync(string id, bool tracking);

    public ValueTask<IReadOnlyCollection<User>> GetUsersByIdsAsync(IReadOnlyCollection<string> ids, bool tracking);

    public ValueTask<User?> GetUserByEmailAsync(string email, bool tracking);

    /// <summary>ignores guests and the case of the username</summary>
    public ValueTask<User?> GetRegisteredUserByUsernameAsync(string username, bool tracking);

    public void RemoveUser(User user);
}

internal sealed class UserRepository(DbSet<User> userSet, IClock clock) : IUserRepository
{
    private IQueryable<User> Users => userSet;
    private IQueryable<User> UsersNoTracking => Users.AsNoTracking();

    public User AddUser(string username, string? email, AccountType accountType, bool isGuest, string? id = null)
    {
        var user = new User
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Username = username,
            Email = email,
            AccountType = accountType,
            IsGuest = isGuest,
            CreatedAt = clock.GetCurrentInstant(),
        };

        userSet.Add(user);

        return user;
    }

    public async ValueTask<IReadOnlyCollection<User>> GetAllUsersAsync(bool tracking)
    {
        IQueryable<User> source = tracking ? Users : UsersNoTracking;

        List<User> users = await source.ToListAsync();

        return users;
    }

    public async ValueTask<User?> GetUserByIdAsync(string id, bool tracking)
    {
        IQueryable<User> source = tracking ? Users : UsersNoTracking;

        return await source.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async ValueTask<IReadOnlyCollection<User>> GetUsersByIdsAsync(IReadOnlyCollection<string> ids, bool tracking)
    {
        IQueryable<User> source = tracking ? Users : UsersNoTracking;

        List<User> users = await source.Where(u => ids.Contains(u.Id)).ToListAsync();

        return users;
    }

    public async ValueTask<User?> GetUserByEmailAsync(string email, bool tracking)
    {
        IQueryable<User> source = tracking ? Users : UsersNoTracking;

        return await source.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async ValueTask<User?> GetRegisteredUserByUsernameAsync(string username, bool tracking)
    {
        IQueryable<User> source = tracking ? Users : UsersNoTracking;

        string lowered = username.ToLower();

        return await source.FirstOrDefaultAsync(u => !u.IsGuest && u.Username!.ToLower() == lowered);
    }

    public void RemoveUser(User user)
    {
        userSet.Remove(user);
    }
}
