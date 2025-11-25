using FluentAssertions;
using Flowly.Application.DTOs.Transactions;
using Flowly.Application.Interfaces;
using Flowly.Domain.Enums;
using Flowly.Infrastructure.Data;
using Flowly.Infrastructure.Services;
using Flowly.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Flowly.UnitTests.Services;

/// <summary>
/// Тести для фінансових сервісів (TransactionService, BudgetService).
/// Фокус на точності обчислень, захисті від overspending та статистиці.
/// </summary>
public class FinanceServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IArchiveService> _archiveServiceMock;
    private readonly TransactionService _transactionService;
    private readonly BudgetService _budgetService;
    private readonly Guid _testUserId;
    private readonly Guid _otherUserId;

    public FinanceServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _archiveServiceMock = new Mock<IArchiveService>();
        _transactionService = new TransactionService(_context, _archiveServiceMock.Object);
        _budgetService = new BudgetService(_context, _archiveServiceMock.Object);
        
        _testUserId = Guid.NewGuid();
        _otherUserId = Guid.NewGuid();
    }

    // ============================================
    // ТЕСТ 1: Створення транзакції з валідними даними
    // ============================================
    
    /// <summary>
    /// Перевіряємо, що можна створити транзакцію з коректними даними.
    /// </summary>
    [Fact]
    public async Task CreateTransactionAsync_WithValidData_ShouldCreateSuccessfully()
    {
        // Arrange - створюємо валюту та категорію
        await TestDataSeeder.CreateTestCurrencyAsync(_context, "USD");
        var categories = await TestDataSeeder.CreateTestCategoriesAsync(_context, _testUserId);

        var createDto = new CreateTransactionDto
        {
            Title = "Grocery Shopping",
            Amount = 50.75m,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            CategoryId = categories[0].Id,
            Date = DateTime.UtcNow.Date,
            Description = "Weekly groceries"
        };

        // Act
        var result = await _transactionService.CreateAsync(_testUserId, createDto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Grocery Shopping");
        result.Amount.Should().Be(50.75m);
        result.Type.Should().Be(TransactionType.Expense);
        result.Category.Should().NotBeNull();
        result.Category!.Id.Should().Be(categories[0].Id);

        // Перевіряємо в базі
        var transactionInDb = await _context.Transactions.FindAsync(result.Id);
        transactionInDb.Should().NotBeNull();
        transactionInDb!.UserId.Should().Be(_testUserId);
    }

    // ============================================
    // ТЕСТ 2: Валідація суми транзакції
    // ============================================
    
    /// <summary>
    /// Перевіряємо, що сума транзакції має бути позитивною.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(-0.01)]
    public async Task CreateTransactionAsync_WithInvalidAmount_ShouldThrowException(decimal amount)
    {
        // Arrange
        await TestDataSeeder.CreateTestCurrencyAsync(_context, "USD");

        var createDto = new CreateTransactionDto
        {
            Title = "Invalid Transaction",
            Amount = amount,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            Date = DateTime.UtcNow.Date
        };

        // Act & Assert
        var act = async () => await _transactionService.CreateAsync(_testUserId, createDto);
        
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*must be positive*");
    }

    // ============================================
    // ТЕСТ 3: Перевірка budget overspent
    // ============================================
    
    /// <summary>
    /// Перевіряємо, що можна визначити чи бюджет перевищений.
    /// Це важливо для попереджень користувача.
    /// </summary>
    [Fact]
    public async Task IsOverspentAsync_WhenExpensesExceedLimit_ShouldReturnTrue()
    {
        // Arrange - створюємо валюту та бюджет
        await TestDataSeeder.CreateTestCurrencyAsync(_context, "USD");
        var budget = await TestDataSeeder.CreateTestBudgetAsync(
            _context, _testUserId, "USD", limit: 100m);

        // Створюємо транзакції, що перевищують ліміт
        await _transactionService.CreateAsync(_testUserId, new CreateTransactionDto
        {
            Title = "Expense 1",
            Amount = 60m,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            BudgetId = budget.Id,
            Date = budget.PeriodStart.AddDays(1)
        });

        await _transactionService.CreateAsync(_testUserId, new CreateTransactionDto
        {
            Title = "Expense 2",
            Amount = 50m,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            BudgetId = budget.Id,
            Date = budget.PeriodStart.AddDays(2)
        });

        // Act
        var isOverspent = await _budgetService.IsOverspentAsync(_testUserId, budget.Id);

        // Assert
        isOverspent.Should().BeTrue("витрати (110) перевищують ліміт (100)");
    }

    // ============================================
    // ТЕСТ 4: Income зменшує spent в бюджеті
    // ============================================
    
    /// <summary>
    /// Перевіряємо, що income транзакції зменшують витрачену суму в бюджеті.
    /// Наприклад, повернення грошей або refund.
    /// </summary>
    [Fact]
    public async Task BudgetCalculation_WithIncomeTransaction_ShouldReduceSpent()
    {
        // Arrange
        await TestDataSeeder.CreateTestCurrencyAsync(_context, "USD");
        var budget = await TestDataSeeder.CreateTestBudgetAsync(
            _context, _testUserId, "USD", limit: 100m);

        // Створюємо expense
        await _transactionService.CreateAsync(_testUserId, new CreateTransactionDto
        {
            Title = "Purchase",
            Amount = 80m,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            BudgetId = budget.Id,
            Date = budget.PeriodStart.AddDays(1)
        });

        // Створюємо income (refund)
        await _transactionService.CreateAsync(_testUserId, new CreateTransactionDto
        {
            Title = "Refund",
            Amount = 30m,
            CurrencyCode = "USD",
            Type = TransactionType.Income,
            BudgetId = budget.Id,
            Date = budget.PeriodStart.AddDays(2)
        });

        // Act
        var budgetDetails = await _budgetService.GetByIdAsync(_testUserId, budget.Id);

        // Assert
        // Spent = 80 (expense) - 30 (income) = 50
        budgetDetails.CurrentSpent.Should().Be(50m, 
            "income має зменшувати витрачену суму");
        budgetDetails.CurrentSpent.Should().BeLessThan(budgetDetails.Limit);
    }

    // ============================================
    // ТЕСТ 5: Обчислення статистики за період
    // ============================================
    
    /// <summary>
    /// Перевіряємо правильність обчислення фінансової статистики.
    /// </summary>
    [Fact]
    public async Task GetStatsAsync_ShouldCalculateCorrectTotals()
    {
        // Arrange
        await TestDataSeeder.CreateTestCurrencyAsync(_context, "USD");
        var categories = await TestDataSeeder.CreateTestCategoriesAsync(_context, _testUserId);

        var periodStart = new DateTime(2024, 1, 1);
        var periodEnd = new DateTime(2024, 1, 31);

        // Створюємо транзакції в періоді
        await _transactionService.CreateAsync(_testUserId, new CreateTransactionDto
        {
            Title = "Salary",
            Amount = 3000m,
            CurrencyCode = "USD",
            Type = TransactionType.Income,
            CategoryId = categories[2].Id, // Salary category
            Date = new DateTime(2024, 1, 5)
        });

        await _transactionService.CreateAsync(_testUserId, new CreateTransactionDto
        {
            Title = "Rent",
            Amount = 1000m,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            CategoryId = categories[0].Id,
            Date = new DateTime(2024, 1, 10)
        });

        await _transactionService.CreateAsync(_testUserId, new CreateTransactionDto
        {
            Title = "Groceries",
            Amount = 500m,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            CategoryId = categories[0].Id, // Food
            Date = new DateTime(2024, 1, 15)
        });

        // Створюємо транзакцію ПОЗА періодом (не має враховуватися)
        await _transactionService.CreateAsync(_testUserId, new CreateTransactionDto
        {
            Title = "Out of period",
            Amount = 999m,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            Date = new DateTime(2024, 2, 1)
        });

        // Act
        var stats = await _transactionService.GetStatsAsync(
            _testUserId, periodStart, periodEnd, "USD");

        // Assert
        stats.Should().NotBeNull();
        stats.TotalIncome.Should().Be(3000m);
        stats.TotalExpense.Should().Be(1500m, "1000 + 500, транзакція поза періодом не враховується");
        stats.NetAmount.Should().Be(1500m, "3000 - 1500");
        stats.TotalTransactionCount.Should().Be(3, "тільки 3 транзакції в періоді");

        // Перевіряємо статистику по категоріях
        stats.ExpenseByCategory.Should().HaveCount(1, "всі витрати в одній категорії");
        stats.ExpenseByCategory.First().TotalAmount.Should().Be(1500m);
    }

    // ============================================
    // ТЕСТ 6: Статистика по категоріях з відсотками
    // ============================================
    
    /// <summary>
    /// Перевіряємо, що відсотки по категоріях обчислюються правильно.
    /// </summary>
    [Fact]
    public async Task GetStatsAsync_ShouldCalculateCategoryPercentages()
    {
        // Arrange
        await TestDataSeeder.CreateTestCurrencyAsync(_context, "USD");
        var categories = await TestDataSeeder.CreateTestCategoriesAsync(_context, _testUserId);

        var periodStart = DateTime.UtcNow.Date.AddMonths(-1);
        var periodEnd = DateTime.UtcNow.Date;

        // Створюємо витрати: 60% Food, 40% Transport
        await _transactionService.CreateAsync(_testUserId, new CreateTransactionDto
        {
            Title = "Food",
            Amount = 600m,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            CategoryId = categories[0].Id, // Food
            Date = periodStart.AddDays(5)
        });

        await _transactionService.CreateAsync(_testUserId, new CreateTransactionDto
        {
            Title = "Transport",
            Amount = 400m,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            CategoryId = categories[1].Id, // Transport
            Date = periodStart.AddDays(10)
        });

        // Act
        var stats = await _transactionService.GetStatsAsync(
            _testUserId, periodStart, periodEnd, "USD");

        // Assert
        stats.ExpenseByCategory.Should().HaveCount(2);
        
        var foodStats = stats.ExpenseByCategory.First(c => c.CategoryName == "Food");
        foodStats.TotalAmount.Should().Be(600m);
        foodStats.Percentage.Should().BeApproximately(60m, 0.01m);

        var transportStats = stats.ExpenseByCategory.First(c => c.CategoryName == "Transport");
        transportStats.TotalAmount.Should().Be(400m);
        transportStats.Percentage.Should().BeApproximately(40m, 0.01m);
    }

    // ============================================
    // ТЕСТ 7: Неможливість використати чужу категорію
    // ============================================
    
    /// <summary>
    /// БЕЗПЕКА: Користувач не може створити транзакцію з категорією іншого користувача.
    /// </summary>
    [Fact]
    public async Task CreateTransactionAsync_WithOtherUsersCategory_ShouldThrowException()
    {
        // Arrange
        await TestDataSeeder.CreateTestCurrencyAsync(_context, "USD");
        var otherUserCategories = await TestDataSeeder.CreateTestCategoriesAsync(_context, _otherUserId);

        var createDto = new CreateTransactionDto
        {
            Title = "My Transaction",
            Amount = 100m,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            CategoryId = otherUserCategories[0].Id, // Чужа категорія
            Date = DateTime.UtcNow.Date
        };

        // Act & Assert
        var act = async () => await _transactionService.CreateAsync(_testUserId, createDto);
        
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    // ============================================
    // ТЕСТ 8: Неможливість прив'язати до чужого бюджету
    // ============================================
    
    /// <summary>
    /// БЕЗПЕКА: Користувач не може прив'язати транзакцію до бюджету іншого користувача.
    /// </summary>
    [Fact]
    public async Task CreateTransactionAsync_WithOtherUsersBudget_ShouldThrowException()
    {
        // Arrange
        await TestDataSeeder.CreateTestCurrencyAsync(_context, "USD");
        var otherUserBudget = await TestDataSeeder.CreateTestBudgetAsync(
            _context, _otherUserId, "USD", 1000m);

        var createDto = new CreateTransactionDto
        {
            Title = "My Transaction",
            Amount = 100m,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            BudgetId = otherUserBudget.Id, // Чужий бюджет
            Date = DateTime.UtcNow.Date
        };

        // Act & Assert
        var act = async () => await _transactionService.CreateAsync(_testUserId, createDto);
        
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    // ============================================
    // ТЕСТ 9: Транзакції різних користувачів ізольовані
    // ============================================
    
    /// <summary>
    /// БЕЗПЕКА: Користувач бачить тільки свої транзакції в статистиці.
    /// </summary>
    [Fact]
    public async Task GetStatsAsync_ShouldShowOnlyUserTransactions()
    {
        // Arrange
        await TestDataSeeder.CreateTestCurrencyAsync(_context, "USD");

        var period = DateTime.UtcNow.Date;

        // Створюємо транзакції для testUser
        await _transactionService.CreateAsync(_testUserId, new CreateTransactionDto
        {
            Title = "My Income",
            Amount = 1000m,
            CurrencyCode = "USD",
            Type = TransactionType.Income,
            Date = period
        });

        // Створюємо транзакції для otherUser
        await _transactionService.CreateAsync(_otherUserId, new CreateTransactionDto
        {
            Title = "Other Income",
            Amount = 5000m,
            CurrencyCode = "USD",
            Type = TransactionType.Income,
            Date = period
        });

        // Act
        var stats = await _transactionService.GetStatsAsync(
            _testUserId, period, period.AddDays(1), "USD");

        // Assert
        stats.TotalIncome.Should().Be(1000m, "має враховуватися тільки income testUser");
        stats.TotalTransactionCount.Should().Be(1);
    }

    // ============================================
    // ТЕСТ 10: Валідація валюти бюджету та транзакції
    // ============================================
    
    /// <summary>
    /// Перевіряємо, що транзакція та бюджет мають використовувати одну валюту.
    /// Це захищає від помилок в обчисленнях.
    /// </summary>
    [Fact]
    public async Task CreateTransactionAsync_WithDifferentCurrency_ShouldThrowException()
    {
        // Arrange
        await TestDataSeeder.CreateTestCurrencyAsync(_context, "USD");
        await TestDataSeeder.CreateTestCurrencyAsync(_context, "EUR");
        
        var budget = await TestDataSeeder.CreateTestBudgetAsync(
            _context, _testUserId, "USD", 1000m);

        var createDto = new CreateTransactionDto
        {
            Title = "Transaction",
            Amount = 100m,
            CurrencyCode = "EUR", // Інша валюта!
            Type = TransactionType.Expense,
            BudgetId = budget.Id,
            Date = budget.PeriodStart.AddDays(1)
        };

        // Act & Assert
        var act = async () => await _transactionService.CreateAsync(_testUserId, createDto);
        
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*currency*must match*");
    }

    // ============================================
    // ТЕСТ 11: Архівовані транзакції не враховуються в бюджеті
    // ============================================
    
    /// <summary>
    /// Перевіряємо, що архівовані транзакції не враховуються при обчисленні бюджету.
    /// </summary>
    [Fact]
    public async Task BudgetCalculation_ShouldExcludeArchivedTransactions()
    {
        // Arrange
        await TestDataSeeder.CreateTestCurrencyAsync(_context, "USD");
        var budget = await TestDataSeeder.CreateTestBudgetAsync(
            _context, _testUserId, "USD", limit: 100m);

        // Створюємо активну транзакцію
        var activeTransaction = await _transactionService.CreateAsync(_testUserId, new CreateTransactionDto
        {
            Title = "Active Expense",
            Amount = 40m,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            BudgetId = budget.Id,
            Date = budget.PeriodStart.AddDays(1)
        });

        // Створюємо транзакцію, яку потім архівуємо
        var toArchive = await _transactionService.CreateAsync(_testUserId, new CreateTransactionDto
        {
            Title = "To Archive",
            Amount = 60m,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            BudgetId = budget.Id,
            Date = budget.PeriodStart.AddDays(2)
        });

        // Архівуємо другу транзакцію
        var transactionEntity = await _context.Transactions.FindAsync(toArchive.Id);
        transactionEntity!.Archive();
        await _context.SaveChangesAsync();

        // Act
        var budgetDetails = await _budgetService.GetByIdAsync(_testUserId, budget.Id);

        // Assert
        budgetDetails.CurrentSpent.Should().Be(40m, 
            "має враховуватися тільки активна транзакція");
    }

    // ============================================
    // ТЕСТ 12: Середні витрати за день
    // ============================================
    
    /// <summary>
    /// Перевіряємо обчислення середніх щоденних витрат.
    /// </summary>
    [Fact]
    public async Task GetStatsAsync_ShouldCalculateAverageDailyExpense()
    {
        // Arrange
        await TestDataSeeder.CreateTestCurrencyAsync(_context, "USD");

        var periodStart = new DateTime(2024, 1, 1);
        var periodEnd = new DateTime(2024, 1, 10); // 10 днів

        // Створюємо витрати на загальну суму 1000
        await _transactionService.CreateAsync(_testUserId, new CreateTransactionDto
        {
            Title = "Expense 1",
            Amount = 600m,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            Date = new DateTime(2024, 1, 5)
        });

        await _transactionService.CreateAsync(_testUserId, new CreateTransactionDto
        {
            Title = "Expense 2",
            Amount = 400m,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            Date = new DateTime(2024, 1, 8)
        });

        // Act
        var stats = await _transactionService.GetStatsAsync(
            _testUserId, periodStart, periodEnd, "USD");

        // Assert
        // 1000 / 10 днів = 100 на день
        stats.AverageDailyExpense.Should().BeApproximately(100m, 0.01m);
    }

    // ============================================
    // ТЕСТ 13: Неможливість отримати чужий бюджет
    // ============================================
    
    /// <summary>
    /// БЕЗПЕКА: Користувач не може отримати деталі бюджету іншого користувача.
    /// </summary>
    [Fact]
    public async Task GetBudgetByIdAsync_WithOtherUsersBudget_ShouldThrowException()
    {
        // Arrange
        await TestDataSeeder.CreateTestCurrencyAsync(_context, "USD");
        var otherUserBudget = await TestDataSeeder.CreateTestBudgetAsync(
            _context, _otherUserId, "USD", 1000m);

        // Act & Assert
        var act = async () => await _budgetService.GetByIdAsync(_testUserId, otherUserBudget.Id);
        
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    // ============================================
    // ТЕСТ 14: Транзакції поза періодом бюджету не враховуються
    // ============================================
    
    /// <summary>
    /// Перевіряємо, що тільки транзакції в межах періоду бюджету враховуються.
    /// </summary>
    [Fact]
    public async Task BudgetCalculation_ShouldOnlyIncludeTransactionsInPeriod()
    {
        // Arrange
        await TestDataSeeder.CreateTestCurrencyAsync(_context, "USD");
        
        var periodStart = new DateTime(2024, 1, 1);
        var periodEnd = new DateTime(2024, 1, 31);
        
        var budget = new Domain.Entities.Budget
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Title = "January Budget",
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            Limit = 1000m,
            CurrencyCode = "USD",
            CreatedAt = DateTime.UtcNow
        };
        _context.Budgets.Add(budget);
        await _context.SaveChangesAsync();

        // Транзакція ДО періоду
        await _transactionService.CreateAsync(_testUserId, new CreateTransactionDto
        {
            Title = "Before Period",
            Amount = 100m,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            BudgetId = budget.Id,
            Date = periodStart.AddDays(-1)
        });

        // Транзакція В періоді
        await _transactionService.CreateAsync(_testUserId, new CreateTransactionDto
        {
            Title = "In Period",
            Amount = 200m,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            BudgetId = budget.Id,
            Date = periodStart.AddDays(10)
        });

        // Транзакція ПІСЛЯ періоду
        await _transactionService.CreateAsync(_testUserId, new CreateTransactionDto
        {
            Title = "After Period",
            Amount = 300m,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            BudgetId = budget.Id,
            Date = periodEnd.AddDays(1)
        });

        // Act
        var budgetDetails = await _budgetService.GetByIdAsync(_testUserId, budget.Id);

        // Assert
        budgetDetails.CurrentSpent.Should().Be(200m, 
            "має враховуватися тільки транзакція в межах періоду");
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }
}
