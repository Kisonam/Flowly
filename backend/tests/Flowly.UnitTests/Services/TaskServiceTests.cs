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

    [Fact]
    public async Task MoveTaskToThemeAsync_ShouldMoveTaskAndUpdateOrder()
    {
        
        var theme1 = await TestDataSeeder.CreateTestTaskThemeAsync(_context, _testUserId, "Work");
        var theme2 = await TestDataSeeder.CreateTestTaskThemeAsync(_context, _testUserId, "Personal");

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

        var task3 = await _taskService.CreateTaskAsync(_testUserId, new CreateTaskDto
        {
            Title = "Task 3 in Personal",
            ThemeId = theme2.Id
        });

        await _taskService.MoveTaskToThemeAsync(_testUserId, task1.Id, theme2.Id);

        var movedTask = await _taskService.GetTaskByIdAsync(_testUserId, task1.Id);
        movedTask.Theme.Should().NotBeNull();
        movedTask.Theme!.Id.Should().Be(theme2.Id, "задача має бути в новій темі");

        var tasksInTheme2 = await _context.Tasks
            .Where(t => t.TaskThemeId == theme2.Id && t.UserId == _testUserId)
            .OrderBy(t => t.Order)
            .ToListAsync();

        tasksInTheme2.Should().HaveCount(2);
        tasksInTheme2.Last().Id.Should().Be(task1.Id, 
            "переміщена задача має бути в кінці списку");
    }

    [Fact]
    public async Task MoveTaskToThemeAsync_ToNullTheme_ShouldMoveToUnassigned()
    {
        
        var theme = await TestDataSeeder.CreateTestTaskThemeAsync(_context, _testUserId, "Work");
        
        var task = await _taskService.CreateTaskAsync(_testUserId, new CreateTaskDto
        {
            Title = "Task in theme",
            ThemeId = theme.Id
        });

        await _taskService.MoveTaskToThemeAsync(_testUserId, task.Id, null);

        var movedTask = await _taskService.GetTaskByIdAsync(_testUserId, task.Id);
        movedTask.Theme.Should().BeNull("задача має бути без теми");
    }

    [Fact]
    public async Task MoveTaskToThemeAsync_ToOtherUsersTheme_ShouldThrowException()
    {
        
        var otherUserTheme = await TestDataSeeder.CreateTestTaskThemeAsync(
            _context, _otherUserId, "Other User Theme");

        var task = await _taskService.CreateTaskAsync(_testUserId, new CreateTaskDto
        {
            Title = "My Task"
        });

        var act = async () => await _taskService.MoveTaskToThemeAsync(
            _testUserId, task.Id, otherUserTheme.Id);
        
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*", "має бути помилка про неіснуючу тему");
    }

    [Fact]
    public async Task SetRecurrenceAsync_WithDailyRule_ShouldCreateRecurrence()
    {
        
        var task = await _taskService.CreateTaskAsync(_testUserId, new CreateTaskDto
        {
            Title = "Daily Task",
            DueDate = DateTime.UtcNow.Date.AddDays(1)
        });

        var recurrenceDto = new CreateRecurrenceDto
        {
            Rule = "FREQ=DAILY;INTERVAL=1"
        };
        var recurrence = await _taskService.SetRecurrenceAsync(_testUserId, task.Id, recurrenceDto);

        recurrence.Should().NotBeNull();
        recurrence.Rule.Should().Be("FREQ=DAILY;INTERVAL=1");

        var taskWithRecurrence = await _taskService.GetTaskByIdAsync(_testUserId, task.Id);
        taskWithRecurrence.Recurrence.Should().NotBeNull();
        taskWithRecurrence.Recurrence!.Rule.Should().Be("FREQ=DAILY;INTERVAL=1");
    }

    [Fact]
    public async Task CompleteTaskAsync_WithRecurrence_ShouldCreateNextOccurrence()
    {
        
        var dueDate = DateTime.UtcNow.Date.AddDays(1).AddHours(14); 
        var task = await _taskService.CreateTaskAsync(_testUserId, new CreateTaskDto
        {
            Title = "Daily Standup",
            Description = "Team meeting",
            DueDate = dueDate
        });

        await _taskService.SetRecurrenceAsync(_testUserId, task.Id, new CreateRecurrenceDto
        {
            Rule = "FREQ=DAILY;INTERVAL=1"
        });

        await _taskService.CompleteTaskAsync(_testUserId, task.Id);

        var completedTask = await _taskService.GetTaskByIdAsync(_testUserId, task.Id);
        completedTask.Status.Should().Be(TasksStatus.Done);
        completedTask.CompletedAt.Should().NotBeNull();

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

    [Fact]
    public async Task CompleteTaskAsync_WithWeeklyRecurrence_ShouldCreateNextWeek()
    {
        
        var monday = GetNextDayOfWeek(DayOfWeek.Monday);
        var task = await _taskService.CreateTaskAsync(_testUserId, new CreateTaskDto
        {
            Title = "Weekly Review",
            DueDate = monday.AddHours(10)
        });

        await _taskService.SetRecurrenceAsync(_testUserId, task.Id, new CreateRecurrenceDto
        {
            Rule = "FREQ=WEEKLY;INTERVAL=1;BYDAY=MO"
        });

        await _taskService.CompleteTaskAsync(_testUserId, task.Id);

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

    [Fact]
    public async Task AddSubtaskAsync_ShouldCreateSubtask()
    {
        
        var task = await _taskService.CreateTaskAsync(_testUserId, new CreateTaskDto
        {
            Title = "Main Task"
        });

        var subtaskDto = new CreateSubtaskDto
        {
            Title = "Subtask 1"
        };
        var subtask = await _taskService.AddSubtaskAsync(_testUserId, task.Id, subtaskDto);

        subtask.Should().NotBeNull();
        subtask.Title.Should().Be("Subtask 1");
        subtask.IsDone.Should().BeFalse("нова підзадача не має бути виконана");

        var taskWithSubtasks = await _taskService.GetTaskByIdAsync(_testUserId, task.Id);
        taskWithSubtasks.Subtasks.Should().HaveCount(1);
        taskWithSubtasks.Subtasks.First().Title.Should().Be("Subtask 1");
    }

    [Fact]
    public async Task AddSubtaskAsync_Multiple_ShouldMaintainOrder()
    {
        
        var task = await _taskService.CreateTaskAsync(_testUserId, new CreateTaskDto
        {
            Title = "Task with multiple subtasks"
        });

        var subtask1 = await _taskService.AddSubtaskAsync(_testUserId, task.Id, 
            new CreateSubtaskDto { Title = "First" });
        var subtask2 = await _taskService.AddSubtaskAsync(_testUserId, task.Id, 
            new CreateSubtaskDto { Title = "Second" });
        var subtask3 = await _taskService.AddSubtaskAsync(_testUserId, task.Id, 
            new CreateSubtaskDto { Title = "Third" });

        var taskWithSubtasks = await _taskService.GetTaskByIdAsync(_testUserId, task.Id);
        taskWithSubtasks.Subtasks.Should().HaveCount(3);

        taskWithSubtasks.Subtasks[0].Title.Should().Be("First");
        taskWithSubtasks.Subtasks[1].Title.Should().Be("Second");
        taskWithSubtasks.Subtasks[2].Title.Should().Be("Third");

        taskWithSubtasks.Subtasks[0].Order.Should().BeLessThan(taskWithSubtasks.Subtasks[1].Order);
        taskWithSubtasks.Subtasks[1].Order.Should().BeLessThan(taskWithSubtasks.Subtasks[2].Order);
    }

    [Fact]
    public async Task ToggleSubtaskAsync_ShouldChangeStatus()
    {
        
        var task = await _taskService.CreateTaskAsync(_testUserId, new CreateTaskDto
        {
            Title = "Task"
        });
        
        var subtask = await _taskService.AddSubtaskAsync(_testUserId, task.Id, 
            new CreateSubtaskDto { Title = "Subtask" });

        subtask.IsDone.Should().BeFalse("початковий статус - не виконано");

        var toggled1 = await _taskService.ToggleSubtaskAsync(_testUserId, task.Id, subtask.Id);

        toggled1.IsDone.Should().BeTrue("після toggle має бути виконано");
        toggled1.CompletedAt.Should().NotBeNull("має бути встановлена дата завершення");

        var toggled2 = await _taskService.ToggleSubtaskAsync(_testUserId, task.Id, subtask.Id);

        toggled2.IsDone.Should().BeFalse("після другого toggle має бути не виконано");
        toggled2.CompletedAt.Should().BeNull("дата завершення має бути очищена");
    }

    [Fact]
    public async Task AddSubtaskAsync_ToOtherUsersTask_ShouldThrowException()
    {
        
        var otherUserTask = await _taskService.CreateTaskAsync(_otherUserId, new CreateTaskDto
        {
            Title = "Other User Task"
        });

        var act = async () => await _taskService.AddSubtaskAsync(
            _testUserId, 
            otherUserTask.Id, 
            new CreateSubtaskDto { Title = "Malicious Subtask" });
        
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task CompleteTaskAsync_WithRecurrenceAndSubtasks_ShouldCloneSubtasks()
    {
        
        var task = await _taskService.CreateTaskAsync(_testUserId, new CreateTaskDto
        {
            Title = "Weekly Planning",
            DueDate = DateTime.UtcNow.Date.AddDays(1)
        });

        await _taskService.AddSubtaskAsync(_testUserId, task.Id, 
            new CreateSubtaskDto { Title = "Review goals" });
        await _taskService.AddSubtaskAsync(_testUserId, task.Id, 
            new CreateSubtaskDto { Title = "Plan tasks" });

        await _taskService.SetRecurrenceAsync(_testUserId, task.Id, 
            new CreateRecurrenceDto { Rule = "FREQ=WEEKLY;INTERVAL=1" });

        var taskWithSubtasks = await _taskService.GetTaskByIdAsync(_testUserId, task.Id);
        await _taskService.ToggleSubtaskAsync(_testUserId, task.Id, 
            taskWithSubtasks.Subtasks[0].Id);

        await _taskService.CompleteTaskAsync(_testUserId, task.Id);

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

    [Fact]
    public async Task DeleteSubtaskAsync_ShouldRemoveSubtask()
    {
        
        var task = await _taskService.CreateTaskAsync(_testUserId, new CreateTaskDto
        {
            Title = "Task"
        });
        
        var subtask1 = await _taskService.AddSubtaskAsync(_testUserId, task.Id, 
            new CreateSubtaskDto { Title = "Subtask 1" });
        var subtask2 = await _taskService.AddSubtaskAsync(_testUserId, task.Id, 
            new CreateSubtaskDto { Title = "Subtask 2" });

        await _taskService.DeleteSubtaskAsync(_testUserId, task.Id, subtask1.Id);

        var taskAfterDelete = await _taskService.GetTaskByIdAsync(_testUserId, task.Id);
        taskAfterDelete.Subtasks.Should().HaveCount(1);
        taskAfterDelete.Subtasks.First().Title.Should().Be("Subtask 2");
    }

    [Fact]
    public async Task UpdateTaskAsync_ShouldUpdateTimestamp()
    {
        
        var task = await _taskService.CreateTaskAsync(_testUserId, new CreateTaskDto
        {
            Title = "Original"
        });

        var originalUpdatedAt = task.UpdatedAt;
        await Task.Delay(100);

        var updated = await _taskService.UpdateTaskAsync(_testUserId, task.Id, new UpdateTaskDto
        {
            Title = "Updated",
            Status = TasksStatus.InProgress,
            Priority = TaskPriority.High
        });

        updated.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public async Task UpdateTaskAsync_OtherUsersTask_ShouldThrowException()
    {
        
        var otherUserTask = await _taskService.CreateTaskAsync(_otherUserId, new CreateTaskDto
        {
            Title = "Other User Task"
        });

        var act = async () => await _taskService.UpdateTaskAsync(_testUserId, otherUserTask.Id, 
            new UpdateTaskDto { Title = "Hacked", Status = TasksStatus.Done, Priority = TaskPriority.None });
        
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private DateTime GetNextDayOfWeek(DayOfWeek day)
    {
        var today = DateTime.UtcNow.Date;
        int daysUntil = ((int)day - (int)today.DayOfWeek + 7) % 7;
        if (daysUntil == 0) daysUntil = 7; 
        return today.AddDays(daysUntil);
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }
}
