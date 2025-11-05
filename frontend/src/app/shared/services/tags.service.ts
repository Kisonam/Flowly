import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, throwError, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Tag } from '../../features/notes/models/note.models';

export interface CreateTagRequest {
  name: string;
  color?: string;
}

export interface UpdateTagRequest {
  name?: string;
  color?: string;
}

@Injectable({ providedIn: 'root' })
export class TagsService {
  private http = inject(HttpClient);
  private readonly API_URL = `${environment.apiUrl}/tags`;

  getTags(): Observable<Tag[]> {
    return this.http.get<Tag[]>(this.API_URL).pipe(
      catchError((error) => {
        const message = error?.error?.message || error?.message || 'Failed to load tags';
        return throwError(() => new Error(message));
      })
    );
  }

  getTagById(id: string): Observable<Tag> {
    return this.http.get<Tag>(`${this.API_URL}/${id}`).pipe(
      catchError((error) => {
        const message = error?.error?.message || error?.message || 'Failed to load tag';
        return throwError(() => new Error(message));
      })
    );
  }

  createTag(request: CreateTagRequest): Observable<Tag> {
    return this.http.post<Tag>(this.API_URL, request).pipe(
      tap(() => console.log('Tag created:', request)),
      catchError((error) => {
        const message = error?.error?.message || error?.message || 'Failed to create tag';
        return throwError(() => new Error(message));
      })
    );
  }

  updateTag(id: string, request: UpdateTagRequest): Observable<Tag> {
    return this.http.put<Tag>(`${this.API_URL}/${id}`, request).pipe(
      tap(() => console.log('Tag updated:', id)),
      catchError((error) => {
        const message = error?.error?.message || error?.message || 'Failed to update tag';
        return throwError(() => new Error(message));
      })
    );
  }

  deleteTag(id: string): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/${id}`).pipe(
      tap(() => console.log('Tag deleted:', id)),
      catchError((error) => {
        const message = error?.error?.message || error?.message || 'Failed to delete tag';
        return throwError(() => new Error(message));
      })
    );
  }
}
