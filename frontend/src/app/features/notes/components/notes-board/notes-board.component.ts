import { Component, inject, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { DragDropModule, CdkDragDrop, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { Subject, takeUntil, forkJoin } from 'rxjs';
import { NotesService } from '../../services/notes.service';
import { NoteGroupsService } from '../../services/note-groups.service';
import { Note } from '../../models/note.models';
import { NoteGroup } from '../../models/note-group.models';
import { FocusTrapDirective } from '../../../../shared/directives/focus-trap.directive';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ThemeService } from '../../../../core/services/theme.service';

@Component({
  selector: 'app-notes-board',
  imports: [CommonModule, FormsModule, DragDropModule, FocusTrapDirective, TranslateModule],
  templateUrl: './notes-board.component.html',
  styleUrl: './notes-board.component.scss'
})
export class NotesBoardComponent implements OnInit, OnDestroy {
  private notesService = inject(NotesService);
  private groupsService = inject(NoteGroupsService);
  private translate = inject(TranslateService);
  private themeService = inject(ThemeService);
  router = inject(Router);
  private destroy$ = new Subject<void>();

  groups: NoteGroup[] = [];
  notesMap: Map<string, Note[]> = new Map();
  ungroupedNotes: Note[] = [];

  isLoading = false;
  errorMessage = '';

  showGroupModal = false;
  editingGroup: NoteGroup | null = null;
  groupForm = { title: '', color: '#8b5cf6' };

  predefinedColors = [
    '#8b5cf6', '#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#ec4899', '#6366f1'
  ];

  ngOnInit(): void {
    this.loadBoard();

    this.themeService.currentTheme$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        
        this.groups = [...this.groups];
      });
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
          this.errorMessage = error.message || this.translate.instant('NOTES.BOARD.ERRORS.LOAD_FAILED');
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
        alert(error.message || this.translate.instant('NOTES.BOARD.ERRORS.SAVE_FAILED'));
      }
    });
  }

  deleteGroup(groupId: string): void {
    if (!confirm(this.translate.instant('NOTES.BOARD.CONFIRM.DELETE_GROUP'))) return;

    this.groupsService.deleteGroup(groupId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => this.loadBoard(),
        error: (error) => {
          console.error('Failed to delete group:', error);
          alert(error.message || this.translate.instant('NOTES.BOARD.ERRORS.DELETE_FAILED'));
        }
      });
  }

  onGroupsDrop(event: CdkDragDrop<NoteGroup[]>): void {
    if (event.previousIndex === event.currentIndex) return;

    moveItemInArray(this.groups, event.previousIndex, event.currentIndex);

    const updates = this.groups.map((g, idx) =>
      this.groupsService.updateGroup(g.id, { order: idx })
    );

    forkJoin(updates).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => console.log('✅ Groups reordered'),
      error: (err) => {
        console.error('Failed to reorder groups:', err);
        
        moveItemInArray(this.groups, event.currentIndex, event.previousIndex);
        alert(err.message || this.translate.instant('NOTES.BOARD.ERRORS.REORDER_FAILED'));
      }
    });
  }

  onNoteDrop(event: CdkDragDrop<Note[]>, targetGroupId: string | null): void {
    const note: Note | undefined = event.item.data as Note;
    if (!note) return;

    const oldGroupId = note.groupId || null;

    if (oldGroupId === targetGroupId) {
      if (event.previousIndex === event.currentIndex) return;

      const notes = targetGroupId ? this.notesMap.get(targetGroupId) || [] : this.ungroupedNotes;
      moveItemInArray(notes, event.previousIndex, event.currentIndex);

      if (targetGroupId) {
        this.notesMap.set(targetGroupId, notes);
      } else {
        this.ungroupedNotes = [...notes]; 
      }
      return;
    }

    const sourceNotes = oldGroupId ? this.notesMap.get(oldGroupId) || [] : [...this.ungroupedNotes];
    const targetNotes = targetGroupId ? this.notesMap.get(targetGroupId) || [] : [...this.ungroupedNotes];

    transferArrayItem(sourceNotes, targetNotes, event.previousIndex, event.currentIndex);

    note.groupId = targetGroupId || undefined;

    if (oldGroupId) {
      this.notesMap.set(oldGroupId, sourceNotes);
    } else {
      this.ungroupedNotes = sourceNotes;
    }

    if (targetGroupId) {
      this.notesMap.set(targetGroupId, targetNotes);
    } else {
      this.ungroupedNotes = targetNotes;
    }

    const updatePayload = targetGroupId
      ? { groupId: targetGroupId }
      : { groupId: '00000000-0000-0000-0000-000000000000' };

    this.notesService.updateNote(note.id, updatePayload)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => console.log('✅ Note moved'),
        error: (error) => {
          console.error('Failed to move note:', error);

          note.groupId = oldGroupId || undefined;

          transferArrayItem(targetNotes, sourceNotes, event.currentIndex, event.previousIndex);

          if (oldGroupId) {
            this.notesMap.set(oldGroupId, sourceNotes);
          } else {
            this.ungroupedNotes = sourceNotes;
          }

          if (targetGroupId) {
            this.notesMap.set(targetGroupId, targetNotes);
          } else {
            this.ungroupedNotes = targetNotes;
          }

          alert(error.message || this.translate.instant('NOTES.BOARD.ERRORS.MOVE_FAILED'));
        }
      });
  }

  getDropListIds(): string[] {
    const groupIds = this.groups.map(g => `group-${g.id}`);
    return ['group-ungrouped', ...groupIds];
  }

  getListId(groupId: string | null): string {
    return groupId ? `group-${groupId}` : 'group-ungrouped';
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

  getTagColor(tag: { color?: string | null }): string {
    
    if (this.themeService.getCurrentTheme() === 'low-stimulus') {
      return '#6b7280';
    }
    return tag.color || '#6b7280';
  }

  getGroupColor(group: { color?: string | null }): string {
    
    if (this.themeService.getCurrentTheme() === 'low-stimulus') {
      return '#6b7280';
    }
    return group.color || '#8b5cf6';
  }

  getButtonColor(color: string): string {
    
    if (this.themeService.getCurrentTheme() === 'low-stimulus') {
      return '#6b7280';
    }
    return color;
  }

  @HostListener('document:keydown.escape')
  handleEscapeKey(): void {
    if (this.showGroupModal) {
      this.closeGroupModal();
    }
  }
}

