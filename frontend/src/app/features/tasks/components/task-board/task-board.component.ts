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
  id: string | null; 
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

  columns: ThemeColumn[] = [];
  tasksByColumn = new Map<string | null, Task[]>();

  loading = false;
  errorMessage = '';

  showAddTheme = false;
  newTheme = { title: '', color: '#8b5cf6' };

  newTaskTitles = new Map<string | null, string>();

  showFilters = false;
  showArchivedTasks = false;
  filter: { status?: string; priority?: string; search?: string; dueDateTo?: string } = {};

  getSubtaskProgress(task: Task): [number, number] {
    if (!task.subtasks || !Array.isArray(task.subtasks)) return [0, 0];
    const total = task.subtasks.length;
    const done = task.subtasks.filter(s => s && s.isDone).length;
    return [done, total];
  }

  ngOnInit(): void {
    
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

  getDropListIds(): string[] {
    return this.columns.map((c) => this.listId(c.id));
  }

  listId(themeId: string | null): string {
    return `list-${themeId ?? 'null'}`;
  }

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

    const idx = source.findIndex((t) => t.id === task.id);
    if (idx !== -1) {
      const [moved] = source.splice(idx, 1);
      this.tasksByColumn.set(prevThemeId, source);
      target.splice(event.currentIndex, 0, moved);
      this.tasksByColumn.set(currThemeId, target);
    }

    const affectedItems: { taskId: string; themeId: string | null; order: number }[] = [];

    target.forEach((t, i) => {
      affectedItems.push({ taskId: t.id, themeId: currThemeId, order: i });
    });

    if (prevThemeId !== currThemeId && source.length > 0) {
      source.forEach((t, i) => {
        affectedItems.push({ taskId: t.id, themeId: prevThemeId, order: i });
      });
    }

    this.tasksService
      .reorderTasks(affectedItems)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          console.log('✅ Tasks reordered successfully');
        },
        error: (err) => {
          console.error('Failed to reorder tasks', err);
          
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

  quickAddTask(themeId: string | null): void {
    const queryParams: any = {};
    if (themeId) queryParams.themeId = themeId; 
    this.router.navigate(['/tasks/new'], { queryParams });
  }

  toggleFilters(): void {
    this.showFilters = !this.showFilters;
  }

  applyFilters(): void {
    
    this.loading = true;
    const taskFilter: any = { isArchived: this.showArchivedTasks, page: 1, pageSize: 1000 };
    if (this.filter.status) taskFilter.status = this.filter.status;
    if (this.filter.priority) taskFilter.priority = this.filter.priority;
    if (this.filter.search) taskFilter.search = this.filter.search.trim();
    if (this.filter.dueDateTo) {
      
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

    const movedColumn = this.columns[event.previousIndex];
    this.columns.splice(event.previousIndex, 1);
    this.columns.splice(event.currentIndex, 0, movedColumn);

    const themeIds = this.columns
      .filter(c => c.id !== null)
      .map(c => c.id as string);

    this.tasksService
      .reorderThemes(themeIds)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          console.log('✅ Themes reordered');
        },
        error: (err) => {
          console.error('Failed to reorder themes', err);
          
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

    const columnIndex = this.columns.findIndex(c => c.id === themeId);
    const removedColumn = this.columns[columnIndex];
    this.columns.splice(columnIndex, 1);

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

    if (newStatus === 'Done' && task.status !== 'Done') {
      this.tasksService
        .completeTask(task.id)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            console.log('✅ Task completed, reloading board');
            
            this.loadBoard();
          },
          error: (err) => {
            console.error('Failed to complete task', err);
            alert(err.message || this.translate.instant('COMMON.ERRORS.FAILED_TO_UPDATE'));
          }
        });
    } else {
      
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
            task.status = oldStatus; 
            alert(err.message || this.translate.instant('COMMON.ERRORS.FAILED_TO_UPDATE'));
          }
        });
    }
  }

  archiveTask(event: Event, task: Task): void {
    event.stopPropagation();
    if (!confirm(this.translate.instant('COMMON.CONFIRM.ARCHIVE_TASK', { title: task.title }))) return;

    let foundColumnId: string | null | undefined = undefined;
    for (const [key, tasks] of this.tasksByColumn.entries()) {
      if (tasks.some(t => t.id === task.id)) {
        foundColumnId = key;
        break;
      }
    }

    if (foundColumnId === undefined) {
      console.error('Task not found in any column');
      return;
    }

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
          
          if (idx !== -1 && foundColumnId !== undefined) {
            tasks.splice(idx, 0, task);
            this.tasksByColumn.set(foundColumnId, tasks);
          }
          alert(err.message || this.translate.instant('COMMON.ERRORS.FAILED_TO_UPDATE'));
        }
      });
  }

}
