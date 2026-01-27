import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { NoteGroup, CreateNoteGroup, UpdateNoteGroup } from '../models/note-group.models';

@Injectable({
  providedIn: 'root'
})
export class NoteGroupsService {
  private http = inject(HttpClient);
    
    private apiUrl = `${environment.apiUrl}/note-groups`;

  getGroups(): Observable<NoteGroup[]> {
    return this.http.get<NoteGroup[]>(this.apiUrl);
  }

  createGroup(dto: CreateNoteGroup): Observable<NoteGroup> {
    return this.http.post<NoteGroup>(this.apiUrl, dto);
  }

  updateGroup(id: string, dto: UpdateNoteGroup): Observable<NoteGroup> {
    return this.http.put<NoteGroup>(`${this.apiUrl}/${id}`, dto);
  }

  deleteGroup(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
