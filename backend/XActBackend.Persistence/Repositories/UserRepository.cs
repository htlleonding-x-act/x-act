using Microsoft.EntityFrameworkCore;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Repositories;

/// <summary>
///     Repository for <see cref="User"/> entities.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    ///     Add a new user.
    /// </summary>
    /// <param name="username">The username</param>
    /// <param name="email">The email address of the user</param>
    /// <param name="accountType">The account type</param>
    /// <returns>The created user entity</returns>
    public User AddUser(string username, string email, AccountType accountType);

    /// <summary>
    ///     Get all users.
    /// </summary>
    /// <param name="tracking">Flag indicating if entities should be tracked by the context</param>
    /// <returns>All users</returns>
    public ValueTask<IReadOnlyCollection<User>> GetAllUsersAsync(bool tracking);

    /// <summary>
    ///     Get a user by id.
    /// </summary>
    /// <param name="id">The id of the user</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>The user, if found</returns>
    public ValueTask<User?> GetUserByIdAsync(int id, bool tracking);

    /// <summary>
    ///     Get a user by email.
    /// </summary>
    /// <param name="email">The email address of the user</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>The user, if found</returns>
    public ValueTask<User?> GetUserByEmailAsync(string email, bool tracking);

    /// <summary>
    ///     Get a user by username.
    /// </summary>
    /// <param name="username">The username</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>The user, if found</returns>
    public ValueTask<User?> GetUserByUsernameAsync(string username, bool tracking);

    /// <summary>
    ///     Remove a user from the repository.
    /// </summary>
    /// <param name="user">The user to remove</param>
    public void RemoveUser(User user);
}

internal sealed class UserRepository(DbSet<User> userSet) : IUserRepository
{
    private IQueryable<User> Users => userSet;
    private IQueryable<User> UsersNoTracking => Users.AsNoTracking();

    public User AddUser(string username, string email, AccountType accountType)
    {
        var user = new User
        {
            Username = username,
            Email = email,
            AccountType = accountType,
            CreatedAt = SystemClock.Instance.GetCurrentInstant(),
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

    public async ValueTask<User?> GetUserByIdAsync(int id, bool tracking)
    {
        IQueryable<User> source = tracking ? Users : UsersNoTracking;

        return await source.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async ValueTask<User?> GetUserByEmailAsync(string email, bool tracking)
    {
        IQueryable<User> source = tracking ? Users : UsersNoTracking;

        return await source.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async ValueTask<User?> GetUserByUsernameAsync(string username, bool tracking)
    {
        IQueryable<User> source = tracking ? Users : UsersNoTracking;

        return await source.FirstOrDefaultAsync(u => u.Username == username);
    }

    public void RemoveUser(User user)
    {
        userSet.Remove(user);
    }
}
