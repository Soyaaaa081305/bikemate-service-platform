using BikeMate.Core.DTOs;
using BikeMate.Core.Entities;
using BikeMate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BikeMate.Api.Helpers;

public static class MechanicMappingExtensions
{
    public static MechanicProfileDto ToProfileDto(this Mechanic mechanic)
    {
        return new MechanicProfileDto(
            mechanic.MechanicId,
            mechanic.User!.FirstName + " " + mechanic.User.LastName,
            mechanic.User.ProfileImageUrl,
            mechanic.Bio,
            mechanic.YearsExperience,
            mechanic.IsVerified,
            mechanic.AvailabilityStatus,
            mechanic.AverageRating,
            mechanic.TotalCompletedJobs,
            mechanic.User.AccountStatus,
            mechanic.User.EmailVerified,
            mechanic.User.Email,
            mechanic.User.PhoneNumber,
            mechanic.AddressLine,
            mechanic.Barangay,
            mechanic.City,
            mechanic.Province,
            mechanic.ZipCode,
            mechanic.ValidIdImageUrl,
            mechanic.CertificationImageUrl,
            mechanic.User.FirstName,
            mechanic.MiddleName,
            mechanic.User.LastName,
            mechanic.Sex,
            mechanic.Birthdate);
    }

    public static async Task<int> GetMechanicIdAsync(this BikeMateDbContext db, int userId, CancellationToken cancellationToken)
    {
        return await db.Mechanics.Where(x => x.UserId == userId).Select(x => x.MechanicId).SingleAsync(cancellationToken);
    }

    public static async Task<Mechanic> GetMechanicAsync(this BikeMateDbContext db, int userId, CancellationToken cancellationToken)
    {
        return await db.Mechanics.Include(x => x.User).SingleAsync(x => x.UserId == userId, cancellationToken);
    }
}
