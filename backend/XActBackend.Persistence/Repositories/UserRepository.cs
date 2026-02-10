using Microsoft.EntityFrameworkCore;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Repositories;

public interface IUserRepository
{
    public User AddUser(string username, string email, string passwordHash, AccountType accountType);
    public ValueTask<IReadOnlyCollection<User>> GetAllUsersAsync(bool tracking);
    public ValueTask<User?> GetUserByIdAsync(int id, bool tracking);
    public ValueTask<User?> GetUserByEmailAsync(string email, bool tracking);
    public ValueTask<User?> GetUserByUsernameAsync(string username, bool tracking);
    public void RemoveUser(User user);
}

internal sealed class UserRepository(DbSet<User> userSet) : IUserRepository
{
    private IQueryable<User> Users => userSet;
    private IQueryable<User> UsersNoTracking => Users.AsNoTracking();

    public User AddUser(string username, string email, string passwordHash, AccountType accountType)
    {
        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            AccountType = accountType,
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
