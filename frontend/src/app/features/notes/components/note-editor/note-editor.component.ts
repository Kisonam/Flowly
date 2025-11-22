// frontend/src/app/features/notes/components/note-editor/note-editor.component.ts

import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, debounceTime, takeUntil } from 'rxjs';
import { marked } from 'marked';
import { NotesService } from '../../services/notes.service';
import { TagManagerComponent } from '../../../../shared/components/tag-manager/tag-manager.component';
import { LinkSelectorComponent } from '../../../../shared/components/link-selector/link-selector.component';
import { Note, CreateNoteRequest, UpdateNoteRequest, Tag } from '../../models/note.models';
import { TagsService } from '../../../../shared/services/tags.service';
import { Link, LinkEntityType } from '../../../../shared/models/link.models';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

interface NoteDraft {
  title: string;
  markdown: string;
  tagIds: string[];
  timestamp: number;
}

@Component({
  selector: 'app-note-editor',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, TagManagerComponent, LinkSelectorComponent, TranslateModule],
  templateUrl: './note-editor.component.html',
  styleUrls: ['./note-editor.component.scss']
})
export class NoteEditorComponent implements OnInit, OnDestroy {
  private notesService = inject(NotesService);
  private fb = inject(FormBuilder);
  private tagsService = inject(TagsService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private translate = inject(TranslateService);
  private destroy$ = new Subject<void>();

  noteForm!: FormGroup;
  noteId: string | null = null;
  isEditMode = false;
  isLoading = false;
  isSaving = false;
  errorMessage = '';

  // Preview state
  markdownPreview = '';
  isPreviewMode = false;
  isSplitView = true;

  // Available tags (will be loaded from server or mock)
  availableTags: Tag[] = [];
  selectedTagIds: string[] = [];

  // File upload
  uploadedFiles: File[] = [];
  isDragging = false;
  uploadProgress: number | null = null;

  // Expose LinkEntityType to template
  LinkEntityType = LinkEntityType;

  // Auto-save
  private autoSaveSubject = new Subject<void>();
  private readonly DRAFT_KEY_PREFIX = 'flowly_note_draft_';
  lastSavedTime: Date | null = null;

  ngOnInit(): void {
    this.initializeForm();
    this.setupAutoSave();
    this.loadRouteData();
    this.loadAvailableTags();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeForm(): void {
    this.noteForm = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(200)]],
      markdown: ['', Validators.required]
    });

    // Update preview on markdown changes
    this.noteForm.get('markdown')?.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(markdown => {
        this.updatePreview(markdown);
      });

    // Trigger auto-save on form changes
    this.noteForm.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.autoSaveSubject.next();
      });
  }

  private setupAutoSave(): void {
    this.autoSaveSubject
      .pipe(
        debounceTime(2000), // Auto-save after 2 seconds of inactivity
        takeUntil(this.destroy$)
      )
      .subscribe(() => {
        this.saveDraft();
      });
  }

  private loadRouteData(): void {
    this.noteId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.noteId;

    if (this.isEditMode && this.noteId) {
      this.loadNote(this.noteId);
    } else {
      // Check for draft in localStorage
      this.loadDraft();
    }
  }

  private loadNote(id: string): void {
    this.isLoading = true;
    this.notesService.getNoteById(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (note) => {
          this.populateForm(note);
          this.isLoading = false;
        },
        error: (error) => {
          this.errorMessage = 'Failed to load note';
          console.error('Error loading note:', error);
          this.isLoading = false;
        }
      });
  }

  private populateForm(note: Note): void {
    this.noteForm.patchValue({
      title: note.title,
      markdown: note.markdown
    });
    this.selectedTagIds = note.tags.map(t => t.id);
    this.updatePreview(note.markdown);
  }

  loadAvailableTags(): void {
    this.tagsService.getTags()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (tags) => {
          this.availableTags = tags || [];
        },
        error: (err) => {
          console.error('Failed to load tags:', err);
          this.availableTags = [];
        }
      });
  }

  private updatePreview(markdown: string): void {
    try {
      const html = marked(markdown || '') as string;
      this.markdownPreview = html;
    } catch (error) {
      console.error('Error parsing markdown:', error);
      this.markdownPreview = '<p>Error rendering preview</p>';
    }
  }

  // ============================================
  // Form Actions
  // ============================================

  onSubmit(): void {
    if (this.noteForm.invalid) {
      this.noteForm.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';

    const formValue = this.noteForm.value;

    if (this.isEditMode && this.noteId) {
      this.updateNote(this.noteId, formValue);
    } else {
      this.createNote(formValue);
    }
  }

  private createNote(formValue: any): void {
    // Filter out mock tags and invalid IDs
    const realTags = this.sanitizeTagIds(this.selectedTagIds);

     console.log('🏷️ Selected tag IDs:', this.selectedTagIds);
     console.log('✅ Real tag IDs (filtered):', realTags);

    const request: CreateNoteRequest = {
      title: formValue.title,
      markdown: formValue.markdown,
      tagIds: realTags.length > 0 ? realTags : undefined
    };

     console.log('📤 Sending create note request:', request);

    this.notesService.createNote(request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (note) => {
          console.log('Note created:', note);
          this.clearDraft();
          this.lastSavedTime = new Date();
          this.isSaving = false;
          // Navigate to notes list
          this.router.navigate(['/notes']);
        },
        error: (error) => {
          this.errorMessage = 'Failed to create note';
          console.error('Error creating note:', error);
          this.isSaving = false;
        }
      });
  }

  private updateNote(id: string, formValue: any): void {
    // Filter out mock tags and invalid IDs
    const realTags = this.sanitizeTagIds(this.selectedTagIds);

    const request: UpdateNoteRequest = {
      title: formValue.title,
      markdown: formValue.markdown,
      tagIds: realTags.length > 0 ? realTags : undefined
    };

    this.notesService.updateNote(id, request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (note) => {
          console.log('Note updated:', note);
          this.lastSavedTime = new Date();
          this.isSaving = false;
          this.router.navigate(['/notes']);
        },
        error: (error) => {
          this.errorMessage = 'Failed to update note';
          console.error('Error updating note:', error);
          this.isSaving = false;
        }
      });
  }

  onCancel(): void {
    const message = this.translate.instant('NOTES.EDITOR.CONFIRM_CANCEL');
    if (confirm(message)) {
      this.router.navigate(['/notes']);
    }
  }

  // ============================================
  // View Mode Toggle
  // ============================================

  togglePreview(): void {
    this.isPreviewMode = !this.isPreviewMode;
    if (this.isPreviewMode) {
      this.isSplitView = false;
    }
  }

  toggleSplitView(): void {
    this.isSplitView = !this.isSplitView;
    if (this.isSplitView) {
      this.isPreviewMode = false;
    }
  }

  // ============================================
  // Tag Management
  // ============================================

  onTagsChanged(ids: string[]): void {
    this.selectedTagIds = ids;
    this.autoSaveSubject.next();
  }

  toggleTag(tagId: string): void {
    const index = this.selectedTagIds.indexOf(tagId);
    if (index > -1) {
      this.selectedTagIds.splice(index, 1);
    } else {
      this.selectedTagIds.push(tagId);
    }
    this.autoSaveSubject.next();
  }

  isTagSelected(tagId: string): boolean {
    return this.selectedTagIds.includes(tagId);
  }

  // ============================================
  // File Upload (Drag & Drop)
  // ============================================

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;

    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.handleFiles(files);
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.handleFiles(input.files);
    }
  }

  private handleFiles(files: FileList): void {
    // Filter only images
    const imageFiles = Array.from(files).filter(file =>
      file.type.startsWith('image/')
    );

    if (imageFiles.length === 0) {
      alert(this.translate.instant('NOTES.EDITOR.ERRORS.IMAGES_ONLY'));
      return;
    }

    // Upload first file (for simplicity)
    this.uploadFile(imageFiles[0]);
  }

  private uploadFile(file: File): void {
    if (!this.noteId && !this.isEditMode) {
      alert(this.translate.instant('NOTES.EDITOR.ERRORS.SAVE_FIRST'));
      return;
    }

    this.uploadProgress = 0;

    this.notesService.uploadMedia(this.noteId || 'temp', file)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          console.log('File uploaded:', response);
          this.uploadProgress = null;

          // Insert markdown image syntax at cursor position
          const imageMarkdown = `\n![${file.name}](${response.mediaUrl})\n`;
          const currentMarkdown = this.noteForm.get('markdown')?.value || '';
          this.noteForm.patchValue({
            markdown: currentMarkdown + imageMarkdown
          });
        },
        error: (error) => {
          console.error('Error uploading file:', error);
          this.uploadProgress = null;
          alert(this.translate.instant('NOTES.EDITOR.ERRORS.UPLOAD_FAILED'));
        }
      });
  }

  // ============================================
  // Draft Management (localStorage)
  // ============================================

  private getDraftKey(): string {
    return this.DRAFT_KEY_PREFIX + (this.noteId || 'new');
  }

  private saveDraft(): void {
    const draft: NoteDraft = {
      title: this.noteForm.get('title')?.value || '',
      markdown: this.noteForm.get('markdown')?.value || '',
      tagIds: this.selectedTagIds,
      timestamp: Date.now()
    };

    localStorage.setItem(this.getDraftKey(), JSON.stringify(draft));
    console.log('💾 Draft saved');
  }

  private loadDraft(): void {
    const draftJson = localStorage.getItem(this.getDraftKey());
    if (draftJson) {
      try {
        const draft: NoteDraft = JSON.parse(draftJson);

        // Only load draft if it's less than 24 hours old
        const hoursSinceDraft = (Date.now() - draft.timestamp) / (1000 * 60 * 60);
        if (hoursSinceDraft < 24) {
          this.noteForm.patchValue({
            title: draft.title,
            markdown: draft.markdown
          });
          // Sanitize any stale/invalid tag IDs from older drafts
          this.selectedTagIds = this.sanitizeTagIds(draft.tagIds || []);
          if (draft.tagIds && draft.tagIds.length !== this.selectedTagIds.length) {
            console.log('🧹 Draft tag IDs sanitized:', this.selectedTagIds);
          }
          console.log('📂 Draft loaded');
        } else {
          this.clearDraft();
        }
      } catch (error) {
        console.error('Error loading draft:', error);
      }
    }
  }

  private clearDraft(): void {
    localStorage.removeItem(this.getDraftKey());
    console.log('🗑️ Draft cleared');
  }

  // ============================================
  // Helper Methods
  // ============================================

  getTagStyle(color: string | undefined): any {
    return {
      '--tag-color': color || '#6b7280'
    };
  }

  // ============================================
  // Internal helpers
  // ============================================

  private isGuid(id: string): boolean {
    if (typeof id !== 'string') return false;
    const guidRegex = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;
    return guidRegex.test(id);
  }

  private isMockGuid(id: string): boolean {
    return typeof id === 'string' && id.startsWith('00000000-0000-0000-0000-00000000000');
  }

  private sanitizeTagIds(ids: string[]): string[] {
    if (!Array.isArray(ids)) return [];
    return ids.filter(id => this.isGuid(id));
  }

  get titleControl() {
    return this.noteForm.get('title');
  }

  get markdownControl() {
    return this.noteForm.get('markdown');
  }

  // ============================================
  // Link Management
  // ============================================

  onLinkCreated(link: Link): void {
    console.log('Link created:', link);
  }

  onLinkDeleted(linkId: string): void {
    console.log('Link deleted:', linkId);
  }
}
