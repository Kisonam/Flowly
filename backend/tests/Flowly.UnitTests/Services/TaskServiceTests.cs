using FluentAssertions;
using Flowly.Application.DTOs.Tasks;
using Flowly.Application.Interfaces;
using Flowly.Domain.Entities;
using Flowly.Domain.Enums;
using Flowly.Infrastructure.Data;
using Flowly.Infrastructure.Services;
using Flowly.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Flowly.UnitTests.Services;

/// <summary>
/// Тести для TaskService - перевіряємо роботу з задачами.
/// Фокус на складній логіці: переміщення між темами, recurring tasks, subtasks.
/// </summary>
public class TaskServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IArchiveService> _archiveServiceMock;
    private readonly TaskService _taskService;
    private readonly Guid _testUserId;
    private readonly Guid _otherUserId;

    public TaskServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _archiveServiceMock = new Mock<IArchiveService>();
        _taskService = new TaskService(_context, _archiveServiceMock.Object);
        
        _testUserId = Guid.NewGuid();
        _otherUserId = Guid.NewGuid();
    }

    // ============================================
    // ТЕСТ 1: Переміщення задачі між темами
    // ============================================
    
    /// <summary>
    /// Перевіряємо, що задача може бути переміщена з однієї теми в іншу.
    /// При переміщенні має змінитися Order (задача додається в кінець нової теми).
    /// </summary>
    [Fact]
    public async Task MoveTaskToThemeAsync_ShouldMoveTaskAndUpdateOrder()
    {
        // Arrange - створюємо дві теми
        var theme1 = await TestDataSeeder.CreateTestTaskThemeAsync(_context, _testUserId, "Work");
        var theme2 = await TestDataSeeder.CreateTestTaskThemeAsync(_context, _testUserId, "Personal");

        // Створюємо задачі в theme1
        var task1 = await _taskService.CreateTaskAsync(_testUserId, new CreateTaskDto
        {
            Title = "Task 1 in Work",
            ThemeId = theme1.Id
        });

        var task2 = await _taskService.CreateTaskAsync(_testUserId, new CreateTaskDto
        {
            Title = "Task 2 in Work",
            ThemeId = theme1.Id
        });

        // Створюємо задачу в theme2
        var task3 = await _taskService.CreateTaskAsync(_testUserId, new CreateTaskDto
        {
            Title = "Task 3 in Personal",
            ThemeId = theme2.Id
        });

        // Act - переміщуємо task1 з Work в Personal
        await _taskService.MoveTaskToThemeAsync(_testUserId, task1.Id, theme2.Id);

        // Assert - перевіряємо, що задача переміщена
        var movedTask = await _taskService.GetTaskByIdAsync(_testUserId, task1.Id);
        movedTask.Theme.Should().NotBeNull();
        movedTask.Theme!.Id.Should().Be(theme2.Id, "задача має бути в новій темі");

        // Перевіряємо Order - задача має бути в кінці нової теми
        var tasksInTheme2 = await _context.Tasks
            .Where(t => t.TaskThemeId == theme2.Id && t.UserId == _testUserId)
            .OrderBy(t => t.Order)
            .ToListAsync();

        tasksInTheme2.Should().HaveCount(2);
        tasksInTheme2.Last().Id.Should().Be(task1.Id, 
            "переміщена задача має бути в кінці списку");
    }

    // ============================================
    // ТЕСТ 2: Переміщення задачі в null theme (unassigned)
    // ============================================
    
    /// <summary>
    /// Перевіряємо, що задачу можна перемістити з теми в "без теми".
    /// </summary>
    [Fact]
    public async Task MoveTaskToThemeAsync_ToNullTheme_ShouldMoveToUnassigned()
    {
        // Arrange
        var theme = await TestDataSeeder.CreateTestTaskThemeAsync(_context, _testUserId, "Work");
        
        var task = await _taskService.CreateTaskAsync(_testUserId, new CreateTaskDto
        {
            Title = "Task in theme",
            ThemeId = theme.Id
        });

        // Act - переміщуємо в null (без теми)
        await _taskService.MoveTaskToThemeAsync(_testUserId, task.Id, null);

        // Assert
        var movedTask = await _taskService.GetTaskByIdAsync(_testUserId, task.Id);
        movedTask.Theme.Should().BeNull("задача має бути без теми");
    }

    // ============================================
    // ТЕСТ 3: Неможливість перемістити задачу в чужу тему
    // ============================================
    
    /// <summary>
    /// БЕЗПЕКА: Користувач не може перемістити свою задачу в тему іншого користувача.
    /// </summary>
    [Fact]
    public async Task MoveTaskToThemeAsync_ToOtherUsersTheme_ShouldThrowException()
    {
        // Arrange - створюємо тему для іншого користувача
        var otherUserTheme = await TestDataSeeder.CreateTestTaskThemeAsync(
            _context, _otherUserId, "Other User Theme");

        // Створюємо задачу для testUser
        var task = await _taskService.CreateTaskAsync(_testUserId, new CreateTaskDto
        {
            Title = "My Task"
        });

        // Act & Assert - намагаємося перемістити в чужу тему
        var act = async () => await _taskService.MoveTaskToThemeAsync(
            _testUserId, task.Id, otherUserTheme.Id);
        
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*", "має бути помилка про неіснуючу тему");
    }

    // ============================================
    // ТЕСТ 4: Створення recurring task (щоденна)
    // ============================================
    
    /// <summary>
    /// Перевіряємо створення задачі з recurrence правилом.
    /// </summary>
    [Fact]
    public async Task SetRecurrenceAsync_WithDailyRule_ShouldCreateRecurrence()
    {
        // Arrange - створюємо задачу
        var task = await _taskService.CreateTaskAsync(_testUserId, new CreateTaskDto
        {
            Title = "Daily Task",
            DueDate = DateTime.UtcNow.Date.AddDays(1)
        });

        // Act - встановлюємо щоденне повторення
        var recurrenceDto = new CreateRecurrenceDto
        {
            Rule = "FREQ=DAILY;INTERVAL=1"
        };
        var recurrence = await _taskService.SetRecurrenceAsync(_testUserId, task.Id, recurrenceDto);

        // Assert
        recurrence.Should().NotBeNull();
        recurrence.Rule.Should().Be("FREQ=DAILY;INTERVAL=1");

        // Перевіряємо, що recurrence збережена в базі
        var taskWithRecurrence = await _taskService.GetTaskByIdAsync(_testUserId, task.Id);
        taskWithRecurrence.Recurrence.Should().NotBeNull();
        taskWithRecurrence.Recurrence!.Rule.Should().Be("FREQ=DAILY;INTERVAL=1");
    }

    // ============================================
    // ТЕСТ 5: Завершення recurring task створює нову
    // ============================================
    
    /// <summary>
    /// Перевіряємо, що при завершенні recurring task створюється нова задача на наступну дату.
    /// Це складна логіка, яку важливо тестувати.
    /// </summary>
    [Fact]
    public async Task CompleteTaskAsync_WithRecurrence_ShouldCreateNextOccurrence()
    {
        // Arrange - створюємо recurring task
        var dueDate = DateTime.UtcNow.Date.AddDays(1).AddHours(14); // Завтра о 14:00
        var task = await _taskService.CreateTaskAsync(_testUserId, new CreateTaskDto
        {
            Title = "Daily Standup",
            Description = "Team meeting",
            DueDate = dueDate
        });

        // Встановлюємо щоденне повторення
        await _taskService.SetRecurrenceAsync(_testUserId, task.Id, new CreateRecurrenceDto
        {
            Rule = "FREQ=DAILY;INTERVAL=1"
        });

        // Act - завершуємо задачу
        await _taskService.CompleteTaskAsync(_testUserId, task.Id);

        // Assert - перевіряємо, що оригінальна задача завершена
        var completedTask = await _taskService.GetTaskByIdAsync(_testUserId, task.Id);
        completedTask.Status.Should().Be(TasksStatus.Done);
        completedTask.CompletedAt.Should().NotBeNull();

        // Перевіряємо, що створена нова задача на наступний день
        var allTasks = await _context.Tasks
            .Where(t => t.UserId == _testUserId && t.Title == "Daily Standup")
            .ToListAsync();

        allTasks.Should().HaveCount(2, "має бути 2 задачі: завершена і нова");

        var newTask = allTasks.FirstOrDefault(t => t.Status == TasksStatus.Todo);
        newTask.Should().NotBeNull("має бути створена нова задача");
        newTask!.DueDate.Should().NotBeNull();
        newTask.DueDate!.Value.Date.Should().Be(dueDate.Date.AddDays(1), 
            "нова задача має бути на наступний день");
        newTask.DueDate!.Value.Hour.Should().Be(14, "час має зберегтися");
    }

    // ============================================
    // ТЕСТ 6: Recurring task з тижневим інтервалом
    // ============================================
    
    /// <summary>
    /// Перевіряємо weekly recurrence з конкретними днями тижня.
    /// </summary>
    [Fact]
    public async Task CompleteTaskAsync_WithWeeklyRecurrence_ShouldCreateNextWeek()
    {
        // Arrange - створюємо задачу на понеділок
        var monday = GetNextDayOfWeek(DayOfWeek.Monday);
        var task = await _taskService.CreateTaskAsync(_testUserId, new CreateTaskDto
        {
            Title = "Weekly Review",
            DueDate = monday.AddHours(10)
        });

        // Встановлюємо тижневе повторення по понеділках
        await _taskService.SetRecurrenceAsync(_testUserId, task.Id, new CreateRecurrenceDto
        {
            Rule = "FREQ=WEEKLY;INTERVAL=1;BYDAY=MO"
        });

        // Act
        await _taskService.CompleteTaskAsync(_testUserId, task.Id);

        // Assert
        var newTask = await _context.Tasks
            .Where(t => t.UserId == _testUserId 
                && t.Title == "Weekly Review" 
                && t.Status == TasksStatus.Todo)
            .FirstOrDefaultAsync();

        newTask.Should().NotBeNull();
        newTask!.DueDate.Should().NotBeNull();
        newTask.DueDate!.Value.Date.Should().Be(monday.AddDays(7).Date, 
            "нова задача має бути через тиждень");
    }

    // ============================================
    // ТЕСТ 7: Додавання subtask до задачі
    // ============================================
    
    /// <summary>
    /// Перевіряємо, що можна додати підзадачу до задачі.
    /// </summary>
    [Fact]
    public async Task AddSubtaskAsync_ShouldCreateSubtask()
    {
        // Arrange - створюємо основну задачу
        var task = await _taskService.CreateTaskAsync(_testUserId, new CreateTaskDto
        {
            Title = "Main Task"
        });

        // Act - додаємо підзадачу
        var subtaskDto = new CreateSubtaskDto
        {
            Title = "Subtask 1"
        };
        var subtask = await _taskService.AddSubtaskAsync(_testUserId, task.Id, subtaskDto);

        // Assert
        subtask.Should().NotBeNull();
        subtask.Title.Should().Be("Subtask 1");
        subtask.IsDone.Should().BeFalse("нова підзадача не має бути виконана");

        // Перевіряємо, що підзадача прив'язана до задачі
        var taskWithSubtasks = await _taskService.GetTaskByIdAsync(_testUserId, task.Id);
        taskWithSubtasks.Subtasks.Should().HaveCount(1);
        taskWithSubtasks.Subtasks.First().Title.Should().Be("Subtask 1");
    }

    // ============================================
    // ТЕСТ 8: Порядок subtasks зберігається
    // ============================================
    
    /// <summary>
    /// Перевіряємо, що підзадачі зберігають порядок додавання.
    /// </summary>
    [Fact]
    public async Task AddSubtaskAsync_Multiple_ShouldMaintainOrder()
    {
        // Arrange
        var task = await _taskService.CreateTaskAsync(_testUserId, new CreateTaskDto
        {
            Title = "Task with multiple subtasks"
        });

        // Act - додаємо 3 підзадачі
        var subtask1 = await _taskService.AddSubtaskAsync(_testUserId, task.Id, 
            new CreateSubtaskDto { Title = "First" });
        var subtask2 = await _taskService.AddSubtaskAsync(_testUserId, task.Id, 
            new CreateSubtaskDto { Title = "Second" });
        var subtask3 = await _taskService.AddSubtaskAsync(_testUserId, task.Id, 
            new CreateSubtaskDto { Title = "Third" });

        // Assert
        var taskWithSubtasks = await _taskService.GetTaskByIdAsync(_testUserId, task.Id);
        taskWithSubtasks.Subtasks.Should().HaveCount(3);
        
        // Перевіряємо порядок
        taskWithSubtasks.Subtasks[0].Title.Should().Be("First");
        taskWithSubtasks.Subtasks[1].Title.Should().Be("Second");
        taskWithSubtasks.Subtasks[2].Title.Should().Be("Third");
        
        // Перевіряємо Order values
        taskWithSubtasks.Subtasks[0].Order.Should().BeLessThan(taskWithSubtasks.Subtasks[1].Order);
        taskWithSubtasks.Subtasks[1].Order.Should().BeLessThan(taskWithSubtasks.Subtasks[2].Order);
    }

    // ============================================
    // ТЕСТ 9: Toggle subtask змінює статус
    // ============================================
    
    /// <summary>
    /// Перевіряємо, що можна перемикати статус виконання підзадачі.
    /// </summary>
    [Fact]
    public async Task ToggleSubtaskAsync_ShouldChangeStatus()
    {
        // Arrange
        var task = await _taskService.CreateTaskAsync(_testUserId, new CreateTaskDto
        {
            Title = "Task"
        });
        
        var subtask = await _taskService.AddSubtaskAsync(_testUserId, task.Id, 
            new CreateSubtaskDto { Title = "Subtask" });

        subtask.IsDone.Should().BeFalse("початковий статус - не виконано");

        // Act - перемикаємо на виконано
        var toggled1 = await _taskService.ToggleSubtaskAsync(_testUserId, task.Id, subtask.Id);
        
        // Assert
        toggled1.IsDone.Should().BeTrue("після toggle має бути виконано");
        toggled1.CompletedAt.Should().NotBeNull("має бути встановлена дата завершення");

        // Act - перемикаємо назад на не виконано
        var toggled2 = await _taskService.ToggleSubtaskAsync(_testUserId, task.Id, subtask.Id);
        
        // Assert
        toggled2.IsDone.Should().BeFalse("після другого toggle має бути не виконано");
        toggled2.CompletedAt.Should().BeNull("дата завершення має бути очищена");
    }

    // ============================================
    // ТЕСТ 10: Неможливість додати subtask до чужої задачі
    // ============================================
    
    /// <summary>
    /// БЕЗПЕКА: Користувач не може додати підзадачу до задачі іншого користувача.
    /// </summary>
    [Fact]
    public async Task AddSubtaskAsync_ToOtherUsersTask_ShouldThrowException()
    {
        // Arrange - створюємо задачу для іншого користувача
        var otherUserTask = await _taskService.CreateTaskAsync(_otherUserId, new CreateTaskDto
        {
            Title = "Other User Task"
        });

        // Act & Assert - testUser намагається додати підзадачу
        var act = async () => await _taskService.AddSubtaskAsync(
            _testUserId, 
            otherUserTask.Id, 
            new CreateSubtaskDto { Title = "Malicious Subtask" });
        
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    // ============================================
    // ТЕСТ 11: Recurring task клонує subtasks
    // ============================================
    
    /// <summary>
    /// Перевіряємо, що при створенні наступної recurring task підзадачі також клонуються.
    /// </summary>
    [Fact]
    public async Task CompleteTaskAsync_WithRecurrenceAndSubtasks_ShouldCloneSubtasks()
    {
        // Arrange - створюємо recurring task з підзадачами
        var task = await _taskService.CreateTaskAsync(_testUserId, new CreateTaskDto
        {
            Title = "Weekly Planning",
            DueDate = DateTime.UtcNow.Date.AddDays(1)
        });

        // Додаємо підзадачі
        await _taskService.AddSubtaskAsync(_testUserId, task.Id, 
            new CreateSubtaskDto { Title = "Review goals" });
        await _taskService.AddSubtaskAsync(_testUserId, task.Id, 
            new CreateSubtaskDto { Title = "Plan tasks" });

        // Встановлюємо recurrence
        await _taskService.SetRecurrenceAsync(_testUserId, task.Id, 
            new CreateRecurrenceDto { Rule = "FREQ=WEEKLY;INTERVAL=1" });

        // Завершуємо одну з підзадач
        var taskWithSubtasks = await _taskService.GetTaskByIdAsync(_testUserId, task.Id);
        await _taskService.ToggleSubtaskAsync(_testUserId, task.Id, 
            taskWithSubtasks.Subtasks[0].Id);

        // Act - завершуємо основну задачу
        await _taskService.CompleteTaskAsync(_testUserId, task.Id);

        // Assert - знаходимо нову задачу
        var newTask = await _context.Tasks
            .Include(t => t.Subtasks)
            .Where(t => t.UserId == _testUserId 
                && t.Title == "Weekly Planning" 
                && t.Status == TasksStatus.Todo)
            .FirstOrDefaultAsync();

        newTask.Should().NotBeNull();
        newTask!.Subtasks.Should().HaveCount(2, "підзадачі мають бути клоновані");
        newTask.Subtasks.Should().OnlyContain(s => !s.IsDone, 
            "всі підзадачі в новій задачі мають бути не виконані");
        newTask.Subtasks.Select(s => s.Title).Should().BeEquivalentTo(
            new[] { "Review goals", "Plan tasks" });
    }

    // ============================================
    // ТЕСТ 12: Видалення subtask
    // ============================================
    
    /// <summary>
    /// Перевіряємо, що підзадачу можна видалити.
    /// </summary>
    [Fact]
    public async Task DeleteSubtaskAsync_ShouldRemoveSubtask()
    {
        // Arrange
        var task = await _taskService.CreateTaskAsync(_testUserId, new CreateTaskDto
        {
            Title = "Task"
        });
        
        var subtask1 = await _taskService.AddSubtaskAsync(_testUserId, task.Id, 
            new CreateSubtaskDto { Title = "Subtask 1" });
        var subtask2 = await _taskService.AddSubtaskAsync(_testUserId, task.Id, 
            new CreateSubtaskDto { Title = "Subtask 2" });

        // Act - видаляємо першу підзадачу
        await _taskService.DeleteSubtaskAsync(_testUserId, task.Id, subtask1.Id);

        // Assert
        var taskAfterDelete = await _taskService.GetTaskByIdAsync(_testUserId, task.Id);
        taskAfterDelete.Subtasks.Should().HaveCount(1);
        taskAfterDelete.Subtasks.First().Title.Should().Be("Subtask 2");
    }

    // ============================================
    // ТЕСТ 13: Оновлення задачі оновлює UpdatedAt
    // ============================================
    
    /// <summary>
    /// Перевіряємо, що при будь-яких змінах задачі оновлюється timestamp.
    /// </summary>
    [Fact]
    public async Task UpdateTaskAsync_ShouldUpdateTimestamp()
    {
        // Arrange
        var task = await _taskService.CreateTaskAsync(_testUserId, new CreateTaskDto
        {
            Title = "Original"
        });

        var originalUpdatedAt = task.UpdatedAt;
        await Task.Delay(100);

        // Act
        var updated = await _taskService.UpdateTaskAsync(_testUserId, task.Id, new UpdateTaskDto
        {
            Title = "Updated",
            Status = TasksStatus.InProgress,
            Priority = TaskPriority.High
        });

        // Assert
        updated.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    // ============================================
    // ТЕСТ 14: Неможливість оновити чужу задачу
    // ============================================
    
    /// <summary>
    /// БЕЗПЕКА: Користувач не може оновити задачу іншого користувача.
    /// </summary>
    [Fact]
    public async Task UpdateTaskAsync_OtherUsersTask_ShouldThrowException()
    {
        // Arrange
        var otherUserTask = await _taskService.CreateTaskAsync(_otherUserId, new CreateTaskDto
        {
            Title = "Other User Task"
        });

        // Act & Assert
        var act = async () => await _taskService.UpdateTaskAsync(_testUserId, otherUserTask.Id, 
            new UpdateTaskDto { Title = "Hacked", Status = TasksStatus.Done, Priority = TaskPriority.None });
        
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ============================================
    // Helper Methods
    // ============================================
    
    private DateTime GetNextDayOfWeek(DayOfWeek day)
    {
        var today = DateTime.UtcNow.Date;
        int daysUntil = ((int)day - (int)today.DayOfWeek + 7) % 7;
        if (daysUntil == 0) daysUntil = 7; // Якщо сьогодні цей день, беремо наступний тиждень
        return today.AddDays(daysUntil);
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }
}
