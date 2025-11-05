import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TaskListItem } from '../models/tasks.models';

@Injectable({ providedIn: 'root' })
export class TasksService {
  private http = inject(HttpClient);
  private readonly API_URL = `${environment.apiUrl}/tasks`;

  list(options?: { search?: string; isArchived?: boolean; take?: number }): Observable<TaskListItem[]> {
    let params = new HttpParams();
    if (options?.search) params = params.set('search', options.search);
    if (options?.isArchived !== undefined) params = params.set('isArchived', String(options.isArchived));
    if (options?.take) params = params.set('take', String(options.take));
    return this.http.get<TaskListItem[]>(this.API_URL, { params }).pipe(
      catchError((error) => {
        const message = error?.error?.message || error?.message || 'Failed to load tasks';
        return throwError(() => new Error(message));
      })
    );
  }
}
