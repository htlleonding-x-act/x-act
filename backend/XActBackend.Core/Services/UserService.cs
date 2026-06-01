using OneOf;
using OneOf.Types;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;

namespace XActBackend.Core.Services;

/// <summary>
///     Provides methods to manage users.
/// </summary>
public interface IUserService
{
    /// <summary>
    ///     Get all users.
    /// </summary>
    /// <param name="tracking">Flag indicating if entities should be tracked by the context</param>
    /// <returns>All users</returns>
    public ValueTask<IReadOnlyCollection<User>> GetAllUsersAsync(bool tracking);

    /// <summary>
    ///     Get a user by id.
    /// </summary>
    /// <param name="userId">The id of the user to find</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>The user, if found</returns>
    public ValueTask<OneOf<User, NotFound>> GetUserByIdAsync(string userId, bool tracking);

    /// <summary>
    ///     Get a user by email address.
    /// </summary>
    /// <param name="email">The email address of the user</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>The user, if found</returns>
    public ValueTask<OneOf<User, NotFound>> GetUserByEmailAsync(string email, bool tracking);

    /// <summary>
    ///     Get a user by username.
    /// </summary>
    /// <param name="username">The username of the user</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>The user, if found</returns>
    public ValueTask<OneOf<User, NotFound>> GetUserByUsernameAsync(string username, bool tracking);

    /// <summary>
    ///     Add a new user.
    /// </summary>
    /// <param name="newUser">The user data to create</param>
    /// <returns>The created user, or an error if the operation fails</returns>
    public ValueTask<OneOf<User, Error>> AddUserAsync(UserData newUser);

    /// <summary>
    ///     Update an existing user.
    /// </summary>
    /// <param name="userId">The id of the user to update</param>
    /// <param name="userData">The new user data</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>Result indicating if the update was successful</returns>
    public ValueTask<OneOf<Success, NotFound>> UpdateUserAsync(string userId, UserData userData, bool tracking);

    /// <summary>
    ///     Delete a user.
    /// </summary>
    /// <param name="userId">The id of the user to delete</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>Result indicating if the deletion was successful</returns>
    public ValueTask<OneOf<Success, NotFound>> DeleteUserAsync(string userId, bool tracking);

    /// <summary>
    ///     Data used to create or update a user.
    /// </summary>
    /// <param name="Username">The username</param>
    /// <param name="Email">The email address</param>
    /// <param name="AccountType">The account type</param>
    /// <param name="SubscriptionEndDate">Optional subscription end date</param>
    /// <param name="TotalWins">Total wins the user has achieved</param>
    /// <param name="TotalGamesPlayed">Total games played by the user</param>
    public sealed record UserData(
        string Username,
        string Email,
        AccountType AccountType = AccountType.Free,
        Instant? SubscriptionEndDate = null,
        int TotalWins = 0,
        int TotalGamesPlayed = 0
    );
}

internal sealed class UserService(IUnitOfWork uow, ILogger<UserService> logger) : IUserService
{
    public async ValueTask<IReadOnlyCollection<User>> GetAllUsersAsync(bool tracking)
    {
        IReadOnlyCollection<User> users = await uow.UserRepository.GetAllUsersAsync(tracking);

        return users;
    }

    public async ValueTask<OneOf<User, NotFound>> GetUserByIdAsync(string userId, bool tracking)
    {
        var user = await uow.UserRepository.GetUserByIdAsync(userId, tracking);

        return user is not null ? user : new NotFound();
    }

    public async ValueTask<OneOf<User, NotFound>> GetUserByEmailAsync(string email, bool tracking)
    {
        var user = await uow.UserRepository.GetUserByEmailAsync(email, tracking);

        return user is not null ? user : new NotFound();
    }

    public async ValueTask<OneOf<User, NotFound>> GetUserByUsernameAsync(string username, bool tracking)
    {
        var user = await uow.UserRepository.GetUserByUsernameAsync(username, tracking);

        return user is not null ? user : new NotFound();
    }

    public async ValueTask<OneOf<User, Error>> AddUserAsync(IUserService.UserData newUser)
    {
        try
        {
            var user = uow.UserRepository.AddUser(
                newUser.Username,
                newUser.Email,
                newUser.AccountType
            );

            user.SubscriptionEndDate = newUser.SubscriptionEndDate;
            user.TotalWins = newUser.TotalWins;
            user.TotalGamesPlayed = newUser.TotalGamesPlayed;

            await uow.SaveChangesAsync();

            return user;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add user {Username} ({Email})", newUser.Username, newUser.Email);
            return new Error();
        }
    }

    public async ValueTask<OneOf<Success, NotFound>> UpdateUserAsync(string userId, IUserService.UserData userData, bool tracking)
    {
        var user = await uow.UserRepository.GetUserByIdAsync(userId, tracking);

        if (user is null)
        {
            return new NotFound();
        }

        user.Username = userData.Username;
        user.Email = userData.Email;
        user.AccountType = userData.AccountType;
        user.SubscriptionEndDate = userData.SubscriptionEndDate;
        user.TotalWins = userData.TotalWins;
        user.TotalGamesPlayed = userData.TotalGamesPlayed;

        await uow.SaveChangesAsync();

        return new Success();
    }

    public async ValueTask<OneOf<Success, NotFound>> DeleteUserAsync(string userId, bool tracking)
    {
        var user = await uow.UserRepository.GetUserByIdAsync(userId, tracking);

        if (user is null)
        {
            return new NotFound();
        }

        user.IsDeleted = true;
        user.DeletedAt = SystemClock.Instance.GetCurrentInstant();
        user.Username = $"deleted_user_{user.Id}";
        user.Email = $"deleted_user_{user.Id}@deleted.local";
        user.SubscriptionEndDate = null;

        await uow.SaveChangesAsync();

        return new Success();
    }
}
