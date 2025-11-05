import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subject, takeUntil, forkJoin } from 'rxjs';
import { NotesService } from '../../services/notes.service';
import { NoteGroupsService } from '../../services/note-groups.service';
import { Note } from '../../models/note.models';
import { NoteGroup } from '../../models/note-group.models';

@Component({
  selector: 'app-notes-board',
  imports: [CommonModule, FormsModule],
  templateUrl: './notes-board.component.html',
  styleUrl: './notes-board.component.scss'
})
export class NotesBoardComponent implements OnInit, OnDestroy {
  private notesService = inject(NotesService);
  private groupsService = inject(NoteGroupsService);
  router = inject(Router);
  private destroy$ = new Subject<void>();

  groups: NoteGroup[] = [];
  notesMap: Map<string, Note[]> = new Map();
  ungroupedNotes: Note[] = [];

  isLoading = false;
  errorMessage = '';

  // Drag state
  draggedNote: Note | null = null;
  draggedGroup: NoteGroup | null = null;

  // Modal state
  showGroupModal = false;
  editingGroup: NoteGroup | null = null;
  groupForm = { title: '', color: '#8b5cf6' };

  predefinedColors = [
    '#8b5cf6', '#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#ec4899', '#6366f1'
  ];

  ngOnInit(): void {
    this.loadBoard();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadBoard(): void {
    this.isLoading = true;
    this.errorMessage = '';

    forkJoin({
      groups: this.groupsService.getGroups(),
      notes: this.notesService.getNotes({ isArchived: false, page: 1, pageSize: 500 })
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ groups, notes }) => {
          this.groups = groups.sort((a, b) => a.order - b.order);
          this.organizeNotes(notes.items);
          this.isLoading = false;
        },
        error: (error) => {
          console.error('Failed to load board:', error);
          this.errorMessage = error.message || 'Failed to load board';
          this.isLoading = false;
        }
      });
  }

  organizeNotes(notes: Note[]): void {
    this.notesMap.clear();
    this.ungroupedNotes = [];

    for (const note of notes) {
      if (note.groupId) {
        const list = this.notesMap.get(note.groupId) || [];
        list.push(note);
        this.notesMap.set(note.groupId, list);
      } else {
        this.ungroupedNotes.push(note);
      }
    }
  }

  getNotesForGroup(groupId: string): Note[] {
    return this.notesMap.get(groupId) || [];
  }

  // Group CRUD
  openGroupModal(group?: NoteGroup): void {
    this.editingGroup = group || null;
    this.groupForm = group
      ? { title: group.title, color: group.color || '#8b5cf6' }
      : { title: '', color: '#8b5cf6' };
    this.showGroupModal = true;
  }

  closeGroupModal(): void {
    this.showGroupModal = false;
    this.editingGroup = null;
    this.groupForm = { title: '', color: '#8b5cf6' };
  }

  saveGroup(): void {
    if (!this.groupForm.title.trim()) return;

    const dto = { title: this.groupForm.title.trim(), color: this.groupForm.color };

    const action$ = this.editingGroup
      ? this.groupsService.updateGroup(this.editingGroup.id, dto)
      : this.groupsService.createGroup(dto);

    action$.pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.closeGroupModal();
        this.loadBoard();
      },
      error: (error) => {
        console.error('Failed to save group:', error);
        alert(error.message || 'Failed to save group');
      }
    });
  }

  deleteGroup(groupId: string): void {
    if (!confirm('Delete this group? Notes will be ungrouped.')) return;

    this.groupsService.deleteGroup(groupId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => this.loadBoard(),
        error: (error) => {
          console.error('Failed to delete group:', error);
          alert(error.message || 'Failed to delete group');
        }
      });
  }

  // Drag & Drop for columns
  onGroupDragStart(group: NoteGroup): void {
    this.draggedGroup = group;
  }

  onGroupDragOver(event: DragEvent, overGroup: NoteGroup): void {
    event.preventDefault();
    if (!this.draggedGroup || this.draggedGroup.id === overGroup.id) return;
  }

  onGroupDrop(event: DragEvent, targetGroup: NoteGroup): void {
    event.preventDefault();
    if (!this.draggedGroup || this.draggedGroup.id === targetGroup.id) return;

    const fromIdx = this.groups.findIndex(g => g.id === this.draggedGroup!.id);
    const toIdx = this.groups.findIndex(g => g.id === targetGroup.id);
    if (fromIdx === -1 || toIdx === -1) return;

    const moved = this.groups.splice(fromIdx, 1)[0];
    this.groups.splice(toIdx, 0, moved);

    this.reorderGroups();
    this.draggedGroup = null;
  }

  reorderGroups(): void {
    const updates = this.groups.map((g, idx) =>
      this.groupsService.updateGroup(g.id, { order: idx })
    );
    forkJoin(updates).pipe(takeUntil(this.destroy$)).subscribe({
      error: (err) => console.error('Failed to reorder groups:', err)
    });
  }

  // Drag & Drop for notes
  onNoteDragStart(note: Note): void {
    this.draggedNote = note;
  }

  onNoteDragOver(event: DragEvent): void {
    event.preventDefault();
  }

  onNoteDrop(event: DragEvent, targetGroupId: string | null): void {
    event.preventDefault();
    if (!this.draggedNote) return;

    const oldGroupId = this.draggedNote.groupId || null;
    if (oldGroupId === targetGroupId) {
      this.draggedNote = null;
      return;
    }

    this.notesService.updateNote(this.draggedNote.id, { groupId: targetGroupId || undefined })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.draggedNote = null;
          this.loadBoard();
        },
        error: (error) => {
          console.error('Failed to move note:', error);
          alert(error.message || 'Failed to move note');
          this.draggedNote = null;
        }
      });
  }

  openNote(noteId: string): void {
    this.router.navigate(['/notes', noteId]);
  }

  getPreview(markdown: string, maxLength: number = 80): string {
    let text = markdown
      .replace(/#{1,6}\s/g, '')
      .replace(/\*\*|__/g, '')
      .replace(/\*|_/g, '')
      .replace(/\[([^\]]+)\]\([^\)]+\)/g, '$1')
      .replace(/`{1,3}[^`]+`{1,3}/g, '')
      .trim();

    if (text.length > maxLength) {
      return text.substring(0, maxLength) + '...';
    }
    return text;
  }
}

