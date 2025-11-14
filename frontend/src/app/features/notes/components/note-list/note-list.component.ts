import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Subject, takeUntil, debounceTime, distinctUntilChanged } from 'rxjs';
import { NotesService } from '../../services/notes.service';
import { TagsService } from '../../../../shared/services/tags.service';
import { Note, NoteFilter, PaginatedResult } from '../../models/note.models';

@Component({
  selector: 'app-note-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './note-list.component.html',
  styleUrls: ['./note-list.component.scss']
})
export class NoteListComponent implements OnInit, OnDestroy {
  private notesService = inject(NotesService);
  private tagsService = inject(TagsService);
  router = inject(Router);
  private destroy$ = new Subject<void>();
  private searchSubject$ = new Subject<string>();

  // Data
  notes: Note[] = [];
  paginatedResult?: PaginatedResult<Note>;

  // Filter state
  filter: NoteFilter = {
    search: '',
    tagIds: [],
    isArchived: false,
    page: 1,
    pageSize: 12
  };

  // UI state
  isLoading = false;
  errorMessage = '';
  viewMode: 'grid' | 'list' = 'grid';
  showFilters = false;
  private dragSourceId: string | null = null;

  // Tags for filter (loaded from backend)
  availableTags: { id: string; name: string; color?: string }[] = [];

  ngOnInit(): void {
    this.setupSearchDebounce();
    this.loadAvailableTags();
    this.loadNotes();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Setup debounced search
   */
  private setupSearchDebounce(): void {
    this.searchSubject$
      .pipe(
        debounceTime(500),
        distinctUntilChanged(),
        takeUntil(this.destroy$)
      )
      .subscribe(searchTerm => {
        this.filter.search = searchTerm;
        this.filter.page = 1; // Reset to first page
        this.loadNotes();
      });
  }

  /**
   * Load available tags for filtering
   */
  private loadAvailableTags(): void {
    this.tagsService.getTags()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (tags) => this.availableTags = tags || [],
        error: (err) => {
          console.error('Failed to load tags:', err);
          this.availableTags = [];
        }
      });
  }

  /**
   * Load notes with current filter
   */
  loadNotes(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.notesService.getNotes(this.filter)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.paginatedResult = result;
          this.notes = this.applySavedOrder(result.items);
          this.isLoading = false;
        },
        error: (error) => {
          console.error('Failed to load notes:', error);
          this.errorMessage = error.message || 'Failed to load notes';
          this.isLoading = false;
        }
      });
  }

  /**
   * Drag & Drop ordering (client-side, per page)
   */
  onDragStart(noteId: string): void {
    this.dragSourceId = noteId;
  }

  onDragOver(event: DragEvent, overNoteId: string): void {
    event.preventDefault();
    event.dataTransfer?.dropEffect && (event.dataTransfer.dropEffect = 'move');
  }

  onDrop(event: DragEvent, dropNoteId: string): void {
    event.preventDefault();
    if (!this.dragSourceId || this.dragSourceId === dropNoteId) return;

    const fromIndex = this.notes.findIndex(n => n.id === this.dragSourceId);
    const toIndex = this.notes.findIndex(n => n.id === dropNoteId);
    if (fromIndex === -1 || toIndex === -1) return;

    const moved = this.notes.splice(fromIndex, 1)[0];
    this.notes.splice(toIndex, 0, moved);

    this.saveOrder();
    this.dragSourceId = null;
  }

  private getOrderStorageKey(): string {
    const base = this.filter.isArchived ? 'archived' : 'active';
    // Keyed by page to keep it simple
    return `notes_order_${base}_p${this.filter.page}_s${this.filter.pageSize}`;
  }

  private saveOrder(): void {
    const key = this.getOrderStorageKey();
    const order = this.notes.map(n => n.id);
    localStorage.setItem(key, JSON.stringify(order));
  }

  private applySavedOrder(items: Note[]): Note[] {
    const key = this.getOrderStorageKey();
    const json = localStorage.getItem(key);
    if (!json) return items;
    try {
      const order: string[] = JSON.parse(json);
      const map = new Map(items.map(i => [i.id, i] as const));
      const ordered: Note[] = [];
      for (const id of order) {
        const item = map.get(id);
        if (item) {
          ordered.push(item);
          map.delete(id);
        }
      }
      // Append any new items not in saved order
      for (const rest of map.values()) {
        ordered.push(rest);
      }
      return ordered;
    } catch {
      return items;
    }
  }

  /**
   * Handle search input
   */
  onSearchChange(searchTerm: string): void {
    this.searchSubject$.next(searchTerm);
  }

  /**
   * Toggle tag filter
   */
  toggleTagFilter(tagId: string): void {
    const index = this.filter.tagIds?.indexOf(tagId) ?? -1;

    if (index > -1) {
      this.filter.tagIds?.splice(index, 1);
    } else {
      if (!this.filter.tagIds) {
        this.filter.tagIds = [];
      }
      this.filter.tagIds.push(tagId);
    }

    this.filter.page = 1; // Reset to first page
    this.loadNotes();
  }

  /**
   * Check if tag is selected
   */
  isTagSelected(tagId: string): boolean {
    return this.filter.tagIds?.includes(tagId) ?? false;
  }

  /**
   * Clear all filters
   */
  clearFilters(): void {
    this.filter = {
      search: '',
      tagIds: [],
      isArchived: false,
      page: 1,
      pageSize: 12
    };
    this.loadNotes();
  }

  /**
   * Change page
   */
  goToPage(page: number): void {
    if (page < 1 || (this.paginatedResult && page > this.paginatedResult.totalPages)) {
      return;
    }

    this.filter.page = page;
    this.loadNotes();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  /**
   * Change page size
   */
  changePageSize(size: number): void {
    this.filter.pageSize = size;
    this.filter.page = 1; // Reset to first page
    this.loadNotes();
  }

  /**
   * Toggle view mode
   */
  toggleViewMode(): void {
    this.viewMode = this.viewMode === 'grid' ? 'list' : 'grid';
  }

  /**
   * Toggle filters panel
   */
  toggleFilters(): void {
    this.showFilters = !this.showFilters;
  }

  /**
   * Navigate to create note
   */
  createNote(): void {
    this.router.navigate(['/notes/new']);
  }

  /**
   * Navigate to note detail
   */
  viewNote(noteId: string): void {
    this.router.navigate(['/notes', noteId]);
  }

  /**
   * Quick edit note (inline)
   */
  editNote(noteId: string, event: Event): void {
    event.stopPropagation();
    this.router.navigate(['/notes', noteId, 'edit']);
  }

  /**
   * Archive note
   */
  archiveNote(noteId: string, event: Event): void {
    event.stopPropagation();

    if (!confirm('Are you sure you want to archive this note?')) {
      return;
    }

    this.notesService.deleteNote(noteId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.loadNotes(); // Reload list
        },
        error: (error) => {
          console.error('Failed to archive note:', error);
          alert(error.message || 'Failed to archive note');
        }
      });
  }

  /**
   * Restore note from archive
   */
  restoreNote(noteId: string, event: Event): void {
    event.stopPropagation();

    this.notesService.restoreNote(noteId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.loadNotes(); // Reload list
        },
        error: (error) => {
          console.error('Failed to restore note:', error);
          alert(error.message || 'Failed to restore note');
        }
      });
  }

  /**
   * Export note as markdown
   */
  exportNote(noteId: string, event: Event): void {
    event.stopPropagation();

    this.notesService.exportMarkdown(noteId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        error: (error) => {
          console.error('Failed to export note:', error);
          alert(error.message || 'Failed to export note');
        }
      });
  }

  /**
   * Get tag by ID
   */
  getTag(tagId: string) {
    return this.availableTags.find(t => t.id === tagId);
  }

  /**
   * Get end item number for pagination info
   */
  getEndItem(): number {
    if (!this.paginatedResult) return 0;
    return Math.min(
      this.paginatedResult.page * this.paginatedResult.pageSize,
      this.paginatedResult.totalCount
    );
  }

  /**
   * Format date for display
   */
  formatDate(date: Date): string {
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    const minutes = Math.floor(diff / 60000);
    const hours = Math.floor(diff / 3600000);
    const days = Math.floor(diff / 86400000);

    if (minutes < 1) return 'just now';
    if (minutes < 60) return `${minutes}m ago`;
    if (hours < 24) return `${hours}h ago`;
    if (days < 7) return `${days}d ago`;

    return date.toLocaleDateString();
  }

  /**
   * Get preview text from markdown
   */
  getPreview(markdown: string, maxLength: number = 150): string {
    // Remove markdown syntax for preview
    let text = markdown
      .replace(/#{1,6}\s/g, '') // Headers
      .replace(/\*\*|__/g, '') // Bold
      .replace(/\*|_/g, '') // Italic
      .replace(/\[([^\]]+)\]\([^\)]+\)/g, '$1') // Links
      .replace(/`{1,3}[^`]+`{1,3}/g, '') // Code
      .trim();

    if (text.length > maxLength) {
      return text.substring(0, maxLength) + '...';
    }

    return text;
  }

  /**
   * Generate array for pagination
   */
  getPaginationArray(): number[] {
    if (!this.paginatedResult) return [];

    const totalPages = this.paginatedResult.totalPages;
    const currentPage = this.paginatedResult.page;
    const delta = 2; // Pages to show on each side of current page

    const range: number[] = [];
    const rangeWithDots: (number | string)[] = [];

    for (
      let i = Math.max(2, currentPage - delta);
      i <= Math.min(totalPages - 1, currentPage + delta);
      i++
    ) {
      range.push(i);
    }

    if (currentPage - delta > 2) {
      rangeWithDots.push(1, '...');
    } else {
      rangeWithDots.push(1);
    }

    rangeWithDots.push(...range);

    if (currentPage + delta < totalPages - 1) {
      rangeWithDots.push('...', totalPages);
    } else if (totalPages > 1) {
      rangeWithDots.push(totalPages);
    }

    return rangeWithDots.filter(p => typeof p === 'number') as number[];
  }
}
