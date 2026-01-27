using FluentAssertions;
using Flowly.Application.DTOs.Notes;
using Flowly.Application.Interfaces;
using Flowly.Infrastructure.Data;
using Flowly.Infrastructure.Services;
using Flowly.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Flowly.UnitTests.Services;

public class NoteServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IArchiveService> _archiveServiceMock;
    private readonly NoteService _noteService;
    private readonly Guid _testUserId;
    private readonly Guid _otherUserId;

    public NoteServiceTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _archiveServiceMock = new Mock<IArchiveService>();
        _noteService = new NoteService(_context, _archiveServiceMock.Object);

        _testUserId = Guid.NewGuid();
        _otherUserId = Guid.NewGuid();
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateNoteSuccessfully()
    {
        
        var createDto = new CreateNoteDto
        {
            Title = "My First Note",
            Markdown = "# Hello World\n\nThis is my first note.",
            GroupId = null,
            TagIds = null
        };

        var result = await _noteService.CreateAsync(_testUserId, createDto);

        result.Should().NotBeNull();
        result.Title.Should().Be("My First Note");
        result.Markdown.Should().Be("# Hello World\n\nThis is my first note.");
        result.IsArchived.Should().BeFalse("нова нотатка не має бути архівована");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var noteInDb = await _context.Notes.FindAsync(result.Id);
        noteInDb.Should().NotBeNull();
        noteInDb!.UserId.Should().Be(_testUserId, "нотатка має належати правильному користувачу");
    }

    [Fact]
    public async Task CreateAsync_WithTags_ShouldAttachTagsToNote()
    {
        
        var tags = await TestDataSeeder.CreateTestTagsAsync(_context, _testUserId, 2);

        var createDto = new CreateNoteDto
        {
            Title = "Tagged Note",
            Markdown = "Note with tags",
            TagIds = tags.Select(t => t.Id).ToList()
        };

        var result = await _noteService.CreateAsync(_testUserId, createDto);

        result.Tags.Should().HaveCount(2, "нотатка має мати 2 теги");
        result.Tags.Select(t => t.Id).Should().BeEquivalentTo(tags.Select(t => t.Id));

        var noteTags = await _context.NoteTags
            .Where(nt => nt.NoteId == result.Id)
            .ToListAsync();
        noteTags.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_WithOtherUsersTags_ShouldThrowException()
    {
        
        var otherUserTags = await TestDataSeeder.CreateTestTagsAsync(_context, _otherUserId, 2);

        var createDto = new CreateNoteDto
        {
            Title = "My Note",
            Markdown = "Trying to use someone else's tags",
            TagIds = otherUserTags.Select(t => t.Id).ToList()
        };

        var act = async () => await _noteService.CreateAsync(_testUserId, createDto);
        
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tags not found*", 
                "має бути помилка про неіснуючі теги (не розкриваємо, що вони чужі)");
    }

    [Theory]
    [InlineData("", "Content", "Title is required")]
    [InlineData("   ", "Content", "Title is required")]
    [InlineData("Title", "", "Content is required")]
    [InlineData("Title", "   ", "Content is required")]
    public async Task CreateAsync_WithInvalidData_ShouldThrowArgumentException(
        string title, string markdown, string expectedMessage)
    {
        
        var createDto = new CreateNoteDto
        {
            Title = title,
            Markdown = markdown
        };

        var act = async () => await _noteService.CreateAsync(_testUserId, createDto);
        
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"*{expectedMessage}*");
    }

    [Fact]
    public async Task ArchiveAsync_ShouldCallArchiveService()
    {
        
        var createDto = new CreateNoteDto
        {
            Title = "Note to Archive",
            Markdown = "This will be archived"
        };
        var note = await _noteService.CreateAsync(_testUserId, createDto);

        await _noteService.ArchiveAsync(_testUserId, note.Id);

        _archiveServiceMock.Verify(
            x => x.ArchiveEntityAsync(
                _testUserId,
                Domain.Enums.LinkEntityType.Note,
                note.Id),
            Times.Once,
            "має бути викликаний метод архівування");
    }

    [Fact]
    public async Task GetAllAsync_FilterByTags_ShouldReturnOnlyMatchingNotes()
    {
        
        var tags = await TestDataSeeder.CreateTestTagsAsync(_context, _testUserId, 3);
        var tag1 = tags[0];
        var tag2 = tags[1];
        var tag3 = tags[2];

        var note1 = await _noteService.CreateAsync(_testUserId, new CreateNoteDto
        {
            Title = "Note 1",
            Markdown = "Content 1",
            TagIds = new List<Guid> { tag1.Id, tag2.Id }
        });

        var note2 = await _noteService.CreateAsync(_testUserId, new CreateNoteDto
        {
            Title = "Note 2",
            Markdown = "Content 2",
            TagIds = new List<Guid> { tag2.Id, tag3.Id }
        });

        var note3 = await _noteService.CreateAsync(_testUserId, new CreateNoteDto
        {
            Title = "Note 3",
            Markdown = "Content 3",
            TagIds = new List<Guid> { tag3.Id }
        });

        var filter = new NoteFilterDto
        {
            TagIds = new List<Guid> { tag2.Id },
            Page = 1,
            PageSize = 10
        };
        var result = await _noteService.GetAllAsync(_testUserId, filter);

        result.Items.Should().HaveCount(2, "тільки 2 нотатки мають tag2");
        result.Items.Select(n => n.Id).Should().Contain(new[] { note1.Id, note2.Id });
        result.Items.Select(n => n.Id).Should().NotContain(note3.Id);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetAllAsync_SearchByText_ShouldReturnMatchingNotes()
    {
        
        await _noteService.CreateAsync(_testUserId, new CreateNoteDto
        {
            Title = "Shopping List",
            Markdown = "Buy milk and bread"
        });

        await _noteService.CreateAsync(_testUserId, new CreateNoteDto
        {
            Title = "Work Notes",
            Markdown = "Meeting at 3pm about project"
        });

        await _noteService.CreateAsync(_testUserId, new CreateNoteDto
        {
            Title = "Recipe",
            Markdown = "How to make bread: mix flour and water"
        });

        var filter = new NoteFilterDto
        {
            Search = "bread",
            Page = 1,
            PageSize = 10
        };
        var result = await _noteService.GetAllAsync(_testUserId, filter);

        result.Items.Should().HaveCount(2, "2 нотатки містять слово 'bread'");
        result.Items.Should().Contain(n => n.Title == "Shopping List");
        result.Items.Should().Contain(n => n.Title == "Recipe");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyUserOwnNotes()
    {
        
        await _noteService.CreateAsync(_testUserId, new CreateNoteDto
        {
            Title = "My Note 1",
            Markdown = "My content 1"
        });

        await _noteService.CreateAsync(_testUserId, new CreateNoteDto
        {
            Title = "My Note 2",
            Markdown = "My content 2"
        });

        await _noteService.CreateAsync(_otherUserId, new CreateNoteDto
        {
            Title = "Other User Note",
            Markdown = "Other user content"
        });

        var filter = new NoteFilterDto { Page = 1, PageSize = 10 };
        var result = await _noteService.GetAllAsync(_testUserId, filter);

        result.Items.Should().HaveCount(2, "користувач має бачити тільки свої 2 нотатки");
        result.Items.Should().OnlyContain(n => n.Title.StartsWith("My Note"));
        result.Items.Should().NotContain(n => n.Title == "Other User Note");
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetByIdAsync_WithOtherUsersNoteId_ShouldThrowException()
    {
        
        var otherUserNote = await _noteService.CreateAsync(_otherUserId, new CreateNoteDto
        {
            Title = "Private Note",
            Markdown = "Confidential information"
        });

        var act = async () => await _noteService.GetByIdAsync(_testUserId, otherUserNote.Id);
        
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*", 
                "має бути загальна помилка без деталей для безпеки");
    }

    [Fact]
    public async Task GetAllAsync_FilterByArchived_ShouldReturnCorrectNotes()
    {
        
        var note1 = await _noteService.CreateAsync(_testUserId, new CreateNoteDto
        {
            Title = "Active Note",
            Markdown = "Active content"
        });

        var note2 = await _noteService.CreateAsync(_testUserId, new CreateNoteDto
        {
            Title = "To Be Archived",
            Markdown = "Will be archived"
        });

        var noteEntity = await _context.Notes.FindAsync(note2.Id);
        noteEntity!.Archive();
        await _context.SaveChangesAsync();

        var activeFilter = new NoteFilterDto
        {
            IsArchived = false,
            Page = 1,
            PageSize = 10
        };
        var activeResult = await _noteService.GetAllAsync(_testUserId, activeFilter);

        var archivedFilter = new NoteFilterDto
        {
            IsArchived = true,
            Page = 1,
            PageSize = 10
        };
        var archivedResult = await _noteService.GetAllAsync(_testUserId, archivedFilter);

        activeResult.Items.Should().HaveCount(1);
        activeResult.Items.First().Id.Should().Be(note1.Id);

        archivedResult.Items.Should().HaveCount(1);
        archivedResult.Items.First().Id.Should().Be(note2.Id);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateTimestamp()
    {
        
        var note = await _noteService.CreateAsync(_testUserId, new CreateNoteDto
        {
            Title = "Original Title",
            Markdown = "Original content"
        });

        var originalUpdatedAt = note.UpdatedAt;

        await Task.Delay(100);

        var updateDto = new UpdateNoteDto
        {
            Title = "Updated Title",
            Markdown = "Updated content"
        };
        var updated = await _noteService.UpdateAsync(_testUserId, note.Id, updateDto);

        updated.Title.Should().Be("Updated Title");
        updated.Markdown.Should().Be("Updated content");
        updated.UpdatedAt.Should().BeAfter(originalUpdatedAt, 
            "UpdatedAt має бути оновлений");
    }

    [Fact]
    public async Task UpdateAsync_WithOtherUsersNote_ShouldThrowException()
    {
        
        var otherUserNote = await _noteService.CreateAsync(_otherUserId, new CreateNoteDto
        {
            Title = "Other User Note",
            Markdown = "Original content"
        });

        var updateDto = new UpdateNoteDto
        {
            Title = "Hacked Title",
            Markdown = "Hacked content"
        };

        var act = async () => await _noteService.UpdateAsync(_testUserId, otherUserNote.Id, updateDto);
        
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");

        var unchangedNote = await _noteService.GetByIdAsync(_otherUserId, otherUserNote.Id);
        unchangedNote.Title.Should().Be("Other User Note", "нотатка не має бути змінена");
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ShouldReturnCorrectPage()
    {
        
        for (int i = 1; i <= 5; i++)
        {
            await _noteService.CreateAsync(_testUserId, new CreateNoteDto
            {
                Title = $"Note {i}",
                Markdown = $"Content {i}"
            });
        }

        var filter = new NoteFilterDto
        {
            Page = 2,
            PageSize = 2
        };
        var result = await _noteService.GetAllAsync(_testUserId, filter);

        result.Items.Should().HaveCount(2, "на сторінці має бути 2 елементи");
        result.TotalCount.Should().Be(5, "загалом має бути 5 нотаток");
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(2);
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }
}
