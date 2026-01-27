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

    [Fact]
    public async Task CreateTransactionAsync_WithValidData_ShouldCreateSuccessfully()
    {
        
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

        var result = await _transactionService.CreateAsync(_testUserId, createDto);

        result.Should().NotBeNull();
        result.Title.Should().Be("Grocery Shopping");
        result.Amount.Should().Be(50.75m);
        result.Type.Should().Be(TransactionType.Expense);
        result.Category.Should().NotBeNull();
        result.Category!.Id.Should().Be(categories[0].Id);

        var transactionInDb = await _context.Transactions.FindAsync(result.Id);
        transactionInDb.Should().NotBeNull();
        transactionInDb!.UserId.Should().Be(_testUserId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(-0.01)]
    public async Task CreateTransactionAsync_WithInvalidAmount_ShouldThrowException(decimal amount)
    {
        
        await TestDataSeeder.CreateTestCurrencyAsync(_context, "USD");

        var createDto = new CreateTransactionDto
        {
            Title = "Invalid Transaction",
            Amount = amount,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            Date = DateTime.UtcNow.Date
        };

        var act = async () => await _transactionService.CreateAsync(_testUserId, createDto);
        
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*must be positive*");
    }

    [Fact]
    public async Task IsOverspentAsync_WhenExpensesExceedLimit_ShouldReturnTrue()
    {
        
        await TestDataSeeder.CreateTestCurrencyAsync(_context, "USD");
        var budget = await TestDataSeeder.CreateTestBudgetAsync(
            _context, _testUserId, "USD", limit: 100m);

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

        var isOverspent = await _budgetService.IsOverspentAsync(_testUserId, budget.Id);

        isOverspent.Should().BeTrue("витрати (110) перевищують ліміт (100)");
    }

    [Fact]
    public async Task BudgetCalculation_WithIncomeTransaction_ShouldReduceSpent()
    {
        
        await TestDataSeeder.CreateTestCurrencyAsync(_context, "USD");
        var budget = await TestDataSeeder.CreateTestBudgetAsync(
            _context, _testUserId, "USD", limit: 100m);

        await _transactionService.CreateAsync(_testUserId, new CreateTransactionDto
        {
            Title = "Purchase",
            Amount = 80m,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            BudgetId = budget.Id,
            Date = budget.PeriodStart.AddDays(1)
        });

        await _transactionService.CreateAsync(_testUserId, new CreateTransactionDto
        {
            Title = "Refund",
            Amount = 30m,
            CurrencyCode = "USD",
            Type = TransactionType.Income,
            BudgetId = budget.Id,
            Date = budget.PeriodStart.AddDays(2)
        });

        var budgetDetails = await _budgetService.GetByIdAsync(_testUserId, budget.Id);

        budgetDetails.CurrentSpent.Should().Be(50m, 
            "income має зменшувати витрачену суму");
        budgetDetails.CurrentSpent.Should().BeLessThan(budgetDetails.Limit);
    }

    [Fact]
    public async Task GetStatsAsync_ShouldCalculateCorrectTotals()
    {
        
        await TestDataSeeder.CreateTestCurrencyAsync(_context, "USD");
        var categories = await TestDataSeeder.CreateTestCategoriesAsync(_context, _testUserId);

        var periodStart = new DateTime(2024, 1, 1);
        var periodEnd = new DateTime(2024, 1, 31);

        await _transactionService.CreateAsync(_testUserId, new CreateTransactionDto
        {
            Title = "Salary",
            Amount = 3000m,
            CurrencyCode = "USD",
            Type = TransactionType.Income,
            CategoryId = categories[2].Id, 
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
            CategoryId = categories[0].Id, 
            Date = new DateTime(2024, 1, 15)
        });

        await _transactionService.CreateAsync(_testUserId, new CreateTransactionDto
        {
            Title = "Out of period",
            Amount = 999m,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            Date = new DateTime(2024, 2, 1)
        });

        var stats = await _transactionService.GetStatsAsync(
            _testUserId, periodStart, periodEnd, "USD");

        stats.Should().NotBeNull();
        stats.TotalIncome.Should().Be(3000m);
        stats.TotalExpense.Should().Be(1500m, "1000 + 500, транзакція поза періодом не враховується");
        stats.NetAmount.Should().Be(1500m, "3000 - 1500");
        stats.TotalTransactionCount.Should().Be(3, "тільки 3 транзакції в періоді");

        stats.ExpenseByCategory.Should().HaveCount(1, "всі витрати в одній категорії");
        stats.ExpenseByCategory.First().TotalAmount.Should().Be(1500m);
    }

    [Fact]
    public async Task GetStatsAsync_ShouldCalculateCategoryPercentages()
    {
        
        await TestDataSeeder.CreateTestCurrencyAsync(_context, "USD");
        var categories = await TestDataSeeder.CreateTestCategoriesAsync(_context, _testUserId);

        var periodStart = DateTime.UtcNow.Date.AddMonths(-1);
        var periodEnd = DateTime.UtcNow.Date;

        await _transactionService.CreateAsync(_testUserId, new CreateTransactionDto
        {
            Title = "Food",
            Amount = 600m,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            CategoryId = categories[0].Id, 
            Date = periodStart.AddDays(5)
        });

        await _transactionService.CreateAsync(_testUserId, new CreateTransactionDto
        {
            Title = "Transport",
            Amount = 400m,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            CategoryId = categories[1].Id, 
            Date = periodStart.AddDays(10)
        });

        var stats = await _transactionService.GetStatsAsync(
            _testUserId, periodStart, periodEnd, "USD");

        stats.ExpenseByCategory.Should().HaveCount(2);
        
        var foodStats = stats.ExpenseByCategory.First(c => c.CategoryName == "Food");
        foodStats.TotalAmount.Should().Be(600m);
        foodStats.Percentage.Should().BeApproximately(60m, 0.01m);

        var transportStats = stats.ExpenseByCategory.First(c => c.CategoryName == "Transport");
        transportStats.TotalAmount.Should().Be(400m);
        transportStats.Percentage.Should().BeApproximately(40m, 0.01m);
    }

    [Fact]
    public async Task CreateTransactionAsync_WithOtherUsersCategory_ShouldThrowException()
    {
        
        await TestDataSeeder.CreateTestCurrencyAsync(_context, "USD");
        var otherUserCategories = await TestDataSeeder.CreateTestCategoriesAsync(_context, _otherUserId);

        var createDto = new CreateTransactionDto
        {
            Title = "My Transaction",
            Amount = 100m,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            CategoryId = otherUserCategories[0].Id, 
            Date = DateTime.UtcNow.Date
        };

        var act = async () => await _transactionService.CreateAsync(_testUserId, createDto);
        
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task CreateTransactionAsync_WithOtherUsersBudget_ShouldThrowException()
    {
        
        await TestDataSeeder.CreateTestCurrencyAsync(_context, "USD");
        var otherUserBudget = await TestDataSeeder.CreateTestBudgetAsync(
            _context, _otherUserId, "USD", 1000m);

        var createDto = new CreateTransactionDto
        {
            Title = "My Transaction",
            Amount = 100m,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            BudgetId = otherUserBudget.Id, 
            Date = DateTime.UtcNow.Date
        };

        var act = async () => await _transactionService.CreateAsync(_testUserId, createDto);
        
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task GetStatsAsync_ShouldShowOnlyUserTransactions()
    {
        
        await TestDataSeeder.CreateTestCurrencyAsync(_context, "USD");

        var period = DateTime.UtcNow.Date;

        await _transactionService.CreateAsync(_testUserId, new CreateTransactionDto
        {
            Title = "My Income",
            Amount = 1000m,
            CurrencyCode = "USD",
            Type = TransactionType.Income,
            Date = period
        });

        await _transactionService.CreateAsync(_otherUserId, new CreateTransactionDto
        {
            Title = "Other Income",
            Amount = 5000m,
            CurrencyCode = "USD",
            Type = TransactionType.Income,
            Date = period
        });

        var stats = await _transactionService.GetStatsAsync(
            _testUserId, period, period.AddDays(1), "USD");

        stats.TotalIncome.Should().Be(1000m, "має враховуватися тільки income testUser");
        stats.TotalTransactionCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateTransactionAsync_WithDifferentCurrency_ShouldThrowException()
    {
        
        await TestDataSeeder.CreateTestCurrencyAsync(_context, "USD");
        await TestDataSeeder.CreateTestCurrencyAsync(_context, "EUR");
        
        var budget = await TestDataSeeder.CreateTestBudgetAsync(
            _context, _testUserId, "USD", 1000m);

        var createDto = new CreateTransactionDto
        {
            Title = "Transaction",
            Amount = 100m,
            CurrencyCode = "EUR", 
            Type = TransactionType.Expense,
            BudgetId = budget.Id,
            Date = budget.PeriodStart.AddDays(1)
        };

        var act = async () => await _transactionService.CreateAsync(_testUserId, createDto);
        
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*currency*must match*");
    }

    [Fact]
    public async Task BudgetCalculation_ShouldExcludeArchivedTransactions()
    {
        
        await TestDataSeeder.CreateTestCurrencyAsync(_context, "USD");
        var budget = await TestDataSeeder.CreateTestBudgetAsync(
            _context, _testUserId, "USD", limit: 100m);

        var activeTransaction = await _transactionService.CreateAsync(_testUserId, new CreateTransactionDto
        {
            Title = "Active Expense",
            Amount = 40m,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            BudgetId = budget.Id,
            Date = budget.PeriodStart.AddDays(1)
        });

        var toArchive = await _transactionService.CreateAsync(_testUserId, new CreateTransactionDto
        {
            Title = "To Archive",
            Amount = 60m,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            BudgetId = budget.Id,
            Date = budget.PeriodStart.AddDays(2)
        });

        var transactionEntity = await _context.Transactions.FindAsync(toArchive.Id);
        transactionEntity!.Archive();
        await _context.SaveChangesAsync();

        var budgetDetails = await _budgetService.GetByIdAsync(_testUserId, budget.Id);

        budgetDetails.CurrentSpent.Should().Be(40m, 
            "має враховуватися тільки активна транзакція");
    }

    [Fact]
    public async Task GetStatsAsync_ShouldCalculateAverageDailyExpense()
    {
        
        await TestDataSeeder.CreateTestCurrencyAsync(_context, "USD");

        var periodStart = new DateTime(2024, 1, 1);
        var periodEnd = new DateTime(2024, 1, 10); 

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

        var stats = await _transactionService.GetStatsAsync(
            _testUserId, periodStart, periodEnd, "USD");

        stats.AverageDailyExpense.Should().BeApproximately(100m, 0.01m);
    }

    [Fact]
    public async Task GetBudgetByIdAsync_WithOtherUsersBudget_ShouldThrowException()
    {
        
        await TestDataSeeder.CreateTestCurrencyAsync(_context, "USD");
        var otherUserBudget = await TestDataSeeder.CreateTestBudgetAsync(
            _context, _otherUserId, "USD", 1000m);

        var act = async () => await _budgetService.GetByIdAsync(_testUserId, otherUserBudget.Id);
        
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task BudgetCalculation_ShouldOnlyIncludeTransactionsInPeriod()
    {
        
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

        await _transactionService.CreateAsync(_testUserId, new CreateTransactionDto
        {
            Title = "Before Period",
            Amount = 100m,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            BudgetId = budget.Id,
            Date = periodStart.AddDays(-1)
        });

        await _transactionService.CreateAsync(_testUserId, new CreateTransactionDto
        {
            Title = "In Period",
            Amount = 200m,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            BudgetId = budget.Id,
            Date = periodStart.AddDays(10)
        });

        await _transactionService.CreateAsync(_testUserId, new CreateTransactionDto
        {
            Title = "After Period",
            Amount = 300m,
            CurrencyCode = "USD",
            Type = TransactionType.Expense,
            BudgetId = budget.Id,
            Date = periodEnd.AddDays(1)
        });

        var budgetDetails = await _budgetService.GetByIdAsync(_testUserId, budget.Id);

        budgetDetails.CurrentSpent.Should().Be(200m, 
            "має враховуватися тільки транзакція в межах періоду");
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }
}
