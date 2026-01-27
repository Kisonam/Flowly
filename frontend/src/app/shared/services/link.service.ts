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

@Injectable({
  providedIn: 'root'
})
export class LinkService {
  private http = inject(HttpClient);
  private readonly API_URL = `${environment.apiUrl}/links`;

  createLink(request: CreateLinkRequest): Observable<Link> {
    console.log('📤 Creating link:', request);
    return this.http.post<Link>(this.API_URL, request)
      .pipe(
        map(link => this.convertLinkDates(link)),
        tap(link => console.log('✅ Link created:', link)),
        catchError(this.handleError('createLink'))
      );
  }

  deleteLink(linkId: string): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/${linkId}`)
      .pipe(
        tap(() => console.log('✅ Link deleted:', linkId)),
        catchError(this.handleError('deleteLink'))
      );
  }

  getLinksForEntity(entityType: LinkEntityType, entityId: string): Observable<Link[]> {
    
    const typeName = LinkEntityType[entityType]; 

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

  getPreview(entityType: LinkEntityType, entityId: string): Observable<EntityPreview> {
    
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

  getLinksForNote(noteId: string): Observable<Link[]> {
    return this.getLinksForEntity(LinkEntityType.Note, noteId);
  }

  getLinksForTask(taskId: string): Observable<Link[]> {
    return this.getLinksForEntity(LinkEntityType.Task, taskId);
  }

  getLinksForTransaction(transactionId: string): Observable<Link[]> {
    return this.getLinksForEntity(LinkEntityType.Transaction, transactionId);
  }

  linkFromNote(noteId: string, toType: LinkEntityType, toId: string): Observable<Link> {
    return this.createLink({
      fromType: LinkEntityType.Note,
      fromId: noteId,
      toType,
      toId
    });
  }

  linkFromTask(taskId: string, toType: LinkEntityType, toId: string): Observable<Link> {
    return this.createLink({
      fromType: LinkEntityType.Task,
      fromId: taskId,
      toType,
      toId
    });
  }

  linkFromTransaction(transactionId: string, toType: LinkEntityType, toId: string): Observable<Link> {
    return this.createLink({
      fromType: LinkEntityType.Transaction,
      fromId: transactionId,
      toType,
      toId
    });
  }

  private convertLinkDates(link: any): Link {
    
    const fromType = this.convertTypeToEnum(link.fromType);
    const toType = this.convertTypeToEnum(link.toType);

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

  private convertTypeToEnum(type: string | number): LinkEntityType {
    if (typeof type === 'number') return type as LinkEntityType;

    switch (type) {
      case 'Note': return LinkEntityType.Note;
      case 'Task': return LinkEntityType.Task;
      case 'Transaction': return LinkEntityType.Transaction;
      default: return LinkEntityType.Note;
    }
  }

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
