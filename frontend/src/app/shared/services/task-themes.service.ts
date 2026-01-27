import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TaskTheme, CreateTaskTheme, UpdateTaskTheme } from '../models/task-theme.models';

@Injectable({
  providedIn: 'root'
})
export class TaskThemesService {
  private http = inject(HttpClient);
  
  private apiUrl = `${environment.apiUrl}/task-themes`;

  getThemes(): Observable<TaskTheme[]> {
    return this.http.get<TaskTheme[]>(this.apiUrl);
  }

  createTheme(dto: CreateTaskTheme): Observable<TaskTheme> {
    return this.http.post<TaskTheme>(this.apiUrl, dto);
  }

  updateTheme(id: string, dto: UpdateTaskTheme): Observable<TaskTheme> {
    return this.http.put<TaskTheme>(`${this.apiUrl}/${id}`, dto);
  }

  deleteTheme(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
