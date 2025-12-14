using OneOf;
using OneOf.Types;

namespace XAct.Core.Users;

public interface IUserService
{
    public ValueTask<IReadOnlyCollection<User>> GetAllUsersAsync();
    public ValueTask<OneOf<User, NotFound>> GetUserByIdAsync(Guid userId);
    public ValueTask<OneOf<User, Error>> AddUserAsync(UserData newUser);
    public ValueTask<OneOf<Success, NotFound>> UpdateUserAsync(Guid userId, UserData userData);
    public ValueTask<OneOf<Success, NotFound>> DeleteUserAsync(Guid userId);

    public sealed record UserData(
        string Username,
        string Email,
        string PasswordHash,
        AccountType AccountType = AccountType.FREE,
        Instant? SubscriptionEndDate = null,
        int TotalWins = 0,
        int TotalGamesPlayed = 0
    );
}

public sealed class UserService(IDataStorage dataStorage) : IUserService
{
    private readonly IDataStorage _dataStorage = dataStorage;

    public async ValueTask<IReadOnlyCollection<User>> GetAllUsersAsync()
    {
        IEnumerable<User> users = await _dataStorage.GetUsersAsync();

        return [.. users];
    }

    public async ValueTask<OneOf<User, NotFound>> GetUserByIdAsync(Guid userId)
    {
        var user = await GetUserById(userId);

        return user is not null ? user : new NotFound();
    }

    public async ValueTask<OneOf<User, Error>> AddUserAsync(IUserService.UserData newUser)
    {
        try
        {
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Username = newUser.Username,
                Email = newUser.Email,
                PasswordHash = newUser.PasswordHash,
                AccountType = newUser.AccountType,
                SubscriptionEndDate = newUser.SubscriptionEndDate,
                TotalWins = newUser.TotalWins,
                TotalGamesPlayed = newUser.TotalGamesPlayed
            };

            await _dataStorage.AddUserAsync(user);

            return user;
        }
        catch (Exception)
        {
            return new Error();
        }
    }

    public async ValueTask<OneOf<Success, NotFound>> UpdateUserAsync(Guid userId, IUserService.UserData userData)
    {
        var user = await GetUserById(userId);

        if (user is null)
        {
            return new NotFound();
        }

        user.Username = userData.Username;
        user.Email = userData.Email;
        user.PasswordHash = userData.PasswordHash;
        user.AccountType = userData.AccountType;
        user.SubscriptionEndDate = userData.SubscriptionEndDate;
        user.TotalWins = userData.TotalWins;
        user.TotalGamesPlayed = userData.TotalGamesPlayed;

        return new Success();
    }

    public async ValueTask<OneOf<Success, NotFound>> DeleteUserAsync(Guid userId)
    {
        var user = await GetUserById(userId);

        if (user is null)
        {
            return new NotFound();
        }

        await _dataStorage.RemoveUserAsync(user);

        return new Success();
    }

    private async ValueTask<User?> GetUserById(Guid userId)
    {
        IEnumerable<User> users = await _dataStorage.GetUsersAsync();

        return users.FirstOrDefault(u => u.UserId == userId);
    }
}
