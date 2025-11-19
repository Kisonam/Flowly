import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, inject, OnInit, signal, HostListener } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Tag } from '../../../features/notes/models/note.models';
import { TagsService, CreateTagRequest, UpdateTagRequest } from '../../services/tags.service';
import { Subject, takeUntil } from 'rxjs';
import { FocusTrapDirective } from '../../directives/focus-trap.directive';

/**
 * Tag Manager Component
 *
 * Allows creating, editing, deleting and selecting tags inline.
 * Designed to be embedded in editors (notes, tasks, transactions).
 */
@Component({
  selector: 'app-tag-manager',
  standalone: true,
  imports: [CommonModule, FormsModule, FocusTrapDirective],
  templateUrl: './tag-manager.component.html',
  styleUrls: ['./tag-manager.component.scss']
})
export class TagManagerComponent implements OnInit {
  private tagsService = inject(TagsService);
  private destroy$ = new Subject<void>();

  @Input() title = 'Tags';
  @Input() selectedIds: string[] = [];
  @Output() selectedIdsChange = new EventEmitter<string[]>();
  @Output() tagsUpdated = new EventEmitter<void>();

  // State
  tags = signal<Tag[]>([]);
  isLoading = signal(false);
  error = signal<string | null>(null);

  // Create/Edit form state
  showCreateForm = signal(false);
  showEditForm = signal(false);
  editingTag = signal<Tag | null>(null);

  // Form data
  createFormData = {
    name: '',
    color: '#8b5cf6'
  };

  editFormData = {
    name: '',
    color: '#8b5cf6'
  };

  // Predefined colors
  readonly predefinedColors = [
    '#8b5cf6', // Purple
    '#3b82f6', // Blue
    '#10b981', // Green
    '#f59e0b', // Amber
    '#ef4444', // Red
    '#ec4899', // Pink
    '#06b6d4', // Cyan
    '#6366f1', // Indigo
    '#84cc16', // Lime
    '#14b8a6', // Teal
    '#f97316', // Orange
    '#a855f7', // Purple Variant
  ];

  ngOnInit(): void {
    this.loadTags();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadTags(): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.tagsService.getTags()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (tags) => {
          this.tags.set(tags);
          this.isLoading.set(false);
        },
        error: (err) => {
          console.error('❌ Failed to load tags', err);
          this.error.set(err.message || 'Failed to load tags');
          this.isLoading.set(false);
        }
      });
  }

  // Selection
  toggleTag(tagId: string): void {
    const set = new Set(this.selectedIds);
    if (set.has(tagId)) {
      set.delete(tagId);
    } else {
      set.add(tagId);
    }
    this.selectedIds = Array.from(set);
    this.selectedIdsChange.emit(this.selectedIds);
  }

  isSelected(tagId: string): boolean {
    return this.selectedIds?.includes(tagId) ?? false;
  }

  // Create
  openCreateForm(): void {
    this.showCreateForm.set(true);
    this.createFormData = {
      name: '',
      color: '#8b5cf6'
    };
  }

  cancelCreate(): void {
    this.showCreateForm.set(false);
    this.createFormData = {
      name: '',
      color: '#8b5cf6'
    };
  }

  createTag(): void {
    const name = this.createFormData.name.trim();
    if (!name) {
      alert('Tag name is required');
      return;
    }

    const request: CreateTagRequest = {
      name,
      color: this.createFormData.color
    };

    this.tagsService.createTag(request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (tag) => {
          console.log('✅ Tag created:', tag);
          this.loadTags();
          this.cancelCreate();
          // Auto-select newly created tag
          this.toggleTag(tag.id);
          this.tagsUpdated.emit();
        },
        error: (err) => {
          console.error('❌ Failed to create tag', err);
          alert('Failed to create tag: ' + (err.message || 'Unknown error'));
        }
      });
  }

  // Edit
  openEditForm(tag: Tag): void {
    this.editingTag.set(tag);
    this.editFormData = {
      name: tag.name,
      color: tag.color || '#8b5cf6'
    };
    this.showEditForm.set(true);
  }

  cancelEdit(): void {
    this.showEditForm.set(false);
    this.editingTag.set(null);
    this.editFormData = {
      name: '',
      color: '#8b5cf6'
    };
  }

  updateTag(): void {
    const tag = this.editingTag();
    if (!tag) return;

    const name = this.editFormData.name.trim();
    if (!name) {
      alert('Tag name is required');
      return;
    }

    const request: UpdateTagRequest = {
      name,
      color: this.editFormData.color
    };

    this.tagsService.updateTag(tag.id, request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (updatedTag) => {
          console.log('✅ Tag updated:', updatedTag);
          this.loadTags();
          this.cancelEdit();
          this.tagsUpdated.emit();
        },
        error: (err) => {
          console.error('❌ Failed to update tag', err);
          alert('Failed to update tag: ' + (err.message || 'Unknown error'));
        }
      });
  }

  // Delete
  deleteTag(tag: Tag): void {
    if (!confirm(`Delete tag "${tag.name}"? This will remove it from all items.`)) {
      return;
    }

    this.tagsService.deleteTag(tag.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          console.log('✅ Tag deleted:', tag.name);
          this.loadTags();
          this.cancelEdit();
          // Remove from selection if selected
          if (this.isSelected(tag.id)) {
            this.toggleTag(tag.id);
          }
          this.tagsUpdated.emit();
        },
        error: (err) => {
          console.error('❌ Failed to delete tag', err);
          alert('Failed to delete tag: ' + (err.message || 'Unknown error'));
        }
      });
  }

  selectColor(color: string, isCreate: boolean): void {
    if (isCreate) {
      this.createFormData.color = color;
    } else {
      this.editFormData.color = color;
    }
  }

  /**
   * Handle Escape key to close modals
   */
  @HostListener('document:keydown.escape')
  handleEscapeKey(): void {
    if (this.showEditForm()) {
      this.cancelEdit();
    } else if (this.showCreateForm()) {
      this.cancelCreate();
    }
  }
}
