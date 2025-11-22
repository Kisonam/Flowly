import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { TasksService } from '../../services/tasks.service';
import { TagsService } from '../../../../shared/services/tags.service';
import { Task, TaskTheme, TaskFilter, PaginatedResult, TaskPriority, TasksStatus } from '../../models/task.models';
import { Subject, debounceTime, takeUntil, switchMap } from 'rxjs';

interface SortOption {
  key: 'dueDate' | 'createdAt' | 'priority' | 'status';
  label: string;
}

@Component({
  selector: 'app-task-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, TranslateModule],
  templateUrl: './task-list.component.html',
  styleUrls: ['./task-list.component.scss']
})
export class TaskListComponent implements OnInit, OnDestroy {
  private tasksService = inject(TasksService);
  private tagsService = inject(TagsService);
  private fb = inject(FormBuilder);
  private translate = inject(TranslateService);
  private destroy$ = new Subject<void>();

  // Data
  tasks: Task[] = [];
  themes: TaskTheme[] = [];
  tags: { id: string; name: string; color?: string }[] = [];
  page = 1;
  pageSize = 20;
  totalPages = 1;
  totalCount = 0;

  // UI state
  loading = false;
  errorMessage = '';
  empty = false;

  // Sorting
  sortOptions: SortOption[] = [];
  currentSort: SortOption | null = null;
  sortDirection: 'asc' | 'desc' = 'asc';

  // Filter form
  filterForm: FormGroup = this.fb.group({
    search: [''],
    status: [''],
    priority: [''],
    themeId: [''],
    tagIds: [[] as string[]],
    dueTo: ['']
  });

  ngOnInit(): void {
    this.initializeSortOptions();
    this.loadAuxData();
    this.setupFilterListeners();
    this.fetchTasks();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  initializeSortOptions(): void {
    this.sortOptions = [
      { key: 'dueDate', label: this.translate.instant('TASKS.LIST.SORT.DUE_DATE') },
      { key: 'createdAt', label: this.translate.instant('TASKS.LIST.SORT.CREATED') },
      { key: 'priority', label: this.translate.instant('TASKS.LIST.SORT.PRIORITY') },
      { key: 'status', label: this.translate.instant('TASKS.LIST.SORT.STATUS') }
    ];
  }


  loadAuxData(): void {
    // Load themes & tags in parallel
    this.tasksService.getThemes().pipe(takeUntil(this.destroy$)).subscribe({
      next: (themes) => (this.themes = themes),
      error: (err) => console.error('Failed to load themes', err)
    });
    this.tagsService.getTags().pipe(takeUntil(this.destroy$)).subscribe({
      next: (tags) => (this.tags = tags),
      error: (err) => console.error('Failed to load tags', err)
    });
  }

  setupFilterListeners(): void {
    this.filterForm.valueChanges
      .pipe(debounceTime(250), takeUntil(this.destroy$))
      .subscribe(() => {
        this.page = 1; // reset page on filter change
        this.fetchTasks();
      });
  }

  buildFilter(): TaskFilter {
    const v = this.filterForm.value;
    return {
      search: v.search?.trim() || undefined,
      status: v.status || undefined,
      priority: v.priority || undefined,
      themeIds: v.themeId ? [v.themeId] : undefined,
      tagIds: v.tagIds?.length ? v.tagIds : undefined,
      dueDateOn: v.dueTo || undefined,
      page: this.page,
      pageSize: this.pageSize
    };
  }

  fetchTasks(): void {
    this.loading = true;
    this.errorMessage = '';
    const filter = this.buildFilter();

    this.tasksService.getTasks(filter)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.tasks = this.applySorting(result.items.slice());
          this.totalCount = result.totalCount;
          this.page = result.page;
          this.pageSize = result.pageSize;
          this.totalPages = result.totalPages;
          this.empty = result.items.length === 0;
          this.loading = false;
        },
        error: (err) => {
          console.error('Failed to fetch tasks', err);
          this.errorMessage = err.message || 'Failed to fetch tasks';
          this.loading = false;
        }
      });
  }

  // Sorting
  setSort(option: SortOption): void {
    if (this.currentSort?.key === option.key) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.currentSort = option;
      this.sortDirection = 'asc';
    }
    this.tasks = this.applySorting(this.tasks.slice());
  }

  applySorting(list: Task[]): Task[] {
    if (!this.currentSort) return list;
    const dir = this.sortDirection === 'asc' ? 1 : -1;

    return list.sort((a, b) => {
      switch (this.currentSort!.key) {
        case 'dueDate': {
          const ad = a.dueDate ? new Date(a.dueDate).getTime() : 0;
          const bd = b.dueDate ? new Date(b.dueDate).getTime() : 0;
          return (ad - bd) * dir;
        }
        case 'createdAt': {
          const ac = new Date(a.createdAt).getTime();
          const bc = new Date(b.createdAt).getTime();
          return (ac - bc) * dir;
        }
        case 'priority': {
          return (priorityWeight(a.priority) - priorityWeight(b.priority)) * dir;
        }
        case 'status': {
          return (statusOrder(a.status) - statusOrder(b.status)) * dir;
        }
        default:
          return 0;
      }
    });

    function priorityWeight(p: TaskPriority): number {
      switch (p) {
        case 'High': return 3;
        case 'Medium': return 2;
        case 'Low': return 1;
        default: return 0;
      }
    }
    function statusOrder(s: TasksStatus): number {
      switch (s) {
        case 'Todo': return 0;
        case 'InProgress': return 1;
        case 'Done': return 2;
        default: return 99;
      }
    }
  }

  // Pagination
  nextPage(): void {
    if (this.page < this.totalPages) {
      this.page++;
      this.fetchTasks();
    }
  }

  prevPage(): void {
    if (this.page > 1) {
      this.page--;
      this.fetchTasks();
    }
  }

  // Tag selection toggle
  toggleTag(tagId: string): void {
    const current: string[] = this.filterForm.value.tagIds || [];
    if (current.includes(tagId)) {
      this.filterForm.patchValue({ tagIds: current.filter((id) => id !== tagId) });
    } else {
      this.filterForm.patchValue({ tagIds: [...current, tagId] });
    }
  }

  clearFilters(): void {
    this.filterForm.reset({
      search: '',
      status: '',
      priority: '',
      themeId: '',
      tagIds: [],
      dueTo: ''
    });
  }

  completeTask(taskId: string): void {
    this.tasksService.completeTask(taskId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          console.log(' Task completed, refreshing list');
          this.fetchTasks();
        },
        error: (err) => {
          console.error('Failed to complete task', err);
          this.errorMessage = err.message || 'Failed to complete task';
        }
      });
  }
}
