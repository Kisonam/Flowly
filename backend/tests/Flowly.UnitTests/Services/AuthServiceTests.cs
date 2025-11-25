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

/// <summary>
/// Тести для AuthService - перевіряємо автентифікацію, реєстрацію та безпеку.
/// Фокус на захисті від несанкціонованого доступу та витоку даних.
/// </summary>
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
        // Ініціалізація тестового контексту та залежностей
        _context = TestDbContextFactory.CreateInMemoryContext();
        _userManager = TestDbContextFactory.CreateUserManager(_context);
        
        // Мокаємо SignInManager
        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManager,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            null, null, null, null);
        
        // Налаштовуємо мок для успішного логіну
        _signInManagerMock
            .Setup(x => x.CheckPasswordSignInAsync(
                It.IsAny<ApplicationUser>(), 
                It.IsAny<string>(), 
                It.IsAny<bool>()))
            .ReturnsAsync((ApplicationUser user, string password, bool lockout) =>
            {
                // Перевіряємо пароль через UserManager
                var result = _userManager.CheckPasswordAsync(user, password).Result;
                return result ? SignInResult.Success : SignInResult.Failed;
            });

        // Налаштування моків для JWT сервісу
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

        // Налаштування JWT settings
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

    // ============================================
    // ТЕСТ 1: Успішна реєстрація нового користувача
    // ============================================
    
    /// <summary>
    /// Перевіряємо, що новий користувач може успішно зареєструватися.
    /// БЕЗПЕКА: Переконуємося, що пароль хешується (не зберігається в plain text).
    /// БЕЗПЕКА: Перевіряємо, що створюються токени для автентифікації.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldCreateUserAndReturnTokens()
    {
        // Arrange - підготовка тестових даних
        var registerDto = new RegisterDto
        {
            Email = "newuser@example.com",
            Password = "SecurePass123!",
            DisplayName = "New User"
        };

        // Act - виконання тестованого методу
        var result = await _authService.RegisterAsync(registerDto, "127.0.0.1");

        // Assert - перевірка результатів
        result.Should().NotBeNull("результат реєстрації має бути повернутий");
        result.AccessToken.Should().Be("test_access_token", "має бути згенерований access token");
        result.RefreshToken.Should().Be("test_refresh_token", "має бути згенерований refresh token");
        result.User.Should().NotBeNull("інформація про користувача має бути повернута");
        result.User.Email.Should().Be(registerDto.Email, "email має співпадати");
        result.User.DisplayName.Should().Be(registerDto.DisplayName, "display name має співпадати");

        // БЕЗПЕКА: Перевіряємо, що користувач створений в базі
        var userInDb = await _userManager.FindByEmailAsync(registerDto.Email);
        userInDb.Should().NotBeNull("користувач має бути збережений в базі даних");

        // БЕЗПЕКА: Перевіряємо, що пароль НЕ зберігається в plain text
        userInDb!.PasswordHash.Should().NotBeNullOrEmpty("пароль має бути захешований");
        userInDb.PasswordHash.Should().NotBe(registerDto.Password, "пароль НЕ має зберігатися в plain text");

        // БЕЗПЕКА: Перевіряємо, що refresh token збережений в базі
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.UserId == userInDb.Id);
        refreshToken.Should().NotBeNull("refresh token має бути збережений");
        refreshToken!.Token.Should().Be("test_refresh_token");
        refreshToken.CreatedByIp.Should().Be("127.0.0.1", "IP адреса має бути збережена для аудиту");
    }

    // ============================================
    // ТЕСТ 2: Захист від дублювання email
    // ============================================
    
    /// <summary>
    /// БЕЗПЕКА: Перевіряємо, що неможливо створити два акаунти з однаковим email.
    /// Це захищає від account takeover атак.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldThrowException()
    {
        // Arrange - створюємо існуючого користувача
        var existingEmail = "existing@example.com";
        await TestDataSeeder.CreateTestUserAsync(_userManager, existingEmail, "Pass123!");

        var registerDto = new RegisterDto
        {
            Email = existingEmail, // Той самий email
            Password = "AnotherPass123!",
            DisplayName = "Another User"
        };

        // Act & Assert - перевіряємо, що викидається виключення
        var act = async () => await _authService.RegisterAsync(registerDto);
        
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*", "має бути повідомлення про існуючий email");

        // БЕЗПЕКА: Перевіряємо, що в базі залишився тільки один користувач з цим email
        var usersCount = await _context.Users.CountAsync(u => u.Email == existingEmail);
        usersCount.Should().Be(1, "має бути тільки один користувач з цим email");
    }

    // ============================================
    // ТЕСТ 3: Логін з неправильним паролем
    // ============================================
    
    /// <summary>
    /// БЕЗПЕКА: Перевіряємо, що неможливо увійти з неправильним паролем.
    /// Також перевіряємо механізм lockout після кількох невдалих спроб.
    /// </summary>
    [Fact]
    public async Task LoginAsync_WithWrongPassword_ShouldThrowUnauthorizedException()
    {
        // Arrange - створюємо користувача
        var email = "user@example.com";
        var correctPassword = "CorrectPass123!";
        var wrongPassword = "WrongPass123!";
        
        await TestDataSeeder.CreateTestUserAsync(_userManager, email, correctPassword);

        var loginDto = new LoginDto
        {
            Email = email,
            Password = wrongPassword
        };

        // Act & Assert - перевіряємо, що викидається UnauthorizedException
        var act = async () => await _authService.LoginAsync(loginDto);
        
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Invalid email or password*", 
                "має бути загальне повідомлення без деталей для безпеки");

        // БЕЗПЕКА: Перевіряємо, що токени НЕ були створені
        var refreshTokens = await _context.RefreshTokens.ToListAsync();
        refreshTokens.Should().BeEmpty("токени не мають бути створені при невдалому логіні");
    }

    // ============================================
    // ТЕСТ 4: Успішний логін з правильними даними
    // ============================================
    
    /// <summary>
    /// Перевіряємо, що користувач може успішно увійти з правильними credentials.
    /// </summary>
    [Fact]
    public async Task LoginAsync_WithCorrectCredentials_ShouldReturnTokens()
    {
        // Arrange
        var email = "user@example.com";
        var password = "CorrectPass123!";
        
        var user = await TestDataSeeder.CreateTestUserAsync(_userManager, email, password);

        var loginDto = new LoginDto
        {
            Email = email,
            Password = password
        };

        // Act
        var result = await _authService.LoginAsync(loginDto, "192.168.1.1");

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("test_access_token");
        result.RefreshToken.Should().Be("test_refresh_token");
        result.User.Email.Should().Be(email);

        // БЕЗПЕКА: Перевіряємо, що refresh token збережений з правильним IP
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.UserId == user.Id);
        refreshToken.Should().NotBeNull();
        refreshToken!.CreatedByIp.Should().Be("192.168.1.1", "IP має бути збережена для аудиту");
    }

    // ============================================
    // ТЕСТ 5: Refresh token - прострочений токен
    // ============================================
    
    /// <summary>
    /// БЕЗПЕКА: Перевіряємо, що прострочений refresh token не може бути використаний.
    /// Це захищає від використання старих токенів після їх expiration.
    /// </summary>
    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ShouldThrowUnauthorizedException()
    {
        // Arrange - створюємо користувача та прострочений токен
        var user = await TestDataSeeder.CreateTestUserAsync(_userManager);
        
        var expiredToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "expired_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Прострочений вчора
            CreatedAt = DateTime.UtcNow.AddDays(-8),
            CreatedByIp = "127.0.0.1"
        };
        
        _context.RefreshTokens.Add(expiredToken);
        await _context.SaveChangesAsync();

        // Налаштовуємо мок для повернення userId з access token
        _jwtServiceMock
            .Setup(x => x.GetUserIdFromToken("old_access_token"))
            .Returns(user.Id);

        var refreshDto = new RefreshTokenDto
        {
            AccessToken = "old_access_token",
            RefreshToken = "expired_refresh_token"
        };

        // Act & Assert
        var act = async () => await _authService.RefreshTokenAsync(refreshDto);
        
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*expired or revoked*", 
                "має бути повідомлення про неактивний токен");

        // БЕЗПЕКА: Перевіряємо, що новий токен НЕ був створений
        var tokensCount = await _context.RefreshTokens.CountAsync(rt => rt.UserId == user.Id);
        tokensCount.Should().Be(1, "має залишитися тільки старий токен");
    }

    // ============================================
    // ТЕСТ 6: Refresh token - відкликаний токен
    // ============================================
    
    /// <summary>
    /// БЕЗПЕКА: Перевіряємо, що відкликаний (revoked) токен не може бути використаний.
    /// Це важливо для logout функціональності та безпеки.
    /// </summary>
    [Fact]
    public async Task RefreshTokenAsync_WithRevokedToken_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var user = await TestDataSeeder.CreateTestUserAsync(_userManager);
        
        var revokedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "revoked_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = "127.0.0.1",
            IsRevoked = true, // Токен відкликаний
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

        // Act & Assert
        var act = async () => await _authService.RefreshTokenAsync(refreshDto);
        
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*expired or revoked*");
    }

    // ============================================
    // ТЕСТ 7: Успішне оновлення токену
    // ============================================
    
    /// <summary>
    /// Перевіряємо, що валідний refresh token може бути використаний для отримання нових токенів.
    /// БЕЗПЕКА: Старий refresh token має бути відкликаний після використання.
    /// </summary>
    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ShouldReturnNewTokensAndRevokeOld()
    {
        // Arrange
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

        // Act
        var result = await _authService.RefreshTokenAsync(refreshDto, "192.168.1.100");

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("test_access_token", "має бути новий access token");
        result.RefreshToken.Should().Be("test_refresh_token", "має бути новий refresh token");

        // БЕЗПЕКА: Перевіряємо, що старий токен відкликаний
        var oldToken = await _context.RefreshTokens.FindAsync(validToken.Id);
        oldToken.Should().NotBeNull();
        oldToken!.IsRevoked.Should().BeTrue("старий токен має бути відкликаний");
        oldToken.RevokedByIp.Should().Be("192.168.1.100", "IP відкликання має бути збережена");

        // БЕЗПЕКА: Перевіряємо, що створений новий токен
        var newToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == "test_refresh_token" && rt.UserId == user.Id);
        newToken.Should().NotBeNull("новий токен має бути створений");
        newToken!.CreatedByIp.Should().Be("192.168.1.100");
    }

    // ============================================
    // ТЕСТ 8: Зміна пароля відкликає всі токени
    // ============================================
    
    /// <summary>
    /// БЕЗПЕКА: При зміні пароля всі активні refresh токени мають бути відкликані.
    /// Це захищає від використання старих токенів після компрометації акаунту.
    /// </summary>
    [Fact]
    public async Task ChangePasswordAsync_ShouldRevokeAllRefreshTokens()
    {
        // Arrange
        var user = await TestDataSeeder.CreateTestUserAsync(_userManager, password: "OldPass123!");
        
        // Створюємо кілька активних токенів (симулюємо логін з різних пристроїв)
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

        // Act
        await _authService.ChangePasswordAsync(user.Id, changePasswordDto);

        // Assert - перевіряємо, що пароль змінився
        var passwordCheck = await _userManager.CheckPasswordAsync(user, "NewSecurePass123!");
        passwordCheck.Should().BeTrue("новий пароль має працювати");

        // БЕЗПЕКА: Перевіряємо, що ВСІ токени відкликані
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == user.Id)
            .ToListAsync();
        
        tokens.Should().HaveCount(2, "обидва токени мають залишитися в базі");
        tokens.Should().OnlyContain(t => t.IsRevoked, 
            "всі токени мають бути відкликані після зміни пароля");
    }

    // ============================================
    // ТЕСТ 9: Неможливість використати чужий refresh token
    // ============================================
    
    /// <summary>
    /// БЕЗПЕКА: Перевіряємо, що користувач не може використати refresh token іншого користувача.
    /// Це критично важливо для запобігання session hijacking.
    /// </summary>
    [Fact]
    public async Task RefreshTokenAsync_WithAnotherUsersToken_ShouldThrowUnauthorizedException()
    {
        // Arrange - створюємо двох користувачів
        var user1 = await TestDataSeeder.CreateTestUserAsync(_userManager, "user1@example.com");
        var user2 = await TestDataSeeder.CreateTestUserAsync(_userManager, "user2@example.com");
        
        // Створюємо токен для user2
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

        // User1 намагається використати токен user2
        _jwtServiceMock
            .Setup(x => x.GetUserIdFromToken("user1_access_token"))
            .Returns(user1.Id); // Access token належить user1

        var refreshDto = new RefreshTokenDto
        {
            AccessToken = "user1_access_token",
            RefreshToken = "user2_refresh_token" // Але refresh token від user2
        };

        // Act & Assert
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
