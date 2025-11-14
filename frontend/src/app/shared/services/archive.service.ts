import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ArchiveListResponse, ArchiveQuery, ArchivedEntity, ArchivedEntityDetail } from '../models/archive.models';

@Injectable({
  providedIn: 'root'
})
export class ArchiveService {
  private readonly API_URL = `${environment.apiUrl}/archive`;

  constructor(private http: HttpClient) {}

  /**
   * Get paginated list of archived entities
   */
  getArchived(query?: ArchiveQuery): Observable<ArchiveListResponse> {
    let params = new HttpParams();

    if (query) {
      if (query.entityType !== undefined) {
        params = params.set('entityType', query.entityType.toString());
      }
      if (query.search) {
        params = params.set('search', query.search);
      }
      if (query.page) {
        params = params.set('page', query.page.toString());
      }
      if (query.pageSize) {
        params = params.set('pageSize', query.pageSize.toString());
      }
      if (query.sortBy) {
        params = params.set('sortBy', query.sortBy);
      }
      if (query.sortDirection) {
        params = params.set('sortDirection', query.sortDirection);
      }
    }

    return this.http.get<ArchiveListResponse>(this.API_URL, { params }).pipe(
      tap(() => console.log('📦 Fetched archived items')),
      catchError(this.handleError)
    );
  }

  /**
   * Get detailed information about a specific archived entity (includes full JSON payload)
   */
  getDetail(archiveEntryId: string): Observable<ArchivedEntityDetail> {
    return this.http.get<ArchivedEntityDetail>(`${this.API_URL}/${archiveEntryId}/detail`).pipe(
      tap(() => console.log('🔍 Fetched archive detail:', archiveEntryId)),
      catchError(this.handleError)
    );
  }

  /**
   * Restore an archived entity
   */
  restore(archiveEntryId: string): Observable<void> {
    return this.http.post<void>(`${this.API_URL}/${archiveEntryId}/restore`, {}).pipe(
      tap(() => console.log('♻️ Restored archive entry:', archiveEntryId)),
      catchError(this.handleError)
    );
  }

  /**
   * Permanently delete an archived entity
   */
  permanentDelete(archiveEntryId: string): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/${archiveEntryId}/permanent`).pipe(
      tap(() => console.log('🗑️ Permanently deleted archive entry:', archiveEntryId)),
      catchError(this.handleError)
    );
  }

  /**
   * Migrate existing archived entities to the new archive system
   */
  migrateExistingArchives(): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.API_URL}/migrate`, {}).pipe(
      tap(() => console.log('🔄 Migration completed')),
      catchError(this.handleError)
    );
  }

  /**
   * Handle HTTP errors
   */
  private handleError(error: any): Observable<never> {
    console.error('❌ Archive service error:', error);
    throw error;
  }
}
