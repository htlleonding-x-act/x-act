using OneOf;
using OneOf.Types;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;

namespace XActBackend.Core.Services;

public interface IUserService
{
    public ValueTask<IReadOnlyCollection<User>> GetAllUsersAsync(bool tracking);

    public ValueTask<OneOf<User, NotFound>> GetUserByIdAsync(string userId, bool tracking);

    public ValueTask<OneOf<User, NotFound>> GetUserByEmailAsync(string email, bool tracking);

    /// <summary>creates a guest, whose name only has to differ from the registered usernames</summary>
    public ValueTask<OneOf<User, DomainError, Error>> AddUserAsync(UserData newUser);

    public ValueTask<OneOf<Success, NotFound>> UpdateUserAsync(string userId, UserData userData, bool tracking);

    /// <summary>changes what a user may edit about themselves; the username has to stay unique among registered users</summary>
    public ValueTask<OneOf<Success, NotFound, DomainError>> UpdateProfileAsync(string userId, string username);

    /// <summary>soft delete: flags the user and replaces username and email with placeholders</summary>
    public ValueTask<OneOf<Success, NotFound>> DeleteUserAsync(string userId, bool tracking);

    /// <summary>finds the user linked to the keycloak subject, creates user and identity on first login and restores a deleted user</summary>
    public ValueTask<OneOf<User, Error>> GetOrCreateByKeycloakSubjectAsync(string keycloakSubject, string username, string? email);

    public sealed record UserData(
        string Username,
        string? Email,
        AccountType AccountType = AccountType.Free,
        Instant? SubscriptionEndDate = null,
        int TotalWins = 0,
        int TotalGamesPlayed = 0
    );
}

internal sealed class UserService(IUnitOfWork uow, IClock clock, ILogger<UserService> logger) : IUserService
{
    // column length of User.Email in DatabaseContext
    private const int MaxEmailLength = 100;

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

    public async ValueTask<OneOf<User, Error>> GetOrCreateByKeycloakSubjectAsync(string keycloakSubject, string username, string? email)
    {
        try
        {
            var existingIdentity = await uow.UserAuthIdentityRepository.GetBySubjectAsync(keycloakSubject, tracking: false);
            if (existingIdentity is not null)
            {
                var existingUser = await uow.UserRepository.GetUserByIdAsync(existingIdentity.UserId, tracking: true);
                if (existingUser is null)
                {
                    logger.LogError("UserAuthIdentity for subject {Subject} references missing user {UserId}",
                                    keycloakSubject, existingIdentity.UserId);
                    return new Error();
                }

                // the user id is the keycloak subject, so a deleted user can never be replaced by a new one
                if (existingUser.IsDeleted)
                {
                    existingUser.IsDeleted = false;
                    existingUser.DeletedAt = null;
                    existingUser.Username = username;
                    existingUser.Email = await FreeEmailOrNullAsync(email);
                    await uow.SaveChangesAsync();
                }

                return existingUser;
            }

            var newUser = uow.UserRepository.AddUser(
                username,
                await FreeEmailOrNullAsync(email),
                AccountType.Free,
                isGuest: false,
                id: keycloakSubject
            );
            uow.UserAuthIdentityRepository.AddAuthIdentity(newUser.Id, keycloakSubject);
            await uow.SaveChangesAsync();

            return newUser;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get or create user for Keycloak subject {Subject}", keycloakSubject);
            return new Error();
        }
    }

    /// <summary>the email is optional, so a missing, too long or already used one is dropped instead of failing the login</summary>
    private async ValueTask<string?> FreeEmailOrNullAsync(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > MaxEmailLength)
        {
            return null;
        }

        var userWithEmail = await uow.UserRepository.GetUserByEmailAsync(email, tracking: false);

        return userWithEmail is null ? email : null;
    }

    public async ValueTask<OneOf<User, DomainError, Error>> AddUserAsync(IUserService.UserData newUser)
    {
        try
        {
            var registeredUser = await uow.UserRepository.GetRegisteredUserByUsernameAsync(newUser.Username, tracking: false);
            if (registeredUser is not null)
            {
                logger.LogWarning("Rejected user creation because username {Username} is already taken", newUser.Username);
                return DomainError.UsernameTaken(newUser.Username);
            }

            var user = uow.UserRepository.AddUser(
                newUser.Username,
                newUser.Email,
                newUser.AccountType,
                isGuest: true
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

    public async ValueTask<OneOf<Success, NotFound, DomainError>> UpdateProfileAsync(string userId, string username)
    {
        var user = await uow.UserRepository.GetUserByIdAsync(userId, tracking: true);

        if (user is null)
        {
            return new NotFound();
        }

        var registeredUser = await uow.UserRepository.GetRegisteredUserByUsernameAsync(username, tracking: false);
        if (registeredUser is not null && registeredUser.Id != userId)
        {
            logger.LogWarning("Rejected rename of user {UserId} because username {Username} is already taken", userId, username);
            return DomainError.UsernameTaken(username);
        }

        user.Username = username;

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
        user.DeletedAt = clock.GetCurrentInstant();
        user.Username = $"deleted_user_{user.Id}";
        user.Email = $"deleted_user_{user.Id}@deleted.local";
        user.SubscriptionEndDate = null;

        await uow.SaveChangesAsync();

        return new Success();
    }
}
