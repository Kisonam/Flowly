import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TransactionListItem } from '../models/transactions.models';

@Injectable({ providedIn: 'root' })
export class TransactionsService {
  private http = inject(HttpClient);
  private readonly API_URL = `${environment.apiUrl}/transactions`;

  list(options?: { search?: string; isArchived?: boolean; take?: number }): Observable<TransactionListItem[]> {
    let params = new HttpParams();
    if (options?.search) params = params.set('search', options.search);
    if (options?.isArchived !== undefined) params = params.set('isArchived', String(options.isArchived));
    if (options?.take) params = params.set('take', String(options.take));
    return this.http.get<TransactionListItem[]>(this.API_URL, { params }).pipe(
      catchError((error) => {
        const message = error?.error?.message || error?.message || 'Failed to load transactions';
        return throwError(() => new Error(message));
      })
    );
  }
}
