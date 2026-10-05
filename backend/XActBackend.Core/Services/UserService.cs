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

    public ValueTask<OneOf<User, NotFound>> GetUserByUsernameAsync(string username, bool tracking);

    public ValueTask<OneOf<User, DomainError, Error>> AddUserAsync(UserData newUser);

    public ValueTask<OneOf<Success, NotFound>> UpdateUserAsync(string userId, UserData userData, bool tracking);

    /// <summary>soft delete: flags the user and replaces username and email with placeholders</summary>
    public ValueTask<OneOf<Success, NotFound>> DeleteUserAsync(string userId, bool tracking);

    /// <summary>finds the user linked to the keycloak subject, creates user and identity on first login and restores a deleted user</summary>
    public ValueTask<OneOf<User, Error>> GetOrCreateByKeycloakSubjectAsync(string keycloakSubject, string username, string? email);

    public sealed record UserData(
        string Username,
        string Email,
        AccountType AccountType = AccountType.Free,
        Instant? SubscriptionEndDate = null,
        int TotalWins = 0,
        int TotalGamesPlayed = 0
    );
}

internal sealed class UserService(IUnitOfWork uow, IClock clock, ILogger<UserService> logger) : IUserService
{
    // column lengths of User.Username and User.Email in DatabaseContext
    private const int MaxUsernameLength = 50;
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

    public async ValueTask<OneOf<User, NotFound>> GetUserByUsernameAsync(string username, bool tracking)
    {
        var user = await uow.UserRepository.GetUserByUsernameAsync(username, tracking);

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
                    existingUser.Username = await FindFreeUsernameAsync(username);
                    existingUser.Email = await FreeEmailOrNullAsync(email);
                    await uow.SaveChangesAsync();
                }

                return existingUser;
            }

            var newUser = uow.UserRepository.AddUser(
                await FindFreeUsernameAsync(username),
                await FreeEmailOrNullAsync(email),
                AccountType.Free,
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

    /// <summary>guests can take any free username, so a clashing keycloak username gets a number appended like guest names do</summary>
    private async ValueTask<string> FindFreeUsernameAsync(string username)
    {
        string candidate = Truncate(username, MaxUsernameLength);

        for (int suffix = 2; await uow.UserRepository.GetUserByUsernameAsync(candidate, tracking: false) is not null; suffix++)
        {
            string suffixText = $" {suffix}";
            candidate = Truncate(username, MaxUsernameLength - suffixText.Length) + suffixText;
        }

        return candidate;
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

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    public async ValueTask<OneOf<User, DomainError, Error>> AddUserAsync(IUserService.UserData newUser)
    {
        try
        {
            var userWithUsername = await uow.UserRepository.GetUserByUsernameAsync(newUser.Username, tracking: false);
            if (userWithUsername is not null)
            {
                logger.LogWarning("Rejected user creation because username {Username} is already taken", newUser.Username);
                return DomainError.UsernameTaken(newUser.Username);
            }

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
        user.DeletedAt = clock.GetCurrentInstant();
        user.Username = $"deleted_user_{user.Id}";
        user.Email = $"deleted_user_{user.Id}@deleted.local";
        user.SubscriptionEndDate = null;

        await uow.SaveChangesAsync();

        return new Success();
    }
}
