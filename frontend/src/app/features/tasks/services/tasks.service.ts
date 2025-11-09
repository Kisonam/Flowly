import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, throwError, map, catchError, tap } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  Task,
  TaskTheme,
  Subtask,
  Recurrence,
  CreateTaskRequest,
  UpdateTaskRequest,
  CreateSubtaskRequest,
  UpdateSubtaskRequest,
  CreateTaskThemeRequest,
  UpdateTaskThemeRequest,
  CreateRecurrenceRequest,
  TaskFilter,
  PaginatedResult
} from '../models/task.models';

@Injectable({ providedIn: 'root' })
export class TasksService {
  private http = inject(HttpClient);
  private readonly API_URL = `${environment.apiUrl}/tasks`;

  // ============================================
  // Tasks CRUD
  // ============================================

  /** Get tasks with full filtering + pagination */
  getTasks(filter?: TaskFilter): Observable<PaginatedResult<Task>> {
    let params = new HttpParams();

    if (filter) {
  if (filter.search) params = params.set('search', filter.search);
      if (filter.tagIds?.length) params = params.set('tagIds', filter.tagIds.join(','));
      if (filter.themeIds?.length) params = params.set('themeIds', filter.themeIds.join(','));
      if (filter.status) params = params.set('status', filter.status);
      if (filter.priority) params = params.set('priority', filter.priority);
      if (filter.isArchived !== undefined) params = params.set('isArchived', String(filter.isArchived));
      if (filter.isOverdue !== undefined) params = params.set('isOverdue', String(filter.isOverdue));
      // Normalize dates to ISO (UTC) to ensure consistent backend parsing
      if (filter.dueDateOn) {
        const onIso = this.toIsoStartOfDay(filter.dueDateOn);
        params = params.set('dueDateOn', onIso);
      }
      if (filter.dueDateTo) {
        // Interpret date-only values as end-of-day in the user's local time, then convert to UTC
        const toIso = this.toIsoEndOfDay(filter.dueDateTo);
        params = params.set('dueDateTo', toIso);
      }
      if (filter.page) params = params.set('page', String(filter.page));
      if (filter.pageSize) params = params.set('pageSize', String(filter.pageSize));
    }

    return this.http.get<PaginatedResult<Task>>(this.API_URL, { params }).pipe(
      map(result => this.convertPagedDates(result)),
      tap(result => console.log('✅ Tasks fetched:', result)),
      catchError(this.handleError)
    );
  }

  /** Get single task */
  getTask(id: string): Observable<Task> {
    return this.http.get<Task>(`${this.API_URL}/${id}`).pipe(
      map(task => this.convertTaskDates(task)),
      tap(task => console.log('✅ Task fetched:', task)),
      catchError(this.handleError)
    );
  }

  /** Create task */
  createTask(dto: CreateTaskRequest): Observable<Task> {
    console.log('📤 TasksService.createTask called with:', dto);
    return this.http.post<Task>(this.API_URL, dto).pipe(
      map(task => this.convertTaskDates(task)),
      tap(task => console.log('✅ Task created:', task)),
      catchError(this.handleError)
    );
  }

  /** Update task */
  updateTask(id: string, dto: UpdateTaskRequest): Observable<Task> {
    return this.http.put<Task>(`${this.API_URL}/${id}`, dto).pipe(
      map(task => this.convertTaskDates(task)),
      tap(task => console.log('✅ Task updated:', task)),
      catchError(this.handleError)
    );
  }

  /** Archive task */
  archiveTask(id: string): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/${id}`).pipe(
      tap(() => console.log('✅ Task archived:', id)),
      catchError(this.handleError)
    );
  }

  // ============================================
  // Themes
  // ============================================

  getThemes(): Observable<TaskTheme[]> {
    return this.http.get<TaskTheme[]>(`${this.API_URL}/themes`).pipe(
      tap(list => console.log('✅ Themes fetched:', list)),
      catchError(this.handleError)
    );
  }

  createTheme(dto: CreateTaskThemeRequest): Observable<TaskTheme> {
    return this.http.post<TaskTheme>(`${this.API_URL}/themes`, dto).pipe(
      tap(theme => console.log('✅ Theme created:', theme)),
      catchError(this.handleError)
    );
  }

  updateTheme(id: string, dto: UpdateTaskThemeRequest): Observable<TaskTheme> {
    return this.http.put<TaskTheme>(`${this.API_URL}/themes/${id}`, dto).pipe(
      tap(theme => console.log('✅ Theme updated:', theme)),
      catchError(this.handleError)
    );
  }

  deleteTheme(id: string): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/themes/${id}`).pipe(
      tap(() => console.log('✅ Theme deleted:', id)),
      catchError(this.handleError)
    );
  }

  reorderThemes(themeIds: string[]): Observable<void> {
    return this.http.post<void>(`${this.API_URL}/themes/reorder`, themeIds).pipe(
      tap(() => console.log('✅ Themes reordered')),
      catchError(this.handleError)
    );
  }

  moveTaskToTheme(taskId: string, themeId?: string | null): Observable<void> {
    const target = themeId ? themeId : 'null';
    return this.http.post<void>(`${this.API_URL}/${taskId}/move/${target}`, {}).pipe(
      tap(() => console.log(`✅ Task ${taskId} moved to theme ${themeId}`)),
      catchError(this.handleError)
    );
  }

  // ============================================
  // Ordering & status helpers
  // ============================================

  reorderTasks(items: { taskId: string; themeId?: string | null; order: number }[]): Observable<void> {
    return this.http.post<void>(`${this.API_URL}/reorder`, { items }).pipe(
      tap(() => console.log('✅ Tasks reordered')),
      catchError(this.handleError)
    );
  }

  completeTask(taskId: string): Observable<void> {
    return this.http.post<void>(`${this.API_URL}/${taskId}/complete`, {}).pipe(
      tap(() => console.log(`✅ Task completed: ${taskId}`)),
      catchError(this.handleError)
    );
  }

  changeStatus(taskId: string, status: 'Todo' | 'InProgress' | 'Done'): Observable<void> {
    return this.http.post<void>(`${this.API_URL}/${taskId}/status/${status}`, {}).pipe(
      tap(() => console.log(`✅ Task ${taskId} status -> ${status}`)),
      catchError(this.handleError)
    );
  }

  // ============================================
  // Subtasks
  // ============================================

  addSubtask(taskId: string, dto: CreateSubtaskRequest): Observable<Subtask> {
    return this.http.post<Subtask>(`${this.API_URL}/${taskId}/subtasks`, dto).pipe(
      tap(subtask => console.log('✅ Subtask added:', subtask)),
      catchError(this.handleError)
    );
  }

  updateSubtask(taskId: string, subtaskId: string, dto: UpdateSubtaskRequest): Observable<Subtask> {
    return this.http.put<Subtask>(`${this.API_URL}/${taskId}/subtasks/${subtaskId}`, dto).pipe(
      tap(subtask => console.log('✅ Subtask updated:', subtask)),
      catchError(this.handleError)
    );
  }

  deleteSubtask(taskId: string, subtaskId: string): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/${taskId}/subtasks/${subtaskId}`).pipe(
      tap(() => console.log('✅ Subtask deleted:', subtaskId)),
      catchError(this.handleError)
    );
  }

  // ============================================
  // Recurrence
  // ============================================

  setRecurrence(taskId: string, dto: CreateRecurrenceRequest): Observable<Recurrence> {
    return this.http.put<Recurrence>(`${this.API_URL}/${taskId}/recurrence`, dto).pipe(
      tap(r => console.log('✅ Recurrence set:', r)),
      catchError(this.handleError)
    );
  }

  // ============================================
  // Tags
  // ============================================

  addTag(taskId: string, tagId: string): Observable<void> {
    return this.http.post<void>(`${this.API_URL}/${taskId}/tags/${tagId}`, {}).pipe(
      tap(() => console.log(`✅ Tag ${tagId} added to task ${taskId}`)),
      catchError(this.handleError)
    );
  }

  removeTag(taskId: string, tagId: string): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/${taskId}/tags/${tagId}`).pipe(
      tap(() => console.log(`✅ Tag ${tagId} removed from task ${taskId}`)),
      catchError(this.handleError)
    );
  }

  // ============================================
  // Helpers
  // ============================================

  private convertPagedDates(result: PaginatedResult<Task>): PaginatedResult<Task> {
    return { ...result, items: result.items.map(t => this.convertTaskDates(t)) };
  }

  private convertTaskDates(task: Task): Task {
    return {
      ...task,
      createdAt: new Date(task.createdAt),
      updatedAt: new Date(task.updatedAt),
      completedAt: task.completedAt ? new Date(task.completedAt) : null,
      dueDate: task.dueDate ? new Date(task.dueDate) : null,
      recurrence: task.recurrence ? {
        ...task.recurrence,
        createdAt: new Date(task.recurrence.createdAt),
        lastOccurrence: task.recurrence.lastOccurrence ? new Date(task.recurrence.lastOccurrence) : null,
        nextOccurrence: task.recurrence.nextOccurrence ? new Date(task.recurrence.nextOccurrence) : null
      } : undefined,
      subtasks: task.subtasks.map(s => ({
        ...s,
        createdAt: new Date(s.createdAt),
        completedAt: s.completedAt ? new Date(s.completedAt) : null
      }))
    };
  }

  private handleError(error: any): Observable<never> {
    // Log detailed server error payload for diagnostics
    const problemErrors = error?.error?.errors;
    console.error('❌ Tasks service error:', {
      status: error.status,
      statusText: error.statusText,
      url: error.url,
      message: error.message,
      server: error.error,
      validation: problemErrors || null
    });
    let message = 'An error occurred';
    if (error.error?.message) message = error.error.message;
    else if (error.message) message = error.message;
    else if (error.status === 0) message = 'Unable to connect to server';
    else if (error.status === 404) message = 'Resource not found';
    else if (error.status === 401) message = 'Unauthorized';
    else if (error.status === 403) message = 'Forbidden';
    // Show first validation error if available
    if (problemErrors) {
      const first = Object.values(problemErrors).flat()[0];
      if (first) message = first as string;
    }
    return throwError(() => new Error(message));
  }

  /** Convert value to ISO string at end-of-day (23:59:59Z). Always returns UTC ISO with 'Z'. */
  private toIsoEndOfDay(value: string | Date): string {
    if (typeof value === 'string') {
      const dateOnlyMatch = /^\d{4}-\d{2}-\d{2}$/.test(value);
      if (dateOnlyMatch) {
        // Construct local date at 23:59:59
        const [y, m, d] = value.split('-').map(Number);
        const local = new Date(y, (m - 1), d, 23, 59, 59, 0);
        if (isNaN(local.getTime())) return value;
        return new Date(Date.UTC(
          local.getUTCFullYear(),
          local.getUTCMonth(),
          local.getUTCDate(),
          local.getUTCHours(),
          local.getUTCMinutes(),
          local.getUTCSeconds()
        )).toISOString(); // keep trailing 'Z'
      }
    }
    // Fallback: keep original conversion
    return this.toIsoDate(value);
  }

  /** Convert value to ISO string at start-of-day (00:00:00Z). Always returns UTC ISO with 'Z'. */
  private toIsoStartOfDay(value: string | Date): string {
    if (typeof value === 'string') {
      const dateOnlyMatch = /^\d{4}-\d{2}-\d{2}$/.test(value);
      if (dateOnlyMatch) {
        const [y, m, d] = value.split('-').map(Number);
        const local = new Date(y, (m - 1), d, 0, 0, 0, 0);
        if (isNaN(local.getTime())) return value;
        return new Date(Date.UTC(
          local.getUTCFullYear(),
          local.getUTCMonth(),
          local.getUTCDate(),
          local.getUTCHours(),
          local.getUTCMinutes(),
          local.getUTCSeconds()
        )).toISOString();
      }
    }
    return this.toIsoDate(value);
  }

  /** Convert string or Date to ISO with timezone Z for backend query (keeps seconds, may include .000Z) */
  private toIsoDate(value: string | Date): string {
    const dateObj = typeof value === 'string' ? new Date(value) : value;
    // If invalid date, skip normalization and return original string to let backend handle
    if (isNaN(dateObj.getTime())) {
      return typeof value === 'string' ? value : '';
    }
    // Ensure UTC and include 'Z'
    return new Date(Date.UTC(
      dateObj.getUTCFullYear(),
      dateObj.getUTCMonth(),
      dateObj.getUTCDate(),
      dateObj.getUTCHours(),
      dateObj.getUTCMinutes(),
      dateObj.getUTCSeconds()
    )).toISOString();
  }
}
