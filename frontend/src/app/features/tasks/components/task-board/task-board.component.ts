import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DragDropModule, CdkDragDrop } from '@angular/cdk/drag-drop';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { Subject, forkJoin, takeUntil } from 'rxjs';
import { TasksService } from '../../services/tasks.service';
import { Task } from '../../models/task.models';

interface ThemeColumn {
  id: string | null; // null for unassigned
  title: string;
  color?: string;
  order: number;
}

@Component({
  selector: 'app-task-board',
  standalone: true,
  imports: [CommonModule, FormsModule, DragDropModule, RouterModule, TranslateModule],
  templateUrl: './task-board.component.html',
  styleUrls: ['./task-board.component.scss']
})
export class TaskBoardComponent implements OnInit, OnDestroy {
  private tasksService = inject(TasksService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private translate = inject(TranslateService);
  private destroy$ = new Subject<void>();

  // Columns: themes + Unassigned
  columns: ThemeColumn[] = [];
  tasksByColumn = new Map<string | null, Task[]>();

  // UI state
  loading = false;
  errorMessage = '';

  // Add theme form
  showAddTheme = false;
  newTheme = { title: '', color: '#8b5cf6' };

  // Quick add per column
  newTaskTitles = new Map<string | null, string>();


  // Filters panel state
  showFilters = false;
  showArchivedTasks = false;
  filter: { status?: string; priority?: string; search?: string; dueDateTo?: string } = {};

  // Returns [done, total] subtasks count for a task
  getSubtaskProgress(task: Task): [number, number] {
    if (!task.subtasks || !Array.isArray(task.subtasks)) return [0, 0];
    const total = task.subtasks.length;
    const done = task.subtasks.filter(s => s && s.isDone).length;
    return [done, total];
  }

  ngOnInit(): void {
    // Read route data to determine if showing archived tasks
    this.showArchivedTasks = this.route.snapshot.data['archived'] ?? false;
    this.loadBoard();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadBoard(): void {
    this.loading = true;
    this.errorMessage = '';

    forkJoin({
      themes: this.tasksService.getThemes(),
      tasks: this.tasksService.getTasks({ isArchived: this.showArchivedTasks, page: 1, pageSize: 1000 })
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ themes, tasks }) => {
          // Build columns: Unassigned first, then themes by order
          const cols: ThemeColumn[] = [
            { id: null, title: this.translate.instant('TASKS.BOARD.COLUMN.UNASSIGNED'), order: -1, color: '#94a3b8' },
            ...themes
              .slice()
              .sort((a, b) => a.order - b.order)
              .map<ThemeColumn>((t) => ({ id: t.id, title: t.title, color: t.color, order: t.order }))
          ];
          this.columns = cols;

          // Group tasks
          this.tasksByColumn.clear();
          for (const col of cols) this.tasksByColumn.set(col.id, []);
          for (const task of tasks.items) {
            const themeId = task.theme?.id ?? null;
            const arr = this.tasksByColumn.get(themeId) ?? [];
            arr.push(task);
            this.tasksByColumn.set(themeId, arr);
          }
          // Sort tasks within each column by order field
          for (const [themeId, tasks] of this.tasksByColumn.entries()) {
            tasks.sort((a, b) => a.order - b.order);
            this.tasksByColumn.set(themeId, tasks);
          }

          this.loading = false;
        },
        error: (err) => {
          console.error('Failed to load task board', err);
          this.errorMessage = err.message || 'Failed to load board';
          this.loading = false;
        }
      });
  }

  // Connected drop lists ids
  getDropListIds(): string[] {
    return this.columns.map((c) => this.listId(c.id));
  }

  listId(themeId: string | null): string {
    return `list-${themeId ?? 'null'}`;
  }

  // Ensure templates always work with Task[] (never undefined)
  listData(themeId: string | null): Task[] {
    return this.tasksByColumn.get(themeId) ?? [];
  }

  onTaskDrop(event: CdkDragDrop<Task[]>, targetThemeId: string | null): void {
    const prevThemeId = this.getThemeIdFromListId(event.previousContainer.id);
    const currThemeId = this.getThemeIdFromListId(event.container.id);

    const task: Task | undefined = event.item.data as Task;
    if (!task) return;

    const source = this.tasksByColumn.get(prevThemeId) ?? [];
    const target = this.tasksByColumn.get(currThemeId) ?? [];

    // Optimistic local update: remove from source, insert into target
    const idx = source.findIndex((t) => t.id === task.id);
    if (idx !== -1) {
      const [moved] = source.splice(idx, 1);
      this.tasksByColumn.set(prevThemeId, source);
      target.splice(event.currentIndex, 0, moved);
      this.tasksByColumn.set(currThemeId, target);
    }

    // Compute new order for all affected tasks in target column (and source if needed)
    const affectedItems: { taskId: string; themeId: string | null; order: number }[] = [];

    // Target column: assign sequential order from 0
    target.forEach((t, i) => {
      affectedItems.push({ taskId: t.id, themeId: currThemeId, order: i });
    });

    // If theme changed (different column), also update source column if it has tasks
    if (prevThemeId !== currThemeId && source.length > 0) {
      source.forEach((t, i) => {
        affectedItems.push({ taskId: t.id, themeId: prevThemeId, order: i });
      });
    }

    // Persist reorder (send only affected tasks)
    this.tasksService
      .reorderTasks(affectedItems)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          console.log('✅ Tasks reordered successfully');
        },
        error: (err) => {
          console.error('Failed to reorder tasks', err);
          // Revert on error
          const targetArr = this.tasksByColumn.get(currThemeId) ?? [];
          const revertIdx = targetArr.findIndex((t) => t.id === task.id);
          if (revertIdx !== -1) targetArr.splice(revertIdx, 1);
          this.tasksByColumn.set(currThemeId, targetArr);

          const sourceArr = this.tasksByColumn.get(prevThemeId) ?? [];
          sourceArr.splice(event.previousIndex, 0, task);
          this.tasksByColumn.set(prevThemeId, sourceArr);
          alert(err.message || this.translate.instant('COMMON.ERRORS.FAILED_TO_UPDATE'));
        }
      });
  }

  getThemeIdFromListId(listId: string): string | null {
    const suffix = listId.replace('list-', '');
    return suffix === 'null' ? null : suffix;
  }

  toggleAddTheme(): void {
    this.showAddTheme = !this.showAddTheme;
    if (this.showAddTheme) {
      this.newTheme = { title: '', color: '#8b5cf6' };
    }
  }

  saveTheme(): void {
    const title = this.newTheme.title.trim();
    if (!title) return;
    this.tasksService
      .createTheme({ title, color: this.newTheme.color })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.toggleAddTheme();
          this.loadBoard();
        },
        error: (err) => {
          console.error('Failed to create theme', err);
          alert(err.message || this.translate.instant('COMMON.ERRORS.FAILED_TO_CREATE'));
        }
      });
  }

  /** Navigate to full task editor with preselected theme via query param */
  quickAddTask(themeId: string | null): void {
    const queryParams: any = {};
    if (themeId) queryParams.themeId = themeId; // don't send for null (Unassigned)
    this.router.navigate(['/tasks/new'], { queryParams });
  }

  toggleFilters(): void {
    this.showFilters = !this.showFilters;
  }

  applyFilters(): void {
    // Re-fetch tasks with filters (themes unaffected)
    this.loading = true;
    const taskFilter: any = { isArchived: this.showArchivedTasks, page: 1, pageSize: 1000 };
    if (this.filter.status) taskFilter.status = this.filter.status;
    if (this.filter.priority) taskFilter.priority = this.filter.priority;
    if (this.filter.search) taskFilter.search = this.filter.search.trim();
    if (this.filter.dueDateTo) {
      // Date equality: send as dueDateOn (date-only). Service will normalize to start-of-day UTC.
      taskFilter.dueDateOn = this.filter.dueDateTo;
    }

    forkJoin({
      themes: this.tasksService.getThemes(),
      tasks: this.tasksService.getTasks(taskFilter)
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ themes, tasks }) => {
          const cols: ThemeColumn[] = [
            { id: null, title: this.translate.instant('TASKS.BOARD.COLUMN.UNASSIGNED'), order: -1, color: '#94a3b8' },
            ...themes
              .slice()
              .sort((a, b) => a.order - b.order)
              .map<ThemeColumn>((t) => ({ id: t.id, title: t.title, color: t.color, order: t.order }))
          ];
          this.columns = cols;
          this.tasksByColumn.clear();
          for (const col of cols) this.tasksByColumn.set(col.id, []);
          for (const task of tasks.items) {
            const themeId = task.theme?.id ?? null;
            const arr = this.tasksByColumn.get(themeId) ?? [];
            arr.push(task);
            this.tasksByColumn.set(themeId, arr);
          }
          // Sort tasks by order
          for (const [themeId, tasks] of this.tasksByColumn.entries()) {
            tasks.sort((a, b) => a.order - b.order);
            this.tasksByColumn.set(themeId, tasks);
          }
          this.loading = false;
        },
        error: (err) => {
          console.error('Failed to apply filters', err);
          this.errorMessage = err.message || 'Failed to apply filters';
          this.loading = false;
        }
      });
  }

  resetFilters(): void {
    this.filter = {};
    this.applyFilters();
  }

  onThemesDrop(event: CdkDragDrop<ThemeColumn[]>): void {
    if (event.previousIndex === event.currentIndex) return;

    // Reorder columns array
    const movedColumn = this.columns[event.previousIndex];
    this.columns.splice(event.previousIndex, 1);
    this.columns.splice(event.currentIndex, 0, movedColumn);

    // Extract theme IDs (skip Unassigned which is always first)
    const themeIds = this.columns
      .filter(c => c.id !== null)
      .map(c => c.id as string);

    // Send reorder request
    this.tasksService
      .reorderThemes(themeIds)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          console.log('✅ Themes reordered');
        },
        error: (err) => {
          console.error('Failed to reorder themes', err);
          // Revert
          this.columns.splice(event.currentIndex, 1);
          this.columns.splice(event.previousIndex, 0, movedColumn);
          alert(err.message || this.translate.instant('COMMON.ERRORS.FAILED_TO_UPDATE'));
        }
      });
  }

  deleteTheme(event: Event, themeId: string): void {
    event.stopPropagation();

    const column = this.columns.find(c => c.id === themeId);
    if (!column) return;

    const tasksInColumn = this.tasksByColumn.get(themeId) || [];
    const confirmMessage = tasksInColumn.length > 0
      ? this.translate.instant('TASKS.BOARD.CONFIRM_DELETE_THEME_WITH_TASKS', { title: column.title, count: tasksInColumn.length })
      : this.translate.instant('TASKS.BOARD.CONFIRM_DELETE_THEME', { title: column.title });

    if (!confirm(confirmMessage)) return;

    // Optimistic UI update
    const columnIndex = this.columns.findIndex(c => c.id === themeId);
    const removedColumn = this.columns[columnIndex];
    this.columns.splice(columnIndex, 1);

    // Move tasks to Unassigned
    const unassignedTasks = this.tasksByColumn.get(null) || [];
    this.tasksByColumn.set(null, [...unassignedTasks, ...tasksInColumn]);
    this.tasksByColumn.delete(themeId);

    this.tasksService
      .deleteTheme(themeId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          console.log('✅ Theme deleted');
        },
        error: (err) => {
          console.error('Failed to delete theme', err);
          // Revert
          this.columns.splice(columnIndex, 0, removedColumn);
          this.tasksByColumn.set(themeId, tasksInColumn);
          const revertedUnassigned = unassignedTasks;
          this.tasksByColumn.set(null, revertedUnassigned);
          alert(err.message || this.translate.instant('COMMON.ERRORS.FAILED_TO_DELETE'));
        }
      });
  }

  toggleTaskComplete(event: Event, task: Task): void {
    event.stopPropagation();
    const newStatus: 'Todo' | 'Done' = task.status === 'Done' ? 'Todo' : 'Done';

    // If completing a task (changing to Done), use completeTask endpoint to trigger recurrence
    if (newStatus === 'Done' && task.status !== 'Done') {
      this.tasksService
        .completeTask(task.id)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            console.log('✅ Task completed, reloading board');
            // Reload the entire board to show the new recurring task
            this.loadBoard();
          },
          error: (err) => {
            console.error('Failed to complete task', err);
            alert(err.message || this.translate.instant('COMMON.ERRORS.FAILED_TO_UPDATE'));
          }
        });
    } else {
      // For other status changes (e.g., uncompleting), use regular status change
      const oldStatus = task.status;
      task.status = newStatus;

      this.tasksService
        .changeStatus(task.id, newStatus)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            console.log('✅ Task status updated');
          },
          error: (err) => {
            console.error('Failed to update task status', err);
            task.status = oldStatus; // Revert
            alert(err.message || this.translate.instant('COMMON.ERRORS.FAILED_TO_UPDATE'));
          }
        });
    }
  }

  archiveTask(event: Event, task: Task): void {
    event.stopPropagation();
    if (!confirm(this.translate.instant('COMMON.CONFIRM.ARCHIVE_TASK', { title: task.title }))) return;

    // Find which column contains this task (check all columns including null for Unassigned)
    let foundColumnId: string | null | undefined = undefined;
    for (const [key, tasks] of this.tasksByColumn.entries()) {
      if (tasks.some(t => t.id === task.id)) {
        foundColumnId = key;
        break;
      }
    }

    // If not found, can't proceed
    if (foundColumnId === undefined) {
      console.error('Task not found in any column');
      return;
    }

    // Optimistic removal
    const tasks = this.tasksByColumn.get(foundColumnId) ?? [];
    const idx = tasks.findIndex(t => t.id === task.id);
    if (idx !== -1) {
      tasks.splice(idx, 1);
      this.tasksByColumn.set(foundColumnId, tasks);
    }

    this.tasksService
      .archiveTask(task.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          console.log('✅ Task archived');
        },
        error: (err) => {
          console.error('Failed to archive task', err);
          // Revert
          if (idx !== -1 && foundColumnId !== undefined) {
            tasks.splice(idx, 0, task);
            this.tasksByColumn.set(foundColumnId, tasks);
          }
          alert(err.message || this.translate.instant('COMMON.ERRORS.FAILED_TO_UPDATE'));
        }
      });
  }

}
