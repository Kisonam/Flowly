import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { Subject, takeUntil } from 'rxjs';
import { TasksService } from '../../services/tasks.service';
import { TaskTheme, CreateTaskThemeRequest, UpdateTaskThemeRequest } from '../../models/task.models';

@Component({
  selector: 'app-theme-manager',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DragDropModule],
  templateUrl: './theme-manager.component.html',
  styleUrls: ['./theme-manager.component.scss']
})
export class ThemeManagerComponent implements OnInit, OnDestroy {
  private tasksService = inject(TasksService);
  private fb = inject(FormBuilder);
  private destroy$ = new Subject<void>();

  themes: TaskTheme[] = [];
  loading = false;
  error = '';

  editingId: string | null = null;
  editForm: FormGroup = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(100)]],
    color: ['']
  });

  showNewForm = false;
  newForm: FormGroup = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(100)]],
    color: ['']
  });

  ngOnInit(): void {
    this.loadThemes();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadThemes(): void {
    this.loading = true;
    this.tasksService.getThemes().pipe(takeUntil(this.destroy$)).subscribe({
      next: (themes) => {
        this.themes = themes.sort((a, b) => a.order - b.order);
        this.loading = false;
      },
      error: (err) => {
        this.error = err?.message || 'Failed to load themes';
        this.loading = false;
      }
    });
  }

  startEdit(theme: TaskTheme): void {
    this.editingId = theme.id;
    this.editForm.patchValue({ title: theme.title, color: theme.color || '' });
  }

  cancelEdit(): void {
    this.editingId = null;
    this.editForm.reset();
  }

  saveEdit(theme: TaskTheme): void {
    if (this.editForm.invalid) return;
    const dto: UpdateTaskThemeRequest = {
      title: this.editForm.value.title,
      color: this.editForm.value.color || undefined
    };
    this.tasksService.updateTheme(theme.id, dto).pipe(takeUntil(this.destroy$)).subscribe({
      next: (updated) => {
        const idx = this.themes.findIndex(t => t.id === updated.id);
        if (idx >= 0) this.themes[idx] = updated;
        this.cancelEdit();
      },
      error: (err) => (this.error = err?.message || 'Failed to update theme')
    });
  }

  deleteTheme(id: string): void {
    if (!confirm('Delete this theme? Tasks will become unassigned.')) return;
    this.tasksService.deleteTheme(id).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.themes = this.themes.filter(t => t.id !== id);
      },
      error: (err) => (this.error = err?.message || 'Failed to delete theme')
    });
  }

  toggleNewForm(): void {
    this.showNewForm = !this.showNewForm;
    if (!this.showNewForm) this.newForm.reset();
  }

  createTheme(): void {
    if (this.newForm.invalid) return;
    const dto: CreateTaskThemeRequest = {
      title: this.newForm.value.title,
      color: this.newForm.value.color || undefined
    };
    this.tasksService.createTheme(dto).pipe(takeUntil(this.destroy$)).subscribe({
      next: (theme) => {
        this.themes.push(theme);
        this.themes.sort((a, b) => a.order - b.order);
        this.toggleNewForm();
      },
      error: (err) => (this.error = err?.message || 'Failed to create theme')
    });
  }

  onDrop(event: CdkDragDrop<TaskTheme[]>): void {
    if (event.previousIndex === event.currentIndex) return;
    moveItemInArray(this.themes, event.previousIndex, event.currentIndex);
    const ids = this.themes.map(t => t.id);
    this.tasksService.reorderThemes(ids).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => console.log('Themes reordered'),
      error: (err) => {
        this.error = err?.message || 'Failed to reorder';
        this.loadThemes(); 
      }
    });
  }
}
