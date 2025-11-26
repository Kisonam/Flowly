import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators, FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, forkJoin, of, switchMap, takeUntil, tap, debounceTime } from 'rxjs';
import { marked } from 'marked';
import { TasksService } from '../../services/tasks.service';
import { Task, TaskPriority, TasksStatus, CreateTaskRequest, UpdateTaskRequest, Tag } from '../../models/task.models';
import { LinkSelectorComponent } from '../../../../shared/components/link-selector/link-selector.component';
import { Link, LinkEntityType } from '../../../../shared/models/link.models';
import { TagManagerComponent } from '../../../../shared/components/tag-manager/tag-manager.component';
import { TagsService } from '../../../../shared/services/tags.service';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-task-editor',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, LinkSelectorComponent, TagManagerComponent, TranslateModule],
  templateUrl: './task-editor.component.html',
  styleUrls: ['./task-editor.component.scss']
})
export class TaskEditorComponent implements OnInit, OnDestroy {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  router = inject(Router); // public for template
  private tasksService = inject(TasksService);
  private tagsService = inject(TagsService);
  private destroy$ = new Subject<void>();

  isEdit = false;
  taskId: string | null = null;
  task?: Task;
  // Preselected theme when coming from board quick button
  initialThemeId: string | null = null;
  // Markdown preview state & references
  isPreviewMode = false;
  isSplitView = true;
  descriptionPreview = '';
  private autoPreview$ = new Subject<string>();
  // Cancel handler reused from note editor semantics
  onCancel(): void { this.router.navigate(['/tasks/board']); }

  // Expose LinkEntityType to template
  LinkEntityType = LinkEntityType;

  // Tags state
  availableTags: Tag[] = [];
  selectedTagIds: string[] = [];

  loading = false;
  saving = false;
  error = '';
  pastDateWarning = '';

  readonly statusOptions: TasksStatus[] = ['Todo', 'InProgress', 'Done'];
  readonly priorityOptions: TaskPriority[] = ['None', 'Low', 'Medium', 'High'];
  readonly recurrenceOptions = [
    { key: '', label: 'None' },
    { key: 'FREQ=DAILY', label: 'Daily' },
    { key: 'FREQ=WEEKLY', label: 'Weekly' },
    { key: 'FREQ=MONTHLY', label: 'Monthly' }
  ];

  form: FormGroup = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: [''],
    status: ['Todo', Validators.required],
    priority: ['None', Validators.required],
    dueDate: [''],
    dueTime: [{ value: '', disabled: true }], // Disabled by default until date is selected
    // HTML color input does not accept empty string; set safe default to avoid console warning
    color: ['#000000'],
    subtasks: this.fb.array([]),
    enableRecurrence: [false],
    recurrenceRule: ['']
  });

  get subtasks(): FormArray {
    return this.form.get('subtasks') as FormArray;
  }

  ngOnInit(): void {
    // Capture query param for theme preselection (creation mode)
    this.initialThemeId = this.route.snapshot.queryParamMap.get('themeId');

    // Load available tags
    this.loadAvailableTags();

    this.route.paramMap
      .pipe(
        takeUntil(this.destroy$),
        tap(pm => {
          const id = pm.get('id');
          this.isEdit = !!id;
          this.taskId = id;
        }),
        switchMap(() => {
          if (!this.isEdit || !this.taskId) return of(null);
          this.loading = true;
          return this.tasksService.getTask(this.taskId);
        })
      )
      .subscribe({
        next: (task) => {
          if (task) {
            this.task = task;
            this.patchForm(task);
            this.loading = false;
          }
          // If creating new and have themeId, patch hidden value (done in submit)
          if (!this.isEdit && this.initialThemeId) {
            // no direct form field yet; theme applied in createDto
          }
        },
        error: (err) => {
          this.error = err?.message || 'Failed to load task';
          this.loading = false;
        }
      });

    // Live markdown preview & reference preload
    this.form.get('description')?.valueChanges.pipe(takeUntil(this.destroy$)).subscribe(val => this.autoPreview$.next(val || ''));
    this.autoPreview$.pipe(debounceTime(250), takeUntil(this.destroy$)).subscribe((md: string) => this.updatePreview(md));

    // Check for past due date and clear time if date is removed
    this.form.get('dueDate')?.valueChanges.pipe(takeUntil(this.destroy$)).subscribe((dateValue) => {
      this.checkPastDueDate();
      const timeControl = this.form.get('dueTime');
      if (!dateValue) {
        // Clear and disable time if date is removed
        timeControl?.setValue('', { emitEvent: false });
        timeControl?.disable({ emitEvent: false });
      } else {
        // Enable time if date is selected
        timeControl?.enable({ emitEvent: false });
      }
    });
    this.form.get('dueTime')?.valueChanges.pipe(takeUntil(this.destroy$)).subscribe(() => this.checkPastDueDate());
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private patchForm(task: Task): void {
    this.form.patchValue({
      title: task.title,
      description: task.description || '',
      status: task.status,
      priority: task.priority,
      dueDate: task.dueDate ? this.toDateInputValue(task.dueDate) : '',
      dueTime: task.dueDate ? this.toTimeInputValue(task.dueDate) : '',
      color: task.color || ''
    });

    // Load tags
    this.selectedTagIds = task.tags?.map(t => t.id) || [];

    // Load existing subtasks
    this.subtasks.clear();
    if (task.subtasks && task.subtasks.length > 0) {
      task.subtasks.forEach(subtask => {
        this.subtasks.push(this.fb.group({
          title: [subtask.title, Validators.required],
          id: [subtask.id] // Keep track of existing subtask IDs
        }));
      });
    }

    // recurrence
    if (task.recurrence?.rule) {
      this.form.patchValue({ enableRecurrence: true, recurrenceRule: task.recurrence.rule });
    }
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

  onTagsChanged(tagIds: string[]): void {
    this.selectedTagIds = tagIds;
  }

  addSubtaskField(): void {
    this.subtasks.push(this.fb.group({ title: ['', Validators.required] }));
  }

  removeSubtaskField(i: number): void {
    this.subtasks.removeAt(i);
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving = true;
    const v = this.form.value;

    if (!this.isEdit) {
      const dueDateUtc = this.combineDateTimeToUtc(v.dueDate, v.dueTime);
      const createDto: CreateTaskRequest = {
        title: v.title,
        description: v.description || undefined,
        dueDate: dueDateUtc || undefined,
        color: v.color || undefined,
        priority: v.priority,
        themeId: this.initialThemeId || undefined
      };

      console.log('📤 Creating task with DTO:', createDto);

      this.tasksService.createTask(createDto).pipe(
        switchMap((task) => {
          this.task = task;
          const calls = [] as any[];

          // apply non-default status via update
          if (v.status && v.status !== 'Todo') {
            const updateDto: UpdateTaskRequest = {
              title: task.title,
              description: task.description || undefined,
                dueDate: task.dueDate ? (typeof task.dueDate === 'string' ? task.dueDate : task.dueDate.toISOString().split('T')[0]) : undefined,
              status: v.status,
              priority: v.priority,
              color: task.color || undefined,
              themeId: task.theme?.id,
              tagIds: this.selectedTagIds
            };
            calls.push(this.tasksService.updateTask(task.id, updateDto));
          }

          // add subtasks
          const subFields = this.subtasks.controls as FormGroup[];
          subFields.forEach(ctrl => {
            const title = ctrl.value.title?.trim();
            if (title) calls.push(this.tasksService.addSubtask(task.id, { title }));
          });

          // recurrence
          if (v.enableRecurrence && v.recurrenceRule) {
            calls.push(this.tasksService.setRecurrence(task.id, { rule: v.recurrenceRule }));
          }

          return calls.length ? forkJoin(calls) : of(null);
        })
      ).subscribe({
        next: () => {
          this.saving = false;
          // Navigate to tasks board
          this.router.navigate(['/tasks/board']);
        },
        error: (err) => {
          this.error = err?.message || 'Failed to create task';
          this.saving = false;
        }
      });
    } else if (this.isEdit && this.taskId) {
      const dueDateUtc = this.combineDateTimeToUtc(v.dueDate, v.dueTime);

      // Check if status is being changed to Done
      const isCompletingTask = v.status === 'Done' && this.task?.status !== 'Done';

      if (isCompletingTask) {
        // Use completeTask endpoint to trigger recurrence logic
        this.tasksService.completeTask(this.taskId).subscribe({
          next: () => {
            this.saving = false;
            this.router.navigate(['/tasks']);
          },
          error: (err) => {
            this.error = err?.message || 'Failed to complete task';
            this.saving = false;
          }
        });
      } else {
        // Normal update for other changes
        const updateDto: UpdateTaskRequest = {
          title: v.title,
          description: v.description || undefined,
          dueDate: dueDateUtc || undefined,
          status: v.status,
          priority: v.priority,
          color: v.color || undefined,
          themeId: this.task?.theme?.id,
          tagIds: this.selectedTagIds
        };

        this.tasksService.updateTask(this.taskId, updateDto).pipe(
          switchMap((task) => {
            this.task = task;
            const calls = [] as any[];

            // add newly entered subtasks
            const subFields = this.subtasks.controls as FormGroup[];
            subFields.forEach(ctrl => {
              const title = ctrl.value.title?.trim();
              if (title) calls.push(this.tasksService.addSubtask(task.id, { title }));
            });

            // set or clear recurrence (we only support set via UI; clearing could be added later)
            if (v.enableRecurrence && v.recurrenceRule) {
              calls.push(this.tasksService.setRecurrence(task.id, { rule: v.recurrenceRule }));
            }

            return calls.length ? forkJoin(calls) : of(null);
          })
        ).subscribe({
          next: () => {
            this.saving = false;
            this.router.navigate(['/tasks', this.taskId]);
          },
          error: (err) => {
            this.error = err?.message || 'Failed to save task';
            this.saving = false;
          }
        });
      }
    }
  }

  private toDateInputValue(d: string | Date): string {
    const date = d instanceof Date ? d : new Date(d);
    // Extract local date components
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private toTimeInputValue(d: string | Date): string {
    const date = d instanceof Date ? d : new Date(d);
    // Extract local time components
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${hours}:${minutes}`;
  }

  private combineDateTimeToUtc(dateStr?: string, timeStr?: string): string | null {
    if (!dateStr) return null;
    // If no time provided treat as 23:59 local (soft deadline end of day)
    const time = timeStr && timeStr.length ? timeStr : '23:59';
    const [y,m,d] = dateStr.split('-').map(p => parseInt(p,10));
    const [hh,mm] = time.split(':').map(p => parseInt(p,10));
    if ([y,m,d].some(isNaN) || [hh,mm].some(isNaN)) return null;
    // Construct local date - Date constructor already handles local timezone
    const local = new Date(y, m-1, d, hh, mm, 0, 0);
    // Convert to ISO string (already in UTC)
    return local.toISOString();
  }

  private checkPastDueDate(): void {
    const dateStr = this.form.get('dueDate')?.value;
    const timeStr = this.form.get('dueTime')?.value;

    if (!dateStr) {
      this.pastDateWarning = '';
      return;
    }

    const dueDateUtc = this.combineDateTimeToUtc(dateStr, timeStr);
    if (!dueDateUtc) {
      this.pastDateWarning = '';
      return;
    }

    const dueDate = new Date(dueDateUtc);
    const now = new Date();

    if (dueDate < now) {
      this.pastDateWarning = 'TASKS.EDITOR.PAST_DATE_WARNING';
    } else {
      this.pastDateWarning = '';
    }
  }

  // =====================
  // Markdown preview
  // =====================
  togglePreview(): void { this.isPreviewMode = !this.isPreviewMode; if (this.isPreviewMode) this.isSplitView = false; }
  toggleSplitView(): void { this.isSplitView = !this.isSplitView; if (this.isSplitView) this.isPreviewMode = false; }
  private updatePreview(md: string): void {
    try {
      const raw = marked(md || '') as string;
      this.descriptionPreview = raw;
    } catch {
      this.descriptionPreview = '<p>Error rendering preview</p>';
    }
  }

  // =====================
  // Link handlers
  // =====================
  onLinkCreated(link: Link): void {
    console.log('✅ Link created:', link);
  }

  onLinkDeleted(linkId: string): void {
    console.log('🗑️ Link deleted:', linkId);
  }
}
