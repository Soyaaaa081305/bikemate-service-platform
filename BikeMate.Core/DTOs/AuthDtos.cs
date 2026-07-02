namespace BikeMate.Core.DTOs;

public sealed record RegisterRequestDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string ConfirmPassword,
    string? PhoneNumber,
    string Role,
    DateTime? Birthdate = null);

public sealed record LoginRequestDto(
    string Email,
    string Password);

public sealed record AuthAvailabilityDto(
    bool EmailAvailable,
    bool PhoneAvailable);

public sealed record AuthResponseDto(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    UserProfileDto User);

public sealed record UserProfileDto(
    int UserId,
    string FirstName,
    string LastName,
    string Email,
    bool EmailVerified,
    string AccountStatus,
    IReadOnlyCollection<string> Roles);
