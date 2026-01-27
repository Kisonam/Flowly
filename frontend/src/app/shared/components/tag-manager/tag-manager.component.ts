import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, inject, OnInit, signal, HostListener } from '@angular/core';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FormsModule } from '@angular/forms';
import { Tag } from '../../../features/notes/models/note.models';
import { TagsService, CreateTagRequest, UpdateTagRequest } from '../../services/tags.service';
import { Subject, takeUntil } from 'rxjs';
import { FocusTrapDirective } from '../../directives/focus-trap.directive';
import { ThemeService } from '../../../core/services/theme.service';

@Component({
  selector: 'app-tag-manager',
  standalone: true,
  imports: [CommonModule, FormsModule, FocusTrapDirective, TranslateModule],
  templateUrl: './tag-manager.component.html',
  styleUrls: ['./tag-manager.component.scss']
})
export class TagManagerComponent implements OnInit {
  private tagsService = inject(TagsService);
  private translate = inject(TranslateService);
  private themeService = inject(ThemeService);
  private destroy$ = new Subject<void>();

  @Input() title = 'Tags';
  @Input() selectedIds: string[] = [];
  @Output() selectedIdsChange = new EventEmitter<string[]>();
  @Output() tagsUpdated = new EventEmitter<void>();

  tags = signal<Tag[]>([]);
  isLoading = signal(false);
  error = signal<string | null>(null);

  showCreateForm = signal(false);
  showEditForm = signal(false);
  editingTag = signal<Tag | null>(null);

  createFormData = {
    name: '',
    color: '#8b5cf6'
  };

  editFormData = {
    name: '',
    color: '#8b5cf6'
  };

  readonly predefinedColors = [
    '#8b5cf6', 
    '#3b82f6', 
    '#10b981', 
    '#f59e0b', 
    '#ef4444', 
    '#ec4899', 
    '#06b6d4', 
    '#6366f1', 
    '#84cc16', 
    '#14b8a6', 
    '#f97316', 
    '#a855f7', 
  ];

  ngOnInit(): void {
    this.loadTags();

    this.themeService.currentTheme$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        
        this.tags.set([...this.tags()]);
      });
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
      alert(this.translate.instant('COMMON.ERRORS.REQUIRED'));
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
          
          this.toggleTag(tag.id);
          this.tagsUpdated.emit();
        },
        error: (err) => {
          console.error('❌ Failed to create tag', err);
          alert(this.translate.instant('COMMON.ERRORS.FAILED_TO_CREATE') + ': ' + (err.message || 'Unknown error'));
        }
      });
  }

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
      alert(this.translate.instant('COMMON.ERRORS.REQUIRED'));
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
          alert(this.translate.instant('COMMON.ERRORS.FAILED_TO_UPDATE') + ': ' + (err.message || 'Unknown error'));
        }
      });
  }

  deleteTag(tag: Tag): void {
    if (!confirm(this.translate.instant('COMMON.CONFIRM.DELETE_TAG', { name: tag.name }))) {
      return;
    }

    this.tagsService.deleteTag(tag.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          console.log('✅ Tag deleted:', tag.name);
          this.loadTags();
          this.cancelEdit();
          
          if (this.isSelected(tag.id)) {
            this.toggleTag(tag.id);
          }
          this.tagsUpdated.emit();
        },
        error: (err) => {
          console.error('❌ Failed to delete tag', err);
          alert(this.translate.instant('COMMON.ERRORS.FAILED_TO_DELETE') + ': ' + (err.message || 'Unknown error'));
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

  getTagColor(tag: Tag): string {
    
    if (this.themeService.getCurrentTheme() === 'low-stimulus') {
      return '#6b7280';
    }
    return tag.color || '#6b7280';
  }

  getButtonColor(color: string): string {
    
    if (this.themeService.getCurrentTheme() === 'low-stimulus') {
      return '#6b7280';
    }
    return color;
  }

  @HostListener('document:keydown.escape')
  handleEscapeKey(): void {
    if (this.showEditForm()) {
      this.cancelEdit();
    } else if (this.showCreateForm()) {
      this.cancelCreate();
    }
  }
}
