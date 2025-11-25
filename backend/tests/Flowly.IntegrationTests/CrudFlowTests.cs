using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Flowly.Application.DTOs.Auth;
using Flowly.Application.DTOs.Notes;
using Flowly.Application.DTOs.Tasks;
using Flowly.Domain.Enums;
using Flowly.IntegrationTests.Helpers;
using Xunit;

namespace Flowly.IntegrationTests;

/// <summary>
/// Інтеграційні тести для CRUD операцій.
/// Тестуємо повні flow створення, читання, оновлення та видалення через API.
/// </summary>
public class CrudFlowTests : IClassFixture<FlowlyWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FlowlyWebApplicationFactory _factory;

    public CrudFlowTests(FlowlyWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ============================================
    // ТЕСТ 1: Note CRUD Flow - Create → Get → Update → Archive
    // ============================================
    
    /// <summary>
    /// Тестуємо повний життєвий цикл нотатки:
    /// 1. Створення нової нотатки
    /// 2. Отримання нотатки по ID
    /// 3. Оновлення нотатки
    /// 4. Архівування нотатки
    /// 
    /// Це перевіряє всі основні CRUD операції через реальні HTTP запити.
    /// </summary>
    [Fact]
    public async Task NoteCrudFlow_CreateGetUpdateArchive_ShouldWorkEndToEnd()
    {
        // ============================================
        // ПІДГОТОВКА: Автентифікація
        // ============================================
        
        var authToken = await AuthenticateUserAsync("note.crud@example.com", "TestPass123!");
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", authToken);

        // ============================================
        // КРОК 1: Створення нотатки
        // ============================================
        
        var createDto = new CreateNoteDto
        {
            Title = "My Integration Test Note",
            Markdown = "# Hello World\n\nThis is a test note created via integration test.",
            GroupId = null,
            TagIds = null
        };

        var createResponse = await _client.PostAsJsonAsync("/api/notes", createDto);

        // Assert створення
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            "нотатка має бути успішно створена");

        var createdNote = await createResponse.Content.ReadFromJsonAsync<NoteDto>();
        createdNote.Should().NotBeNull();
        createdNote!.Id.Should().NotBeEmpty();
        createdNote.Title.Should().Be(createDto.Title);
        createdNote.Markdown.Should().Be(createDto.Markdown);
        createdNote.IsArchived.Should().BeFalse("нова нотатка не має бути архівована");
        createdNote.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        var noteId = createdNote.Id;

        // ============================================
        // КРОК 2: Отримання нотатки по ID
        // ============================================
        
        var getResponse = await _client.GetAsync($"/api/notes/{noteId}");

        // Assert отримання
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "нотатка має бути знайдена");

        var retrievedNote = await getResponse.Content.ReadFromJsonAsync<NoteDto>();
        retrievedNote.Should().NotBeNull();
        retrievedNote!.Id.Should().Be(noteId);
        retrievedNote.Title.Should().Be(createDto.Title);
        retrievedNote.Markdown.Should().Be(createDto.Markdown);

        // ============================================
        // КРОК 3: Оновлення нотатки
        // ============================================
        
        var updateDto = new UpdateNoteDto
        {
            Title = "Updated Title",
            Markdown = "# Updated Content\n\nThis note has been updated.",
            GroupId = null,
            TagIds = null
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/notes/{noteId}", updateDto);

        // Assert оновлення
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "нотатка має бути успішно оновлена");

        var updatedNote = await updateResponse.Content.ReadFromJsonAsync<NoteDto>();
        updatedNote.Should().NotBeNull();
        updatedNote!.Title.Should().Be(updateDto.Title);
        updatedNote.Markdown.Should().Be(updateDto.Markdown);
        updatedNote.UpdatedAt.Should().BeAfter(updatedNote.CreatedAt,
            "UpdatedAt має бути оновлений");

        // ============================================
        // КРОК 4: Архівування нотатки
        // ============================================
        
        var archiveResponse = await _client.PostAsync($"/api/notes/{noteId}/archive", null);

        // Assert архівування
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "нотатка має бути успішно архівована");

        // Перевіряємо, що нотатка тепер архівована
        var archivedNoteResponse = await _client.GetAsync($"/api/notes/{noteId}");
        var archivedNote = await archivedNoteResponse.Content.ReadFromJsonAsync<NoteDto>();
        archivedNote!.IsArchived.Should().BeTrue("нотатка має бути архівована");
    }

    // ============================================
    // ТЕСТ 2: Task CRUD Flow - Create → Add Subtask → Complete
    // ============================================
    
    /// <summary>
    /// Тестуємо повний життєвий цикл задачі:
    /// 1. Створення нової задачі
    /// 2. Додавання підзадачі
    /// 3. Завершення задачі
    /// 
    /// Це перевіряє роботу з ієрархічними структурами (task + subtasks).
    /// </summary>
    [Fact]
    public async Task TaskCrudFlow_CreateAddSubtaskComplete_ShouldWorkEndToEnd()
    {
        // ============================================
        // ПІДГОТОВКА: Автентифікація
        // ============================================
        
        var authToken = await AuthenticateUserAsync("task.crud@example.com", "TestPass123!");
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", authToken);

        // ============================================
        // КРОК 1: Створення задачі
        // ============================================
        
        var createDto = new CreateTaskDto
        {
            Title = "Integration Test Task",
            Description = "This is a test task created via integration test",
            Priority = TaskPriority.High,
            DueDate = DateTime.UtcNow.Date.AddDays(7),
            ThemeId = null
        };

        var createResponse = await _client.PostAsJsonAsync("/api/tasks", createDto);

        // Assert створення
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            "задача має бути успішно створена");

        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskDto>();
        createdTask.Should().NotBeNull();
        createdTask!.Id.Should().NotBeEmpty();
        createdTask.Title.Should().Be(createDto.Title);
        createdTask.Description.Should().Be(createDto.Description);
        createdTask.Priority.Should().Be(createDto.Priority);
        createdTask.Status.Should().Be(TasksStatus.Todo, "нова задача має бути в статусі Todo");
        createdTask.Subtasks.Should().BeEmpty("нова задача не має мати підзадач");

        var taskId = createdTask.Id;

        // ============================================
        // КРОК 2: Додавання першої підзадачі
        // ============================================
        
        var subtask1Dto = new CreateSubtaskDto
        {
            Title = "First Subtask"
        };

        var subtask1Response = await _client.PostAsJsonAsync(
            $"/api/tasks/{taskId}/subtasks", subtask1Dto);

        // Assert додавання підзадачі
        subtask1Response.StatusCode.Should().Be(HttpStatusCode.Created,
            "підзадача має бути успішно додана");

        var subtask1 = await subtask1Response.Content.ReadFromJsonAsync<SubtaskDto>();
        subtask1.Should().NotBeNull();
        subtask1!.Title.Should().Be(subtask1Dto.Title);
        subtask1.IsDone.Should().BeFalse("нова підзадача не має бути виконана");

        // ============================================
        // КРОК 3: Додавання другої підзадачі
        // ============================================
        
        var subtask2Dto = new CreateSubtaskDto
        {
            Title = "Second Subtask"
        };

        var subtask2Response = await _client.PostAsJsonAsync(
            $"/api/tasks/{taskId}/subtasks", subtask2Dto);

        subtask2Response.StatusCode.Should().Be(HttpStatusCode.Created);

        var subtask2 = await subtask2Response.Content.ReadFromJsonAsync<SubtaskDto>();
        subtask2.Should().NotBeNull();

        // ============================================
        // КРОК 4: Перевірка, що задача має обидві підзадачі
        // ============================================
        
        var getTaskResponse = await _client.GetAsync($"/api/tasks/{taskId}");
        var taskWithSubtasks = await getTaskResponse.Content.ReadFromJsonAsync<TaskDto>();
        
        taskWithSubtasks!.Subtasks.Should().HaveCount(2,
            "задача має мати 2 підзадачі");
        taskWithSubtasks.Subtasks.Should().Contain(s => s.Title == "First Subtask");
        taskWithSubtasks.Subtasks.Should().Contain(s => s.Title == "Second Subtask");

        // ============================================
        // КРОК 5: Виконання першої підзадачі
        // ============================================
        
        var toggleResponse = await _client.PatchAsync(
            $"/api/tasks/{taskId}/subtasks/{subtask1.Id}/toggle", null);

        toggleResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "підзадача має бути успішно виконана");

        var toggledSubtask = await toggleResponse.Content.ReadFromJsonAsync<SubtaskDto>();
        toggledSubtask!.IsDone.Should().BeTrue("підзадача має бути виконана");
        toggledSubtask.CompletedAt.Should().NotBeNull("має бути встановлена дата завершення");

        // ============================================
        // КРОК 6: Завершення основної задачі
        // ============================================
        
        var completeResponse = await _client.PatchAsync($"/api/tasks/{taskId}/complete", null);

        // Assert завершення
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "задача має бути успішно завершена");

        var completedTask = await completeResponse.Content.ReadFromJsonAsync<TaskDto>();
        completedTask!.Status.Should().Be(TasksStatus.Done,
            "задача має бути в статусі Done");
        completedTask.CompletedAt.Should().NotBeNull("має бути встановлена дата завершення");
        completedTask.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    // ============================================
    // ТЕСТ 3: Неможливість отримати чужу нотатку
    // ============================================
    
    /// <summary>
    /// БЕЗПЕКА: Перевіряємо, що користувач не може отримати нотатку іншого користувача.
    /// </summary>
    [Fact]
    public async Task GetNote_FromAnotherUser_ShouldReturn404()
    {
        // Створюємо двох користувачів
        var user1Token = await AuthenticateUserAsync("user1@example.com", "Pass123!");
        var user2Token = await AuthenticateUserAsync("user2@example.com", "Pass123!");

        // User1 створює нотатку
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", user1Token);

        var createDto = new CreateNoteDto
        {
            Title = "User1 Private Note",
            Markdown = "This is private"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/notes", createDto);
        var createdNote = await createResponse.Content.ReadFromJsonAsync<NoteDto>();
        var noteId = createdNote!.Id;

        // User2 намагається отримати нотатку User1
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", user2Token);

        var getResponse = await _client.GetAsync($"/api/notes/{noteId}");

        // Assert - має бути 404 (не розкриваємо, що нотатка існує)
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "користувач не може отримати чужу нотатку");
    }

    // ============================================
    // ТЕСТ 4: Неможливість оновити чужу задачу
    // ============================================
    
    /// <summary>
    /// БЕЗПЕКА: Перевіряємо, що користувач не може оновити задачу іншого користувача.
    /// </summary>
    [Fact]
    public async Task UpdateTask_FromAnotherUser_ShouldReturn404()
    {
        // Створюємо двох користувачів
        var user1Token = await AuthenticateUserAsync("taskuser1@example.com", "Pass123!");
        var user2Token = await AuthenticateUserAsync("taskuser2@example.com", "Pass123!");

        // User1 створює задачу
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", user1Token);

        var createDto = new CreateTaskDto
        {
            Title = "User1 Private Task",
            Priority = TaskPriority.Medium
        };

        var createResponse = await _client.PostAsJsonAsync("/api/tasks", createDto);
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskDto>();
        var taskId = createdTask!.Id;

        // User2 намагається оновити задачу User1
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", user2Token);

        var updateDto = new UpdateTaskDto
        {
            Title = "Hacked Title",
            Status = TasksStatus.Done,
            Priority = TaskPriority.High
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/tasks/{taskId}", updateDto);

        // Assert - має бути 404
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "користувач не може оновити чужу задачу");

        // Перевіряємо, що задача не змінилася
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", user1Token);

        var getResponse = await _client.GetAsync($"/api/tasks/{taskId}");
        var unchangedTask = await getResponse.Content.ReadFromJsonAsync<TaskDto>();
        unchangedTask!.Title.Should().Be("User1 Private Task",
            "задача не має бути змінена");
    }

    // ============================================
    // Helper Methods
    // ============================================
    
    /// <summary>
    /// Допоміжний метод для автентифікації користувача.
    /// Реєструє нового користувача (якщо потрібно) і повертає access token.
    /// </summary>
    private async Task<string> AuthenticateUserAsync(string email, string password)
    {
        var registerDto = new RegisterDto
        {
            Email = email,
            Password = password,
            DisplayName = email.Split('@')[0]
        };

        // Намагаємося зареєструвати (може вже існувати)
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        
        if (registerResponse.IsSuccessStatusCode)
        {
            var registerResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
            return registerResult!.AccessToken;
        }

        // Якщо реєстрація не вдалася (можливо вже існує), логінимося
        var loginDto = new LoginDto
        {
            Email = email,
            Password = password
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        return loginResult!.AccessToken;
    }
}
