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

public class CrudFlowTests : IClassFixture<FlowlyWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FlowlyWebApplicationFactory _factory;
    private readonly System.Text.Json.JsonSerializerOptions _jsonOptions;

    public CrudFlowTests(FlowlyWebApplicationFactory factory)
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
    public async Task NoteCrudFlow_CreateGetUpdateArchive_ShouldWorkEndToEnd()
    {

        var authToken = await AuthenticateUserAsync("note.crud@example.com", "TestPass123!");
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", authToken);

        var createDto = new CreateNoteDto
        {
            Title = "My Integration Test Note",
            Markdown = "# Hello World\n\nThis is a test note created via integration test.",
            GroupId = null,
            TagIds = null
        };

        var createResponse = await _client.PostAsJsonAsync("/api/notes", createDto, _jsonOptions);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            "нотатка має бути успішно створена");

        var createdNote = await createResponse.Content.ReadFromJsonAsync<NoteDto>(_jsonOptions);
        createdNote.Should().NotBeNull();
        createdNote!.Id.Should().NotBeEmpty();
        createdNote.Title.Should().Be(createDto.Title);
        createdNote.Markdown.Should().Be(createDto.Markdown);
        createdNote.IsArchived.Should().BeFalse("нова нотатка не має бути архівована");
        createdNote.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        var noteId = createdNote.Id;

        var getResponse = await _client.GetAsync($"/api/notes/{noteId}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "нотатка має бути знайдена");

        var retrievedNote = await getResponse.Content.ReadFromJsonAsync<NoteDto>(_jsonOptions);
        retrievedNote.Should().NotBeNull();
        retrievedNote!.Id.Should().Be(noteId);
        retrievedNote.Title.Should().Be(createDto.Title);
        retrievedNote.Markdown.Should().Be(createDto.Markdown);

        var updateDto = new UpdateNoteDto
        {
            Title = "Updated Title",
            Markdown = "# Updated Content\n\nThis note has been updated.",
            GroupId = null,
            TagIds = null
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/notes/{noteId}", updateDto, _jsonOptions);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "нотатка має бути успішно оновлена");

        var updatedNote = await updateResponse.Content.ReadFromJsonAsync<NoteDto>(_jsonOptions);
        updatedNote.Should().NotBeNull();
        updatedNote!.Title.Should().Be(updateDto.Title);
        updatedNote.Markdown.Should().Be(updateDto.Markdown);
        updatedNote.UpdatedAt.Should().BeAfter(updatedNote.CreatedAt,
            "UpdatedAt має бути оновлений");

        var archiveResponse = await _client.DeleteAsync($"/api/notes/{noteId}");

        archiveResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "нотатка має бути успішно архівована");

        var archivedNoteResponse = await _client.GetAsync($"/api/notes/{noteId}");
        var archivedNote = await archivedNoteResponse.Content.ReadFromJsonAsync<NoteDto>(_jsonOptions);
        archivedNote!.IsArchived.Should().BeTrue("нотатка має бути архівована");
    }

    [Fact]
    public async Task TaskCrudFlow_CreateAddSubtaskComplete_ShouldWorkEndToEnd()
    {

        var authToken = await AuthenticateUserAsync("task.crud@example.com", "TestPass123!");
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", authToken);

        var createDto = new CreateTaskDto
        {
            Title = "Integration Test Task",
            Description = "This is a test task created via integration test",
            Priority = TaskPriority.High,
            DueDate = DateTime.UtcNow.Date.AddDays(7),
            ThemeId = null
        };

        var createResponse = await _client.PostAsJsonAsync("/api/tasks", createDto, _jsonOptions);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            "задача має бути успішно створена");

        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskDto>(_jsonOptions);
        createdTask.Should().NotBeNull();
        createdTask!.Id.Should().NotBeEmpty();
        createdTask.Title.Should().Be(createDto.Title);
        createdTask.Description.Should().Be(createDto.Description);
        createdTask.Priority.Should().Be(createDto.Priority);
        createdTask.Status.Should().Be(TasksStatus.Todo, "нова задача має бути в статусі Todo");
        createdTask.Subtasks.Should().BeEmpty("нова задача не має мати підзадач");

        var taskId = createdTask.Id;

        var subtask1Dto = new CreateSubtaskDto
        {
            Title = "First Subtask"
        };

        var subtask1Response = await _client.PostAsJsonAsync(
            $"/api/tasks/{taskId}/subtasks", subtask1Dto, _jsonOptions);

        subtask1Response.StatusCode.Should().Be(HttpStatusCode.Created,
            "підзадача має бути успішно додана");

        var subtask1 = await subtask1Response.Content.ReadFromJsonAsync<SubtaskDto>(_jsonOptions);
        subtask1.Should().NotBeNull();
        subtask1!.Title.Should().Be(subtask1Dto.Title);
        subtask1.IsDone.Should().BeFalse("нова підзадача не має бути виконана");

        var subtask2Dto = new CreateSubtaskDto
        {
            Title = "Second Subtask"
        };

        var subtask2Response = await _client.PostAsJsonAsync(
            $"/api/tasks/{taskId}/subtasks", subtask2Dto, _jsonOptions);

        subtask2Response.StatusCode.Should().Be(HttpStatusCode.Created);

        var subtask2 = await subtask2Response.Content.ReadFromJsonAsync<SubtaskDto>(_jsonOptions);
        subtask2.Should().NotBeNull();

        var getTaskResponse = await _client.GetAsync($"/api/tasks/{taskId}");
        var taskWithSubtasks = await getTaskResponse.Content.ReadFromJsonAsync<TaskDto>(_jsonOptions);
        
        taskWithSubtasks!.Subtasks.Should().HaveCount(2,
            "задача має мати 2 підзадачі");
        taskWithSubtasks.Subtasks.Should().Contain(s => s.Title == "First Subtask");
        taskWithSubtasks.Subtasks.Should().Contain(s => s.Title == "Second Subtask");

        var updateSubtaskDto = new UpdateSubtaskDto
        {
            Title = subtask1.Title,
            IsDone = true
        };

        var toggleResponse = await _client.PutAsJsonAsync(
            $"/api/tasks/{taskId}/subtasks/{subtask1.Id}", updateSubtaskDto, _jsonOptions);

        toggleResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "підзадача має бути успішно виконана");

        var toggledSubtask = await toggleResponse.Content.ReadFromJsonAsync<SubtaskDto>(_jsonOptions);
        toggledSubtask!.IsDone.Should().BeTrue("підзадача має бути виконана");
        toggledSubtask.CompletedAt.Should().NotBeNull("має бути встановлена дата завершення");

        var completeResponse = await _client.PostAsync($"/api/tasks/{taskId}/complete", null);

        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "задача має бути успішно завершена");

        var completedTaskResponse = await _client.GetAsync($"/api/tasks/{taskId}");
        var completedTask = await completedTaskResponse.Content.ReadFromJsonAsync<TaskDto>(_jsonOptions);
        
        completedTask!.Status.Should().Be(TasksStatus.Done,
            "задача має бути в статусі Done");
        completedTask.CompletedAt.Should().NotBeNull("має бути встановлена дата завершення");
        completedTask.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GetNote_FromAnotherUser_ShouldReturn404()
    {
        
        var user1Token = await AuthenticateUserAsync("user1@example.com", "Pass123!");
        var user2Token = await AuthenticateUserAsync("user2@example.com", "Pass123!");

        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", user1Token);

        var createDto = new CreateNoteDto
        {
            Title = "User1 Private Note",
            Markdown = "This is private"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/notes", createDto, _jsonOptions);
        var createdNote = await createResponse.Content.ReadFromJsonAsync<NoteDto>(_jsonOptions);
        var noteId = createdNote!.Id;

        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", user2Token);

        var getResponse = await _client.GetAsync($"/api/notes/{noteId}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "користувач не може отримати чужу нотатку");
    }

    [Fact]
    public async Task UpdateTask_FromAnotherUser_ShouldReturn404()
    {
        
        var user1Token = await AuthenticateUserAsync("taskuser1@example.com", "Pass123!");
        var user2Token = await AuthenticateUserAsync("taskuser2@example.com", "Pass123!");

        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", user1Token);

        var createDto = new CreateTaskDto
        {
            Title = "User1 Private Task",
            Priority = TaskPriority.Medium
        };

        var createResponse = await _client.PostAsJsonAsync("/api/tasks", createDto, _jsonOptions);
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskDto>(_jsonOptions);
        var taskId = createdTask!.Id;

        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", user2Token);

        var updateDto = new UpdateTaskDto
        {
            Title = "Hacked Title",
            Status = TasksStatus.Done,
            Priority = TaskPriority.High
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/tasks/{taskId}", updateDto, _jsonOptions);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "користувач не може оновити чужу задачу");

        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", user1Token);

        var getResponse = await _client.GetAsync($"/api/tasks/{taskId}");
        var unchangedTask = await getResponse.Content.ReadFromJsonAsync<TaskDto>(_jsonOptions);
        unchangedTask!.Title.Should().Be("User1 Private Task",
            "задача не має бути змінена");
    }

    private async Task<string> AuthenticateUserAsync(string email, string password)
    {
        var registerDto = new RegisterDto
        {
            Email = email,
            Password = password,
            ConfirmPassword = password,
            DisplayName = email.Split('@')[0]
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto, _jsonOptions);
        
        if (registerResponse.IsSuccessStatusCode)
        {
            var registerResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>(_jsonOptions);
            return registerResult!.AccessToken;
        }

        var loginDto = new LoginDto
        {
            Email = email,
            Password = password
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto, _jsonOptions);
        
        if (!loginResponse.IsSuccessStatusCode)
        {
            
            var errorContent = await loginResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Authentication failed for {email}. Register status: {registerResponse.StatusCode}, Login status: {loginResponse.StatusCode}. Error: {errorContent}");
        }

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>(_jsonOptions);
        return loginResult!.AccessToken;
    }
}
