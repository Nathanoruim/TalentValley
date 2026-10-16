using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TalentValley.Api.Domain.Entities;

namespace TalentValley.Api.Services;

public sealed class DuplicateAccountEmailException : Exception;
public enum ResendActivationResult { Accepted, NotFound, AlreadyActivated, DeliveryFailed }

public sealed class AdminAccountService(UserManager<ApplicationUser> users, AccountTokenService tokens,
    ILogger<AdminAccountService> logger)
{
    public async Task EnsureEmailAvailableAsync(string email)
    {
        if (await users.FindByEmailAsync(email) is not null) throw new DuplicateAccountEmailException();
    }

    // The owning student/recruiter service supplies the transaction covering all persistence.
    public async Task<ApplicationUser> CreateAsync(string name, string email, string role)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(), NomeCompleto = name, NomeBusca = NameNormalizer.Normalize(name),
            UserName = email, Email = email, EmailConfirmed = false, CriadoEm = DateTimeOffset.UtcNow
        };
        try
        {
            var result = await users.CreateAsync(user);
            if (result.Errors.Any(x => x.Code is "DuplicateEmail" or "DuplicateUserName"))
                throw new DuplicateAccountEmailException();
            RequireSuccess(result);
            RequireSuccess(await users.AddToRoleAsync(user, role));
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation } postgres &&
            postgres.ConstraintName is "EmailIndex" or "UserNameIndex")
        {
            throw new DuplicateAccountEmailException();
        }
        return user;
    }

    // Rotating the Identity security stamp invalidates every JWT already issued to this account.
    // The owning student/recruiter service supplies the transaction covering all persistence.
    public async Task RevokeSessionsAsync(ApplicationUser user) =>
        RequireSuccess(await users.UpdateSecurityStampAsync(user));

    public static void RequireSuccess(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException("Required Identity persistence failed.");
    }

    public async Task<bool> TrySendActivationAsync(ApplicationUser user)
    {
        try
        {
            await tokens.SendActivationLinkAsync(user);
            return true;
        }
        catch (Exception exception)
        {
            // Provider exception messages can contain the activation URL. Log only the type and account ID.
            logger.LogError("Activation delivery failed for account {UserId} ({FailureType}). Use resend activation to retry.",
                user.Id, exception.GetType().Name);
            return false;
        }
    }

    public async Task<ResendActivationResult> ResendAsync(Guid id)
    {
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null) return ResendActivationResult.NotFound;
        if (user.EmailConfirmed || await users.HasPasswordAsync(user)) return ResendActivationResult.AlreadyActivated;
        return await TrySendActivationAsync(user) ? ResendActivationResult.Accepted : ResendActivationResult.DeliveryFailed;
    }
}
