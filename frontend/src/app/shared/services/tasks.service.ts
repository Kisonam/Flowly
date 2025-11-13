import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, catchError, throwError, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TaskListItem } from '../models/tasks.models';

interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

@Injectable({ providedIn: 'root' })
export class TasksService {
  private http = inject(HttpClient);
  private readonly API_URL = `${environment.apiUrl}/tasks`;

  list(options?: { search?: string; isArchived?: boolean; take?: number }): Observable<TaskListItem[]> {
    let params = new HttpParams();
    if (options?.search) params = params.set('search', options.search);
    if (options?.isArchived !== undefined) params = params.set('isArchived', String(options.isArchived));
    if (options?.take) params = params.set('pageSize', String(options.take));
    return this.http.get<PaginatedResult<TaskListItem>>(this.API_URL, { params }).pipe(
      map(result => result.items),
      catchError((error) => {
        const message = error?.error?.message || error?.message || 'Failed to load tasks';
        return throwError(() => new Error(message));
      })
    );
  }
}
