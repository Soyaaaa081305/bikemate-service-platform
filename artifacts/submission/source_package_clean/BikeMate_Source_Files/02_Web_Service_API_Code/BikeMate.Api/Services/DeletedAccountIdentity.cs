using BikeMate.Core.Entities;
using BikeMate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BikeMate.Api.Services;

public static class DeletedAccountIdentity
{
    public static async Task ReleaseConflictsAsync(
        BikeMateDbContext db,
        string email,
        string? phoneNumber,
        CancellationToken cancellationToken)
    {
        var deletedUsers = await db.Users
            .Include(x => x.AuthProviders)
            .Include(x => x.DeviceTokens)
            .Where(x =>
                x.AccountStatus == "deleted" &&
                (x.Email == email ||
                 (!string.IsNullOrWhiteSpace(phoneNumber) && x.PhoneNumber == phoneNumber)))
            .ToArrayAsync(cancellationToken);

        if (deletedUsers.Length == 0)
        {
            return;
        }

        foreach (var user in deletedUsers)
        {
            Anonymize(user);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public static void Anonymize(User user)
    {
        var now = DateTime.UtcNow;
        var identityMarker = $"{user.UserId}-{Guid.NewGuid():N}";

        user.FirstName = "Deleted";
        user.LastName = "Account";
        user.Email = $"deleted-{identityMarker}@deleted.bikemate.invalid";
        user.PhoneNumber = null;
        user.PasswordHash = null;
        user.ProfileImageUrl = null;
        user.EmailVerified = false;
        user.PhoneVerified = false;
        user.AccountStatus = "deleted";
        user.UpdatedAt = now;

        foreach (var provider in user.AuthProviders)
        {
            provider.ProviderSubject = null;
            provider.ProviderEmail = null;
        }

        foreach (var token in user.DeviceTokens)
        {
            token.IsActive = false;
            token.UpdatedAt = now;
        }
    }
}
