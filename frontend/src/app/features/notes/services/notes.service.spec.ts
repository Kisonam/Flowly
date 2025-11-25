// frontend/src/app/features/notes/services/notes.service.spec.ts

import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { HttpParams } from '@angular/common/http';
import { NotesService } from './notes.service';
import { Note, CreateNoteRequest, NoteFilter, PaginatedResult } from '../models/note.models';
import { environment } from '../../../../environments/environment';

describe('NotesService', () => {
  let service: NotesService;
  let httpMock: HttpTestingController;

  const mockNote: Note = {
    id: '123e4567-e89b-12d3-a456-426614174000',
    title: 'Test Note',
    markdown: '# Test Content',
    isArchived: false,
    tags: [],
    createdAt: new Date('2024-01-01'),
    updatedAt: new Date('2024-01-01')
  };

  const mockPaginatedResult: PaginatedResult<Note> = {
    items: [mockNote],
    totalCount: 1,
    page: 1,
    pageSize: 10,
    totalPages: 1,
    hasPreviousPage: false,
    hasNextPage: false
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [NotesService]
    });

    service = TestBed.inject(NotesService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  // ============================================
  // TEST 1: getNotes calls API with correct params
  // ============================================
  describe('getNotes', () => {
    it('should call API without params when no filter is provided', (done) => {
      // Act
      service.getNotes().subscribe({
        next: (result) => {
          // Assert
          expect(result).toEqual(mockPaginatedResult);
          expect(result.items.length).toBe(1);
          expect(result.items[0].title).toBe('Test Note');
          done();
        },
        error: (error) => {
          fail('getNotes should not fail: ' + error);
          done();
        }
      });

      // Assert HTTP request
      const req = httpMock.expectOne(`${environment.apiUrl}/notes`);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.keys().length).toBe(0); // No query params
      req.flush(mockPaginatedResult);
    });

    it('should call API with search param when filter.search is provided', (done) => {
      // Arrange
      const filter: NoteFilter = {
        search: 'test query'
      };

      // Act
      service.getNotes(filter).subscribe({
        next: (result) => {
          // Assert
          expect(result).toEqual(mockPaginatedResult);
          done();
        }
      });

      // Assert HTTP request
      const req = httpMock.expectOne(req =>
        req.url === `${environment.apiUrl}/notes` &&
        req.params.get('search') === 'test query'
      );
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('search')).toBe('test query');
      req.flush(mockPaginatedResult);
    });

    it('should call API with tagIds param when filter.tagIds is provided', (done) => {
      // Arrange
      const filter: NoteFilter = {
        tagIds: ['tag-1', 'tag-2', 'tag-3']
      };

      // Act
      service.getNotes(filter).subscribe({
        next: (result) => {
          // Assert
          expect(result).toEqual(mockPaginatedResult);
          done();
        }
      });

      // Assert HTTP request
      const req = httpMock.expectOne(req =>
        req.url === `${environment.apiUrl}/notes` &&
        req.params.get('tagIds') === 'tag-1,tag-2,tag-3'
      );
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('tagIds')).toBe('tag-1,tag-2,tag-3');
      req.flush(mockPaginatedResult);
    });

    it('should call API with isArchived param when filter.isArchived is provided', (done) => {
      // Arrange
      const filter: NoteFilter = {
        isArchived: true
      };

      // Act
      service.getNotes(filter).subscribe({
        next: (result) => {
          // Assert
          expect(result).toEqual(mockPaginatedResult);
          done();
        }
      });

      // Assert HTTP request
      const req = httpMock.expectOne(req =>
        req.url === `${environment.apiUrl}/notes` &&
        req.params.get('isArchived') === 'true'
      );
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('isArchived')).toBe('true');
      req.flush(mockPaginatedResult);
    });

    it('should call API with pagination params when filter.page and filter.pageSize are provided', (done) => {
      // Arrange
      const filter: NoteFilter = {
        page: 2,
        pageSize: 20
      };

      // Act
      service.getNotes(filter).subscribe({
        next: (result) => {
          // Assert
          expect(result).toEqual(mockPaginatedResult);
          done();
        }
      });

      // Assert HTTP request
      const req = httpMock.expectOne(req =>
        req.url === `${environment.apiUrl}/notes` &&
        req.params.get('page') === '2' &&
        req.params.get('pageSize') === '20'
      );
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('page')).toBe('2');
      expect(req.request.params.get('pageSize')).toBe('20');
      req.flush(mockPaginatedResult);
    });

    it('should call API with multiple filter params when all filters are provided', (done) => {
      // Arrange
      const filter: NoteFilter = {
        search: 'important',
        tagIds: ['tag-1'],
        isArchived: false,
        page: 1,
        pageSize: 10
      };

      // Act
      service.getNotes(filter).subscribe({
        next: (result) => {
          // Assert
          expect(result).toEqual(mockPaginatedResult);
          done();
        }
      });

      // Assert HTTP request
      const req = httpMock.expectOne(req => {
        return req.url === `${environment.apiUrl}/notes` &&
               req.params.get('search') === 'important' &&
               req.params.get('tagIds') === 'tag-1' &&
               req.params.get('isArchived') === 'false' &&
               req.params.get('page') === '1' &&
               req.params.get('pageSize') === '10';
      });
      expect(req.request.method).toBe('GET');
      req.flush(mockPaginatedResult);
    });

    it('should convert date strings to Date objects in response', (done) => {
      // Arrange
      const mockResponseWithStringDates = {
        items: [{
          ...mockNote,
          createdAt: '2024-01-01T00:00:00Z',
          updatedAt: '2024-01-02T00:00:00Z'
        }],
        totalCount: 1,
        page: 1,
        pageSize: 10,
        totalPages: 1
      };

      // Act
      service.getNotes().subscribe({
        next: (result) => {
          // Assert - dates should be converted to Date objects
          expect(result.items[0].createdAt instanceof Date).toBe(true);
          expect(result.items[0].updatedAt instanceof Date).toBe(true);
          done();
        }
      });

      // Simulate HTTP response
      const req = httpMock.expectOne(`${environment.apiUrl}/notes`);
      req.flush(mockResponseWithStringDates);
    });
  });

  // ============================================
  // TEST 2: createNote posts data
  // ============================================
  describe('createNote', () => {
    it('should POST note data to API and return created note', (done) => {
      // Arrange
      const createRequest: CreateNoteRequest = {
        title: 'New Note',
        markdown: '# New Content',
        tagIds: ['tag-1', 'tag-2']
      };

      const expectedNote: Note = {
        ...mockNote,
        title: 'New Note',
        markdown: '# New Content'
      };

      // Act
      service.createNote(createRequest).subscribe({
        next: (note) => {
          // Assert
          expect(note.title).toBe('New Note');
          expect(note.markdown).toBe('# New Content');
          done();
        },
        error: (error) => {
          fail('createNote should not fail: ' + error);
          done();
        }
      });

      // Assert HTTP request
      const req = httpMock.expectOne(`${environment.apiUrl}/notes`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(createRequest);
      req.flush(expectedNote);
    });

    it('should POST note data without tagIds when not provided', (done) => {
      // Arrange
      const createRequest: CreateNoteRequest = {
        title: 'Simple Note',
        markdown: '# Simple Content'
      };

      // Act
      service.createNote(createRequest).subscribe({
        next: (note) => {
          // Assert
          expect(note).toBeTruthy();
          done();
        }
      });

      // Assert HTTP request
      const req = httpMock.expectOne(`${environment.apiUrl}/notes`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body.title).toBe('Simple Note');
      expect(req.request.body.markdown).toBe('# Simple Content');
      expect(req.request.body.tagIds).toBeUndefined();
      req.flush(mockNote);
    });

    it('should convert date strings to Date objects in created note', (done) => {
      // Arrange
      const createRequest: CreateNoteRequest = {
        title: 'New Note',
        markdown: '# New Content'
      };

      const mockResponseWithStringDates = {
        ...mockNote,
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-01T00:00:00Z'
      };

      // Act
      service.createNote(createRequest).subscribe({
        next: (note) => {
          // Assert - dates should be converted to Date objects
          expect(note.createdAt instanceof Date).toBe(true);
          expect(note.updatedAt instanceof Date).toBe(true);
          done();
        }
      });

      // Simulate HTTP response
      const req = httpMock.expectOne(`${environment.apiUrl}/notes`);
      req.flush(mockResponseWithStringDates);
    });

    it('should handle API error when creating note fails', (done) => {
      // Arrange
      const createRequest: CreateNoteRequest = {
        title: 'New Note',
        markdown: '# New Content'
      };

      const mockError = {
        status: 400,
        statusText: 'Bad Request',
        error: { message: 'Invalid note data' }
      };

      // Act
      service.createNote(createRequest).subscribe({
        next: () => {
          fail('createNote should fail with error');
          done();
        },
        error: (error) => {
          // Assert
          expect(error).toBeTruthy();
          expect(error.message).toBe('Invalid note data');
          done();
        }
      });

      // Simulate HTTP error response
      const req = httpMock.expectOne(`${environment.apiUrl}/notes`);
      req.flush(mockError.error, { status: mockError.status, statusText: mockError.statusText });
    });
  });

  // ============================================
  // Additional Tests
  // ============================================
  describe('getNoteById', () => {
    it('should GET note by ID from API', (done) => {
      // Arrange
      const noteId = '123e4567-e89b-12d3-a456-426614174000';

      // Act
      service.getNoteById(noteId).subscribe({
        next: (note) => {
          // Assert
          expect(note).toEqual(mockNote);
          expect(note.id).toBe(noteId);
          done();
        }
      });

      // Assert HTTP request
      const req = httpMock.expectOne(`${environment.apiUrl}/notes/${noteId}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockNote);
    });
  });

  describe('deleteNote', () => {
    it('should DELETE note by ID', (done) => {
      // Arrange
      const noteId = '123e4567-e89b-12d3-a456-426614174000';

      // Act
      service.deleteNote(noteId).subscribe({
        next: () => {
          // Assert - should complete successfully
          expect(true).toBe(true);
          done();
        }
      });

      // Assert HTTP request
      const req = httpMock.expectOne(`${environment.apiUrl}/notes/${noteId}`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });
});
