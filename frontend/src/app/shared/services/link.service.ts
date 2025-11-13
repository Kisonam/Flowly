import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  Link,
  CreateLinkRequest,
  EntityPreview,
  LinkEntityType
} from '../models/link.models';

/**
 * Service for managing links between entities (Notes, Tasks, Transactions)
 *
 * @example
 * // Inject the service
 * private linkService = inject(LinkService);
 *
 * // Create a link from a note to a task
 * this.linkService.linkFromNote(noteId, LinkEntityType.Task, taskId)
 *   .subscribe(link => console.log('Link created:', link));
 *
 * // Get all links for a note
 * this.linkService.getLinksForNote(noteId)
 *   .subscribe(links => console.log('Links:', links));
 *
 * // Delete a link
 * this.linkService.deleteLink(linkId)
 *   .subscribe(() => console.log('Link deleted'));
 */
@Injectable({
  providedIn: 'root'
})
export class LinkService {
  private http = inject(HttpClient);
  private readonly API_URL = `${environment.apiUrl}/links`;

  /**
   * Create a new link between two entities
   * @param request Link creation data
   * @returns Created link with previews
   */
  createLink(request: CreateLinkRequest): Observable<Link> {
    console.log('📤 Creating link:', request);
    return this.http.post<Link>(this.API_URL, request)
      .pipe(
        map(link => this.convertLinkDates(link)),
        tap(link => console.log('✅ Link created:', link)),
        catchError(this.handleError('createLink'))
      );
  }

  /**
   * Delete a link by ID
   * @param linkId Link ID to delete
   */
  deleteLink(linkId: string): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/${linkId}`)
      .pipe(
        tap(() => console.log('✅ Link deleted:', linkId)),
        catchError(this.handleError('deleteLink'))
      );
  }

  /**
   * Get all links for a specific entity
   * @param entityType Entity type (Note, Task, Transaction)
   * @param entityId Entity ID
   * @returns List of links with previews
   */
  getLinksForEntity(entityType: LinkEntityType, entityId: string): Observable<Link[]> {
    // Convert numeric enum to string name for backend
    const typeName = LinkEntityType[entityType]; // 1 -> "Note", 2 -> "Task", 3 -> "Transaction"

    const params = new HttpParams()
      .set('type', typeName)
      .set('id', entityId);

    console.log('📤 Fetching links for:', { entityType, typeName, entityId, url: this.API_URL });

    return this.http.get<Link[]>(this.API_URL, { params })
      .pipe(
        tap(response => console.log('📥 Raw response:', response)),
        map(links => links.map(link => this.convertLinkDates(link))),
        tap(links => console.log('✅ Links fetched for entity:', { entityType, entityId, count: links.length, links })),
        catchError(this.handleError('getLinksForEntity'))
      );
  }

  /**
   * Get a preview of an entity
   * @param entityType Entity type (Note, Task, Transaction)
   * @param entityId Entity ID
   * @returns Entity preview
   */
  getPreview(entityType: LinkEntityType, entityId: string): Observable<EntityPreview> {
    // Convert numeric enum to string name for backend
    const typeName = LinkEntityType[entityType];

    const params = new HttpParams()
      .set('type', typeName)
      .set('id', entityId);

    return this.http.get<EntityPreview>(`${this.API_URL}/preview`, { params })
      .pipe(
        tap(preview => console.log('✅ Preview fetched:', preview)),
        catchError(this.handleError('getPreview'))
      );
  }

  /**
   * Get all links for a note
   * Helper method for convenience
   */
  getLinksForNote(noteId: string): Observable<Link[]> {
    return this.getLinksForEntity(LinkEntityType.Note, noteId);
  }

  /**
   * Get all links for a task
   * Helper method for convenience
   */
  getLinksForTask(taskId: string): Observable<Link[]> {
    return this.getLinksForEntity(LinkEntityType.Task, taskId);
  }

  /**
   * Get all links for a transaction
   * Helper method for convenience
   */
  getLinksForTransaction(transactionId: string): Observable<Link[]> {
    return this.getLinksForEntity(LinkEntityType.Transaction, transactionId);
  }

  /**
   * Create a link from a note to another entity
   * Helper method for convenience
   */
  linkFromNote(noteId: string, toType: LinkEntityType, toId: string): Observable<Link> {
    return this.createLink({
      fromType: LinkEntityType.Note,
      fromId: noteId,
      toType,
      toId
    });
  }

  /**
   * Create a link from a task to another entity
   * Helper method for convenience
   */
  linkFromTask(taskId: string, toType: LinkEntityType, toId: string): Observable<Link> {
    return this.createLink({
      fromType: LinkEntityType.Task,
      fromId: taskId,
      toType,
      toId
    });
  }

  /**
   * Create a link from a transaction to another entity
   * Helper method for convenience
   */
  linkFromTransaction(transactionId: string, toType: LinkEntityType, toId: string): Observable<Link> {
    return this.createLink({
      fromType: LinkEntityType.Transaction,
      fromId: transactionId,
      toType,
      toId
    });
  }

  /**
   * Convert link date strings to Date objects and string enums to numbers
   */
  private convertLinkDates(link: any): Link {
    // Convert string enum types to numeric enum values
    const fromType = this.convertTypeToEnum(link.fromType);
    const toType = this.convertTypeToEnum(link.toType);

    // Convert preview types if they exist
    const fromPreview = link.fromPreview ? {
      ...link.fromPreview,
      type: this.convertTypeToEnum(link.fromPreview.type)
    } : undefined;

    const toPreview = link.toPreview ? {
      ...link.toPreview,
      type: this.convertTypeToEnum(link.toPreview.type)
    } : undefined;

    return {
      ...link,
      fromType,
      toType,
      fromPreview,
      toPreview,
      createdAt: typeof link.createdAt === 'string' ? new Date(link.createdAt) : link.createdAt
    };
  }

  /**
   * Convert string type to numeric enum
   */
  private convertTypeToEnum(type: string | number): LinkEntityType {
    if (typeof type === 'number') return type as LinkEntityType;

    switch (type) {
      case 'Note': return LinkEntityType.Note;
      case 'Task': return LinkEntityType.Task;
      case 'Transaction': return LinkEntityType.Transaction;
      default: return LinkEntityType.Note;
    }
  }

  /**
   * Error handler
   */
  private handleError(operation: string) {
    return (error: any): Observable<never> => {
      console.error(`❌ ${operation} failed:`, error);
      console.error('Error details:', {
        status: error?.status,
        statusText: error?.statusText,
        message: error?.error?.message,
        errors: error?.error?.errors,
        errorBody: error?.error
      });

      const message = error?.error?.message
        || error?.error?.title
        || error?.message
        || `Failed to ${operation}`;

      return throwError(() => new Error(message));
    };
  }
}
