import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { DashboardData } from '../models/dashboard.models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private http = inject(HttpClient);
  private readonly API_URL = `${environment.apiUrl}/dashboard`;

  getDashboard(): Observable<DashboardData> {
    return this.http.get<DashboardData>(this.API_URL).pipe(
      catchError((error) => {
        const message = error?.error?.message || error?.message || 'Failed to load dashboard data';
        return throwError(() => new Error(message));
      })
    );
  }
}
