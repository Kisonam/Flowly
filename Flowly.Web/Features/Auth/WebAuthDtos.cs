namespace Flowly.Web.Features.Auth;

// Окремі DTO для веб-ендпоїнтів авторизації
public record RegisterDto(string Email, string Password, string FirstName, string LastName);
public record LoginDto(string Email, string Password, bool RememberMe);