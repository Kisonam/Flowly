import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { TagsService, CreateTagRequest, UpdateTagRequest } from '../../../../shared/services/tags.service';
import { Tag } from '../../../notes/models/note.models';

@Component({
  selector: 'app-tags-manager',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './tags-manager.component.html',
  styleUrls: ['./tags-manager.component.scss']
})
export class TagsManagerComponent implements OnInit, OnDestroy {
  private tagsService = inject(TagsService);
  private destroy$ = new Subject<void>();

  tags: Tag[] = [];
  isLoading = false;
  errorMessage = '';

  showForm = false;
  isEditMode = false;
  editingTagId: string | null = null;
  formData: CreateTagRequest = {
    name: '',
    color: '#8b5cf6'
  };

  predefinedColors = [
    '#8b5cf6', 
    '#3b82f6', 
    '#10b981', 
    '#f59e0b', 
    '#ef4444', 
    '#ec4899', 
    '#06b6d4', 
    '#6366f1', 
  ];

  ngOnInit(): void {
    this.loadTags();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadTags(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.tagsService.getTags()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (tags) => {
          this.tags = tags;
          this.isLoading = false;
        },
        error: (error) => {
          this.errorMessage = error.message || 'Failed to load tags';
          this.isLoading = false;
        }
      });
  }

  openCreateForm(): void {
    this.showForm = true;
    this.isEditMode = false;
    this.editingTagId = null;
    this.formData = {
      name: '',
      color: '#8b5cf6'
    };
  }

  openEditForm(tag: Tag): void {
    this.showForm = true;
    this.isEditMode = true;
    this.editingTagId = tag.id;
    this.formData = {
      name: tag.name,
      color: tag.color || '#8b5cf6'
    };
  }

  closeForm(): void {
    this.showForm = false;
    this.isEditMode = false;
    this.editingTagId = null;
    this.formData = {
      name: '',
      color: '#8b5cf6'
    };
  }

  onSubmit(): void {
    if (!this.formData.name.trim()) {
      alert('Tag name is required');
      return;
    }

    if (this.isEditMode && this.editingTagId) {
      this.updateTag();
    } else {
      this.createTag();
    }
  }

  createTag(): void {
    this.tagsService.createTag(this.formData)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.loadTags();
          this.closeForm();
        },
        error: (error) => {
          alert(error.message || 'Failed to create tag');
        }
      });
  }

  updateTag(): void {
    if (!this.editingTagId) return;

    const request: UpdateTagRequest = {
      name: this.formData.name,
      color: this.formData.color
    };

    this.tagsService.updateTag(this.editingTagId, request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.loadTags();
          this.closeForm();
        },
        error: (error) => {
          alert(error.message || 'Failed to update tag');
        }
      });
  }

  deleteTag(tag: Tag): void {
    if (!confirm(`Are you sure you want to delete tag "${tag.name}"? This will remove it from all notes and tasks.`)) {
      return;
    }

    this.tagsService.deleteTag(tag.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.loadTags();
        },
        error: (error) => {
          alert(error.message || 'Failed to delete tag');
        }
      });
  }

  selectColor(color: string): void {
    this.formData.color = color;
  }
}
