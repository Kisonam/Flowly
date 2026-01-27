using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Flowly.Application.DTOs.Auth;
using Flowly.IntegrationTests.Helpers;
using Xunit;

namespace Flowly.IntegrationTests;

public class AuthFlowTests : IClassFixture<FlowlyWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FlowlyWebApplicationFactory _factory;
    private readonly System.Text.Json.JsonSerializerOptions _jsonOptions;

    public AuthFlowTests(FlowlyWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        _jsonOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    }

    [Fact]
    public async Task AuthFlow_RegisterLoginAccessProtected_ShouldWorkEndToEnd()
    {

        var registerDto = new RegisterDto
        {
            Email = "integration.test@example.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!",
            DisplayName = "Integration Test User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto, _jsonOptions);

        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK, 
            "реєстрація має бути успішною");

        var registerResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>(_jsonOptions);
        registerResult.Should().NotBeNull();
        registerResult!.AccessToken.Should().NotBeNullOrEmpty("має бути повернутий access token");
        registerResult.RefreshToken.Should().NotBeNullOrEmpty("має бути повернутий refresh token");
        registerResult.User.Should().NotBeNull();
        registerResult.User.Email.Should().Be(registerDto.Email);

        var loginDto = new LoginDto
        {
            Email = registerDto.Email,
            Password = registerDto.Password
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto, _jsonOptions);

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "логін має бути успішним");

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>(_jsonOptions);
        loginResult.Should().NotBeNull();
        loginResult!.AccessToken.Should().NotBeNullOrEmpty();
        loginResult.RefreshToken.Should().NotBeNullOrEmpty();
        loginResult.User.Email.Should().Be(registerDto.Email);

        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", loginResult.AccessToken);

        var profileResponse = await _client.GetAsync("/api/auth/me");

        profileResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "з валідним токеном має бути доступ до захищеного endpoint");

        var profile = await profileResponse.Content.ReadFromJsonAsync<UserProfileDto>(_jsonOptions);
        profile.Should().NotBeNull();
        profile!.Email.Should().Be(registerDto.Email);
        profile.DisplayName.Should().Be(registerDto.DisplayName);
    }

    [Fact]
    public async Task RefreshTokenFlow_ShouldReturnNewTokens()
    {

        var registerDto = new RegisterDto
        {
            Email = "refresh.test@example.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!",
            DisplayName = "Refresh Test User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto, _jsonOptions);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var registerResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>(_jsonOptions);
        var originalAccessToken = registerResult!.AccessToken;
        var originalRefreshToken = registerResult.RefreshToken;

        var refreshDto = new RefreshTokenDto
        {
            AccessToken = originalAccessToken,
            RefreshToken = originalRefreshToken
        };

        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", refreshDto, _jsonOptions);

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "refresh token має працювати");

        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<AuthResponseDto>(_jsonOptions);
        refreshResult.Should().NotBeNull();
        refreshResult!.AccessToken.Should().NotBeNullOrEmpty();
        refreshResult.RefreshToken.Should().NotBeNullOrEmpty();

        refreshResult.AccessToken.Should().NotBe(originalAccessToken,
            "має бути згенерований новий access token");
        refreshResult.RefreshToken.Should().NotBe(originalRefreshToken,
            "має бути згенерований новий refresh token");

        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", refreshResult.AccessToken);

        var profileResponse = await _client.GetAsync("/api/auth/me");
        profileResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "новий access token має працювати");
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ShouldReturn401()
    {
        
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "без токену доступ має бути заборонений");
    }

    [Fact]
    public async Task ProtectedEndpoint_WithInvalidToken_ShouldReturn401()
    {
        
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", "invalid.token.here");

        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "з невалідним токеном доступ має бути заборонений");
    }

    [Fact]
    public async Task Register_WithExistingEmail_ShouldReturn400()
    {
        
        var registerDto = new RegisterDto
        {
            Email = "duplicate@example.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!",
            DisplayName = "First User"
        };

        var firstResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto, _jsonOptions);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var duplicateDto = new RegisterDto
        {
            Email = "duplicate@example.com", 
            Password = "DifferentPassword123!",
            ConfirmPassword = "DifferentPassword123!",
            DisplayName = "Second User"
        };

        var duplicateResponse = await _client.PostAsJsonAsync("/api/auth/register", duplicateDto, _jsonOptions);

        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "не можна створити два акаунти з одним email");
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldReturn401()
    {
        
        var registerDto = new RegisterDto
        {
            Email = "wrongpass@example.com",
            Password = "CorrectPassword123!",
            ConfirmPassword = "CorrectPassword123!",
            DisplayName = "Test User"
        };

        await _client.PostAsJsonAsync("/api/auth/register", registerDto, _jsonOptions);

        var loginDto = new LoginDto
        {
            Email = registerDto.Email,
            Password = "WrongPassword123!" 
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto, _jsonOptions);

        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "з неправильним паролем логін має бути заборонений");
    }
}
