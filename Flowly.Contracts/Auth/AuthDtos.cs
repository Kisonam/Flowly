namespace Flowly.Contracts.Auth;
public record RegisterRequest(string Email, string Password, string FirstName, string LastName);
public record LoginRequest(string Email, string Password, bool RememberMe);
public record AuthResponse(string UserId, string Email);