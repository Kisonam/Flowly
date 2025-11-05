// frontend/src/app/features/notes/components/note-editor/note-editor.component.ts

import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, debounceTime, takeUntil } from 'rxjs';
import { marked } from 'marked';
import { NotesService } from '../../services/notes.service';
import { TagSelectorComponent } from '../../../../shared/components/tag-selector/tag-selector.component';
import { Note, CreateNoteRequest, UpdateNoteRequest, Tag } from '../../models/note.models';
import { TagsService } from '../../../../shared/services/tags.service';
import { TasksService } from '../../../../shared/services/tasks.service';
import { TransactionsService } from '../../../../shared/services/transactions.service';
import { TaskListItem } from '../../../../shared/models/tasks.models';
import { TransactionListItem } from '../../../../shared/models/transactions.models';

interface NoteDraft {
  title: string;
  markdown: string;
  tagIds: string[];
  timestamp: number;
}

@Component({
  selector: 'app-note-editor',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, TagSelectorComponent],
  templateUrl: './note-editor.component.html',
  styleUrls: ['./note-editor.component.scss']
})
export class NoteEditorComponent implements OnInit, OnDestroy {
  private notesService = inject(NotesService);
  private fb = inject(FormBuilder);
  private tagsService = inject(TagsService);
  private tasksService = inject(TasksService);
  private transactionsService = inject(TransactionsService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
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

  // References (tasks/transactions)
  refType: 'task' | 'tx' = 'task';
  refId: string = '';
  refLabel: string = '';
  // Dropdown sources
  taskOptions: TaskListItem[] = [];
  txOptions: TransactionListItem[] = [];
  refSearch = '';

  // Auto-save
  private autoSaveSubject = new Subject<void>();
  private readonly DRAFT_KEY_PREFIX = 'flowly_note_draft_';
  lastSavedTime: Date | null = null;

  ngOnInit(): void {
    this.initializeForm();
    this.setupAutoSave();
    this.loadRouteData();
    this.loadAvailableTags();
    // Preload default reference options (tasks)
    this.loadTaskOptions();
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

  private loadAvailableTags(): void {
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

  private loadTaskOptions(term?: string): void {
    this.tasksService.list({ search: term, isArchived: false, take: 50 })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (items) => {
          this.taskOptions = items || [];
          // Preselect first task if nothing is selected
          if (this.refType === 'task' && !this.refId && this.taskOptions.length > 0) {
            const first = this.taskOptions[0];
            this.refId = first.id;
            this.refLabel = first.title || '';
          }
        },
        error: (err) => {
          console.error('Failed to load tasks:', err);
          this.taskOptions = [];
        }
      });
  }

  private loadTxOptions(term?: string): void {
    this.transactionsService.list({ search: term, isArchived: false, take: 50 })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (items) => {
          this.txOptions = items || [];
          // Preselect first transaction if nothing is selected
          if (this.refType === 'tx' && !this.refId && this.txOptions.length > 0) {
            const first = this.txOptions[0];
            this.refId = first.id;
            const amount = new Intl.NumberFormat(undefined, { style: 'currency', currency: first.currencyCode }).format(first.amount);
            const date = new Date(first.date).toLocaleDateString();
            this.refLabel = `${amount} · ${date}` + (first.description ? ` · ${first.description}` : '');
          }
        },
        error: (err) => {
          console.error('Failed to load transactions:', err);
          this.txOptions = [];
        }
      });
  }

  private updatePreview(markdown: string): void {
    try {
      const html = marked(markdown || '') as string;
      this.markdownPreview = this.replaceReferenceTokens(html);
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
    if (confirm('Discard changes and return to notes list?')) {
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
      alert('Please select image files only');
      return;
    }

    // Upload first file (for simplicity)
    this.uploadFile(imageFiles[0]);
  }

  private uploadFile(file: File): void {
    if (!this.noteId && !this.isEditMode) {
      alert('Please save the note first before uploading images');
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
          alert('Failed to upload image');
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
  // References helpers
  // ============================================

  onRefTypeChanged(): void {
    // Preload options for the selected type
    this.refSearch = '';
    if (this.refType === 'task') {
      this.loadTaskOptions();
    } else {
      this.loadTxOptions();
    }
    // Selection will be set to the first item when options load
    this.refId = '';
    this.refLabel = '';
  }

  onSearchRef(term: string): void {
    this.refSearch = term;
    if (this.refType === 'task') {
      this.loadTaskOptions(term);
    } else {
      this.loadTxOptions(term);
    }
  }

  onSelectTask(id: string): void {
    this.refId = id;
    const item = this.taskOptions.find(t => t.id === id);
    this.refLabel = item ? item.title : '';
  }

  onSelectTx(id: string): void {
    this.refId = id;
    const item = this.txOptions.find(t => t.id === id);
    if (item) {
      const amount = new Intl.NumberFormat(undefined, { style: 'currency', currency: item.currencyCode }).format(item.amount);
      const date = new Date(item.date).toLocaleDateString();
      this.refLabel = `${amount} · ${date}` + (item.description ? ` · ${item.description}` : '');
    } else {
      this.refLabel = '';
    }
  }

  insertReference(): void {
    const id = (this.refId || '').trim();
    if (!id) {
      alert('Оберіть завдання або транзакцію зі списку');
      return;
    }
    const label = (this.refLabel || '').trim();
    const token = label
      ? `[[${this.refType}:${id}|${label}]]`
      : `[[${this.refType}:${id}]]`;

    const currentMarkdown = this.noteForm.get('markdown')?.value || '';
    const insertion = (currentMarkdown.endsWith('\n') ? '' : '\n') + token + '\n';
    this.noteForm.patchValue({ markdown: currentMarkdown + insertion });

    // Reset inputs
    this.refId = '';
    this.refLabel = '';
  }

  private replaceReferenceTokens(html: string): string {
    // Matches [[task:GUID|Label]] or [[tx:GUID|Label]] (label optional)
    const refRegex = /\[\[(task|tx):([A-Za-z0-9\-]{6,})\|?([^\]]*)\]\]/g;
    return html.replace(refRegex, (_match, type: string, id: string, label: string) => {
      const kind = type === 'task' ? 'Завдання' : 'Транзакція';
      const text = (label && label.trim().length > 0) ? label.trim() : `${kind} ${id.substring(0, 6)}…`;
      const cls = type === 'task' ? 'ref-pill task' : 'ref-pill tx';
      // Safe HTML span we can style; later can turn into routerLink
      return `<span class="${cls}" data-id="${id}" data-type="${type}"><i class="bi ${type === 'task' ? 'bi-check2-square' : 'bi-cash-coin'}"></i> ${this.escapeHtml(text)}</span>`;
    });
  }

  private escapeHtml(str: string): string {
    return str
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#039;');
  }
}
