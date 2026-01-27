using FluentAssertions;
using Flowly.Application.Common.Settings;
using Flowly.Application.DTOs.Auth;
using Flowly.Application.DTOs.Common.Settings;
using Flowly.Application.Interfaces;
using Flowly.Domain.Entities;
using Flowly.Infrastructure.Data;
using Flowly.Infrastructure.Identity;
using Flowly.Infrastructure.Services;
using Flowly.UnitTests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Flowly.UnitTests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly IOptions<JwtSettings> _jwtSettings;
    private readonly IOptions<GoogleSettings> _googleSettings;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        
        _context = TestDbContextFactory.CreateInMemoryContext();
        _userManager = TestDbContextFactory.CreateUserManager(_context);

        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManager,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            null, null, null, null);

        _signInManagerMock
            .Setup(x => x.CheckPasswordSignInAsync(
                It.IsAny<ApplicationUser>(), 
                It.IsAny<string>(), 
                It.IsAny<bool>()))
            .ReturnsAsync((ApplicationUser user, string password, bool lockout) =>
            {
                
                var result = _userManager.CheckPasswordAsync(user, password).Result;
                return result ? SignInResult.Success : SignInResult.Failed;
            });

        _jwtServiceMock = new Mock<IJwtService>();
        _jwtServiceMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<IList<string>>()))
            .Returns("test_access_token");
        _jwtServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("test_refresh_token");
        _jwtServiceMock
            .Setup(x => x.GetUserIdFromToken(It.IsAny<string>()))
            .Returns((string token) => token == "valid_token" ? Guid.NewGuid() : null);

        _jwtSettings = Options.Create(new JwtSettings
        {
            Secret = "test_secret_key_for_testing_purposes_only_minimum_32_chars",
            Issuer = "test_issuer",
            Audience = "test_audience",
            AccessTokenExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        });

        _googleSettings = Options.Create(new GoogleSettings
        {
            ClientId = "test_client_id"
        });

        _authService = new AuthService(
            _userManager,
            _signInManagerMock.Object,
            _jwtServiceMock.Object,
            _context,
            _jwtSettings,
            _googleSettings
        );
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldCreateUserAndReturnTokens()
    {
        
        var registerDto = new RegisterDto
        {
            Email = "newuser@example.com",
            Password = "SecurePass123!",
            DisplayName = "New User"
        };

        var result = await _authService.RegisterAsync(registerDto, "127.0.0.1");

        result.Should().NotBeNull("результат реєстрації має бути повернутий");
        result.AccessToken.Should().Be("test_access_token", "має бути згенерований access token");
        result.RefreshToken.Should().Be("test_refresh_token", "має бути згенерований refresh token");
        result.User.Should().NotBeNull("інформація про користувача має бути повернута");
        result.User.Email.Should().Be(registerDto.Email, "email має співпадати");
        result.User.DisplayName.Should().Be(registerDto.DisplayName, "display name має співпадати");

        var userInDb = await _userManager.FindByEmailAsync(registerDto.Email);
        userInDb.Should().NotBeNull("користувач має бути збережений в базі даних");

        userInDb!.PasswordHash.Should().NotBeNullOrEmpty("пароль має бути захешований");
        userInDb.PasswordHash.Should().NotBe(registerDto.Password, "пароль НЕ має зберігатися в plain text");

        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.UserId == userInDb.Id);
        refreshToken.Should().NotBeNull("refresh token має бути збережений");
        refreshToken!.Token.Should().Be("test_refresh_token");
        refreshToken.CreatedByIp.Should().Be("127.0.0.1", "IP адреса має бути збережена для аудиту");
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldThrowException()
    {
        
        var existingEmail = "existing@example.com";
        await TestDataSeeder.CreateTestUserAsync(_userManager, existingEmail, "Pass123!");

        var registerDto = new RegisterDto
        {
            Email = existingEmail, 
            Password = "AnotherPass123!",
            DisplayName = "Another User"
        };

        var act = async () => await _authService.RegisterAsync(registerDto);
        
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*", "має бути повідомлення про існуючий email");

        var usersCount = await _context.Users.CountAsync(u => u.Email == existingEmail);
        usersCount.Should().Be(1, "має бути тільки один користувач з цим email");
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ShouldThrowUnauthorizedException()
    {
        
        var email = "user@example.com";
        var correctPassword = "CorrectPass123!";
        var wrongPassword = "WrongPass123!";
        
        await TestDataSeeder.CreateTestUserAsync(_userManager, email, correctPassword);

        var loginDto = new LoginDto
        {
            Email = email,
            Password = wrongPassword
        };

        var act = async () => await _authService.LoginAsync(loginDto);
        
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Invalid email or password*", 
                "має бути загальне повідомлення без деталей для безпеки");

        var refreshTokens = await _context.RefreshTokens.ToListAsync();
        refreshTokens.Should().BeEmpty("токени не мають бути створені при невдалому логіні");
    }

    [Fact]
    public async Task LoginAsync_WithCorrectCredentials_ShouldReturnTokens()
    {
        
        var email = "user@example.com";
        var password = "CorrectPass123!";
        
        var user = await TestDataSeeder.CreateTestUserAsync(_userManager, email, password);

        var loginDto = new LoginDto
        {
            Email = email,
            Password = password
        };

        var result = await _authService.LoginAsync(loginDto, "192.168.1.1");

        result.Should().NotBeNull();
        result.AccessToken.Should().Be("test_access_token");
        result.RefreshToken.Should().Be("test_refresh_token");
        result.User.Email.Should().Be(email);

        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.UserId == user.Id);
        refreshToken.Should().NotBeNull();
        refreshToken!.CreatedByIp.Should().Be("192.168.1.1", "IP має бути збережена для аудиту");
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ShouldThrowUnauthorizedException()
    {
        
        var user = await TestDataSeeder.CreateTestUserAsync(_userManager);
        
        var expiredToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "expired_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddDays(-1), 
            CreatedAt = DateTime.UtcNow.AddDays(-8),
            CreatedByIp = "127.0.0.1"
        };
        
        _context.RefreshTokens.Add(expiredToken);
        await _context.SaveChangesAsync();

        _jwtServiceMock
            .Setup(x => x.GetUserIdFromToken("old_access_token"))
            .Returns(user.Id);

        var refreshDto = new RefreshTokenDto
        {
            AccessToken = "old_access_token",
            RefreshToken = "expired_refresh_token"
        };

        var act = async () => await _authService.RefreshTokenAsync(refreshDto);
        
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*expired or revoked*", 
                "має бути повідомлення про неактивний токен");

        var tokensCount = await _context.RefreshTokens.CountAsync(rt => rt.UserId == user.Id);
        tokensCount.Should().Be(1, "має залишитися тільки старий токен");
    }

    [Fact]
    public async Task RefreshTokenAsync_WithRevokedToken_ShouldThrowUnauthorizedException()
    {
        
        var user = await TestDataSeeder.CreateTestUserAsync(_userManager);
        
        var revokedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "revoked_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = "127.0.0.1",
            IsRevoked = true, 
            RevokedAt = DateTime.UtcNow.AddHours(-1),
            RevokedByIp = "127.0.0.1"
        };
        
        _context.RefreshTokens.Add(revokedToken);
        await _context.SaveChangesAsync();

        _jwtServiceMock
            .Setup(x => x.GetUserIdFromToken("access_token"))
            .Returns(user.Id);

        var refreshDto = new RefreshTokenDto
        {
            AccessToken = "access_token",
            RefreshToken = "revoked_refresh_token"
        };

        var act = async () => await _authService.RefreshTokenAsync(refreshDto);
        
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*expired or revoked*");
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ShouldReturnNewTokensAndRevokeOld()
    {
        
        var user = await TestDataSeeder.CreateTestUserAsync(_userManager);
        
        var validToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "valid_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = "127.0.0.1"
        };
        
        _context.RefreshTokens.Add(validToken);
        await _context.SaveChangesAsync();

        _jwtServiceMock
            .Setup(x => x.GetUserIdFromToken("current_access_token"))
            .Returns(user.Id);

        var refreshDto = new RefreshTokenDto
        {
            AccessToken = "current_access_token",
            RefreshToken = "valid_refresh_token"
        };

        var result = await _authService.RefreshTokenAsync(refreshDto, "192.168.1.100");

        result.Should().NotBeNull();
        result.AccessToken.Should().Be("test_access_token", "має бути новий access token");
        result.RefreshToken.Should().Be("test_refresh_token", "має бути новий refresh token");

        var oldToken = await _context.RefreshTokens.FindAsync(validToken.Id);
        oldToken.Should().NotBeNull();
        oldToken!.IsRevoked.Should().BeTrue("старий токен має бути відкликаний");
        oldToken.RevokedByIp.Should().Be("192.168.1.100", "IP відкликання має бути збережена");

        var newToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == "test_refresh_token" && rt.UserId == user.Id);
        newToken.Should().NotBeNull("новий токен має бути створений");
        newToken!.CreatedByIp.Should().Be("192.168.1.100");
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldRevokeAllRefreshTokens()
    {
        
        var user = await TestDataSeeder.CreateTestUserAsync(_userManager, password: "OldPass123!");

        var token1 = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "token_device_1",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = "192.168.1.1"
        };
        
        var token2 = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "token_device_2",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = "192.168.1.2"
        };
        
        _context.RefreshTokens.AddRange(token1, token2);
        await _context.SaveChangesAsync();

        var changePasswordDto = new ChangePasswordDto
        {
            CurrentPassword = "OldPass123!",
            NewPassword = "NewSecurePass123!"
        };

        await _authService.ChangePasswordAsync(user.Id, changePasswordDto);

        var passwordCheck = await _userManager.CheckPasswordAsync(user, "NewSecurePass123!");
        passwordCheck.Should().BeTrue("новий пароль має працювати");

        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == user.Id)
            .ToListAsync();
        
        tokens.Should().HaveCount(2, "обидва токени мають залишитися в базі");
        tokens.Should().OnlyContain(t => t.IsRevoked, 
            "всі токени мають бути відкликані після зміни пароля");
    }

    [Fact]
    public async Task RefreshTokenAsync_WithAnotherUsersToken_ShouldThrowUnauthorizedException()
    {
        
        var user1 = await TestDataSeeder.CreateTestUserAsync(_userManager, "user1@example.com");
        var user2 = await TestDataSeeder.CreateTestUserAsync(_userManager, "user2@example.com");

        var user2Token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user2.Id,
            Token = "user2_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = "192.168.1.2"
        };
        
        _context.RefreshTokens.Add(user2Token);
        await _context.SaveChangesAsync();

        _jwtServiceMock
            .Setup(x => x.GetUserIdFromToken("user1_access_token"))
            .Returns(user1.Id); 

        var refreshDto = new RefreshTokenDto
        {
            AccessToken = "user1_access_token",
            RefreshToken = "user2_refresh_token" 
        };

        var act = async () => await _authService.RefreshTokenAsync(refreshDto);
        
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Invalid refresh token*", 
                "має бути відмова, бо токен не належить користувачу");
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
        _userManager.Dispose();
    }
}
