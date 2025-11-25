using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Flowly.Application.DTOs.Auth;
using Flowly.IntegrationTests.Helpers;
using Xunit;

namespace Flowly.IntegrationTests;

/// <summary>
/// Інтеграційні тести для повного Auth flow.
/// Тестуємо реальні HTTP запити через весь стек додатку.
/// </summary>
public class AuthFlowTests : IClassFixture<FlowlyWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FlowlyWebApplicationFactory _factory;

    public AuthFlowTests(FlowlyWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ============================================
    // ТЕСТ 1: Повний Auth Flow - Register → Login → Access Protected Endpoint
    // ============================================
    
    /// <summary>
    /// Тестуємо повний цикл автентифікації:
    /// 1. Реєстрація нового користувача
    /// 2. Логін з отриманням токенів
    /// 3. Доступ до захищеного endpoint з токеном
    /// 
    /// Це перевіряє, що вся система автентифікації працює end-to-end.
    /// </summary>
    [Fact]
    public async Task AuthFlow_RegisterLoginAccessProtected_ShouldWorkEndToEnd()
    {
        // ============================================
        // КРОК 1: Реєстрація нового користувача
        // ============================================
        
        var registerDto = new RegisterDto
        {
            Email = "integration.test@example.com",
            Password = "TestPassword123!",
            DisplayName = "Integration Test User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert - перевіряємо успішну реєстрацію
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK, 
            "реєстрація має бути успішною");

        var registerResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        registerResult.Should().NotBeNull();
        registerResult!.AccessToken.Should().NotBeNullOrEmpty("має бути повернутий access token");
        registerResult.RefreshToken.Should().NotBeNullOrEmpty("має бути повернутий refresh token");
        registerResult.User.Should().NotBeNull();
        registerResult.User.Email.Should().Be(registerDto.Email);

        // ============================================
        // КРОК 2: Логін з тими самими credentials
        // ============================================
        
        var loginDto = new LoginDto
        {
            Email = registerDto.Email,
            Password = registerDto.Password
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert - перевіряємо успішний логін
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "логін має бути успішним");

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        loginResult.Should().NotBeNull();
        loginResult!.AccessToken.Should().NotBeNullOrEmpty();
        loginResult.RefreshToken.Should().NotBeNullOrEmpty();
        loginResult.User.Email.Should().Be(registerDto.Email);

        // ============================================
        // КРОК 3: Доступ до захищеного endpoint
        // ============================================
        
        // Додаємо JWT токен до заголовків
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", loginResult.AccessToken);

        // Намагаємося отримати профіль користувача (захищений endpoint)
        var profileResponse = await _client.GetAsync("/api/auth/me");

        // Assert - перевіряємо успішний доступ
        profileResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "з валідним токеном має бути доступ до захищеного endpoint");

        var profile = await profileResponse.Content.ReadFromJsonAsync<UserProfileDto>();
        profile.Should().NotBeNull();
        profile!.Email.Should().Be(registerDto.Email);
        profile.DisplayName.Should().Be(registerDto.DisplayName);
    }

    // ============================================
    // ТЕСТ 2: Refresh Token Flow
    // ============================================
    
    /// <summary>
    /// Тестуємо механізм оновлення токенів:
    /// 1. Реєстрація та отримання токенів
    /// 2. Використання refresh token для отримання нових токенів
    /// 3. Перевірка, що нові токени працюють
    /// 
    /// Це важливо для безпеки - користувачі можуть залишатися авторизованими
    /// без повторного введення пароля.
    /// </summary>
    [Fact]
    public async Task RefreshTokenFlow_ShouldReturnNewTokens()
    {
        // ============================================
        // КРОК 1: Реєстрація користувача
        // ============================================
        
        var registerDto = new RegisterDto
        {
            Email = "refresh.test@example.com",
            Password = "TestPassword123!",
            DisplayName = "Refresh Test User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var registerResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        var originalAccessToken = registerResult!.AccessToken;
        var originalRefreshToken = registerResult.RefreshToken;

        // ============================================
        // КРОК 2: Використання refresh token
        // ============================================
        
        var refreshDto = new RefreshTokenDto
        {
            AccessToken = originalAccessToken,
            RefreshToken = originalRefreshToken
        };

        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", refreshDto);

        // Assert - перевіряємо успішне оновлення
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "refresh token має працювати");

        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        refreshResult.Should().NotBeNull();
        refreshResult!.AccessToken.Should().NotBeNullOrEmpty();
        refreshResult.RefreshToken.Should().NotBeNullOrEmpty();
        
        // Нові токени мають відрізнятися від старих
        refreshResult.AccessToken.Should().NotBe(originalAccessToken,
            "має бути згенерований новий access token");
        refreshResult.RefreshToken.Should().NotBe(originalRefreshToken,
            "має бути згенерований новий refresh token");

        // ============================================
        // КРОК 3: Перевірка, що новий токен працює
        // ============================================
        
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", refreshResult.AccessToken);

        var profileResponse = await _client.GetAsync("/api/auth/me");
        profileResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "новий access token має працювати");
    }

    // ============================================
    // ТЕСТ 3: Неможливість доступу без токену
    // ============================================
    
    /// <summary>
    /// БЕЗПЕКА: Перевіряємо, що захищені endpoints недоступні без токену.
    /// </summary>
    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ShouldReturn401()
    {
        // Намагаємося отримати доступ без токену
        var response = await _client.GetAsync("/api/auth/me");

        // Assert - має бути 401 Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "без токену доступ має бути заборонений");
    }

    // ============================================
    // ТЕСТ 4: Неможливість доступу з невалідним токеном
    // ============================================
    
    /// <summary>
    /// БЕЗПЕКА: Перевіряємо, що невалідний токен не дає доступу.
    /// </summary>
    [Fact]
    public async Task ProtectedEndpoint_WithInvalidToken_ShouldReturn401()
    {
        // Додаємо невалідний токен
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", "invalid.token.here");

        var response = await _client.GetAsync("/api/auth/me");

        // Assert - має бути 401 Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "з невалідним токеном доступ має бути заборонений");
    }

    // ============================================
    // ТЕСТ 5: Неможливість зареєструватися з існуючим email
    // ============================================
    
    /// <summary>
    /// БЕЗПЕКА: Перевіряємо, що неможливо створити два акаунти з одним email.
    /// </summary>
    [Fact]
    public async Task Register_WithExistingEmail_ShouldReturn400()
    {
        // КРОК 1: Реєструємо першого користувача
        var registerDto = new RegisterDto
        {
            Email = "duplicate@example.com",
            Password = "TestPassword123!",
            DisplayName = "First User"
        };

        var firstResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // КРОК 2: Намагаємося зареєструвати другого з тим самим email
        var duplicateDto = new RegisterDto
        {
            Email = "duplicate@example.com", // Той самий email
            Password = "DifferentPassword123!",
            DisplayName = "Second User"
        };

        var duplicateResponse = await _client.PostAsJsonAsync("/api/auth/register", duplicateDto);

        // Assert - має бути помилка
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "не можна створити два акаунти з одним email");
    }

    // ============================================
    // ТЕСТ 6: Логін з неправильним паролем
    // ============================================
    
    /// <summary>
    /// БЕЗПЕКА: Перевіряємо, що неможливо увійти з неправильним паролем.
    /// </summary>
    [Fact]
    public async Task Login_WithWrongPassword_ShouldReturn401()
    {
        // КРОК 1: Реєструємо користувача
        var registerDto = new RegisterDto
        {
            Email = "wrongpass@example.com",
            Password = "CorrectPassword123!",
            DisplayName = "Test User"
        };

        await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // КРОК 2: Намагаємося увійти з неправильним паролем
        var loginDto = new LoginDto
        {
            Email = registerDto.Email,
            Password = "WrongPassword123!" // Неправильний пароль
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert - має бути 401 Unauthorized
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "з неправильним паролем логін має бути заборонений");
    }
}
