import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, throwError, tap, catchError, map } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  Note,
  CreateNoteRequest,
  UpdateNoteRequest,
  NoteFilter,
  PaginatedResult
} from '../models/note.models';

@Injectable({
  providedIn: 'root'
})
export class NotesService {
  private http = inject(HttpClient);
  private readonly API_URL = `${environment.apiUrl}/notes`;

  /**
   * Get all notes with optional filtering and pagination
   */
  getNotes(filter?: NoteFilter): Observable<PaginatedResult<Note>> {
    let params = new HttpParams();

    if (filter) {
      if (filter.search) {
        params = params.set('search', filter.search);
      }
      if (filter.tagIds && filter.tagIds.length > 0) {
        params = params.set('tagIds', filter.tagIds.join(','));
      }
      if (filter.isArchived !== undefined) {
        params = params.set('isArchived', filter.isArchived.toString());
      }
      if (filter.page) {
        params = params.set('page', filter.page.toString());
      }
      if (filter.pageSize) {
        params = params.set('pageSize', filter.pageSize.toString());
      }
    }

    return this.http.get<PaginatedResult<Note>>(this.API_URL, { params })
      .pipe(
        map(result => this.convertDates(result)),
        tap(result => console.log('✅ Notes fetched:', result)),
        catchError(this.handleError)
      );
  }

  /**
   * Get a single note by ID
   */
  getNoteById(id: string): Observable<Note> {
    return this.http.get<Note>(`${this.API_URL}/${id}`)
      .pipe(
        map(note => this.convertNoteDate(note)),
        tap(note => console.log('✅ Note fetched:', note)),
        catchError(this.handleError)
      );
  }

  /**
   * Get a specific note by ID
   */
  getNote(id: string): Observable<Note> {
    return this.http.get<Note>(`${this.API_URL}/${id}`)
      .pipe(
        map(note => this.convertNoteDate(note)),
        tap(note => console.log('✅ Note fetched:', note)),
        catchError(this.handleError)
      );
  }

  /**
   * Create a new note
   */
  createNote(request: CreateNoteRequest): Observable<Note> {
    return this.http.post<Note>(this.API_URL, request)
      .pipe(
        map(note => this.convertNoteDate(note)),
        tap(note => console.log('✅ Note created:', note)),
        catchError(this.handleError)
      );
  }

  /**
   * Update an existing note
   */
  updateNote(id: string, request: UpdateNoteRequest): Observable<Note> {
    return this.http.put<Note>(`${this.API_URL}/${id}`, request)
      .pipe(
        map(note => this.convertNoteDate(note)),
        tap(note => console.log('✅ Note updated:', note)),
        catchError(this.handleError)
      );
  }

  /**
   * Archive a note (soft delete)
   */
  deleteNote(id: string): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/${id}`)
      .pipe(
        tap(() => console.log('✅ Note archived:', id)),
        catchError(this.handleError)
      );
  }

  /**
   * Restore an archived note
   */
  restoreNote(id: string): Observable<void> {
    return this.http.post<void>(`${this.API_URL}/${id}/restore`, {})
      .pipe(
        tap(() => console.log('✅ Note restored:', id)),
        catchError(this.handleError)
      );
  }

  /**
   * Add a tag to a note
   */
  addTag(noteId: string, tagId: string): Observable<void> {
    return this.http.post<void>(`${this.API_URL}/${noteId}/tags/${tagId}`, {})
      .pipe(
        tap(() => console.log(`✅ Tag ${tagId} added to note ${noteId}`)),
        catchError(this.handleError)
      );
  }

  /**
   * Remove a tag from a note
   */
  removeTag(noteId: string, tagId: string): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/${noteId}/tags/${tagId}`)
      .pipe(
        tap(() => console.log(`✅ Tag ${tagId} removed from note ${noteId}`)),
        catchError(this.handleError)
      );
  }

  /**
   * Upload media asset to a note
   */
  uploadMedia(noteId: string, file: File): Observable<{ mediaUrl: string }> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<{ mediaUrl: string }>(`${this.API_URL}/${noteId}/media`, formData)
      .pipe(
        tap(response => console.log('✅ Media uploaded:', response.mediaUrl)),
        catchError(this.handleError)
      );
  }

  /**
   * Export note as markdown file
   */
  exportMarkdown(noteId: string): Observable<Blob> {
    return this.http.get(`${this.API_URL}/${noteId}/export`, {
      responseType: 'blob',
      observe: 'response'
    })
      .pipe(
        map(response => {
          // Extract filename from Content-Disposition header if available
          const contentDisposition = response.headers.get('Content-Disposition');
          let filename = 'note.md';

          if (contentDisposition) {
            const filenameMatch = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
            if (filenameMatch && filenameMatch[1]) {
              filename = filenameMatch[1].replace(/['"]/g, '');
            }
          }

          // Create blob and download
          const blob = response.body!;
          const url = window.URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = url;
          link.download = filename;
          link.click();
          window.URL.revokeObjectURL(url);

          console.log('✅ Note exported:', filename);
          return blob;
        }),
        catchError(this.handleError)
      );
  }

  // ============================================
  // Private Helper Methods
  // ============================================

  /**
   * Convert date strings to Date objects in paginated result
   */
  private convertDates(result: PaginatedResult<Note>): PaginatedResult<Note> {
    return {
      ...result,
      items: result.items.map(note => this.convertNoteDate(note))
    };
  }

  /**
   * Convert date strings to Date objects in a single note
   */
  private convertNoteDate(note: Note): Note {
    return {
      ...note,
      createdAt: new Date(note.createdAt),
      updatedAt: new Date(note.updatedAt)
    };
  }

  /**
   * Handle HTTP errors
   */
  private handleError(error: any): Observable<never> {
    console.error('❌ Notes service error:', error);

    let errorMessage = 'An error occurred';

    if (error.error?.message) {
      errorMessage = error.error.message;
    } else if (error.message) {
      errorMessage = error.message;
    } else if (error.status === 0) {
      errorMessage = 'Unable to connect to server';
    } else if (error.status === 404) {
      errorMessage = 'Note not found';
    } else if (error.status === 401) {
      errorMessage = 'Unauthorized access';
    } else if (error.status === 403) {
      errorMessage = 'Access forbidden';
    }

    return throwError(() => new Error(errorMessage));
  }
}
