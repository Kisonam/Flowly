namespace Flowly.Contracts.Profile;

public record UserProfileResponse(
    string UserId,
    string FirstName,
    string LastName,
    string PreferredCulture,
    string? AvatarPath
);

public record UpdateProfileRequest(
    string FirstName,
    string LastName,
    string PreferredCulture,
    string? AvatarPath
);