
import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { marked } from 'marked';

import { TasksService } from '../../services/tasks.service';
import { Task, Subtask, TaskPriority, TasksStatus } from '../../models/task.models';
import { LinkService } from '../../../../shared/services/link.service';
import { Link, LinkEntityType, EntityPreview } from '../../../../shared/models/link.models';
import { ThemeService } from '../../../../core/services/theme.service';

@Component({
  selector: 'app-task-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslateModule],
  templateUrl: './task-detail.component.html',
  styleUrls: ['./task-detail.component.scss']
})
export class TaskDetailComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private taskService = inject(TasksService);
  private linkService = inject(LinkService);
  private themeService = inject(ThemeService);
  private translate = inject(TranslateService);
  private destroy$ = new Subject<void>();

  task: Task | null = null;
  loading = false;
  errorMessage = '';

  // Links
  links: Link[] = [];
  linkedNotes: EntityPreview[] = [];
  linkedTasks: EntityPreview[] = [];
  linkedTransactions: EntityPreview[] = [];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadTask(id);
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadTask(id: string): void {
    this.loading = true;
    this.errorMessage = '';

    this.taskService.getTask(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (task) => {
          this.task = task;
          this.loading = false;
          this.loadLinks();
        },
        error: (err) => {
          console.error('❌ Failed to load task', err);
          this.errorMessage = this.translate.instant('TASKS.DETAIL.ERRORS.LOAD_FAILED');
          this.loading = false;
        }
      });
  }

  loadLinks(): void {
    if (!this.task) return;

    this.linkService.getLinksForEntity(LinkEntityType.Task, this.task.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (links) => {
          this.links = links;
          this.processLinks(links);
        },
        error: (err) => {
          console.error('❌ Failed to load links', err);
          // Optional: show error toast
        }
      });
  }

  processLinks(links: Link[]): void {
    this.linkedNotes = [];
    this.linkedTasks = [];
    this.linkedTransactions = [];

    links.forEach(link => {
      const preview = link.fromType === LinkEntityType.Task && link.fromId === this.task?.id
        ? link.toPreview
        : link.fromPreview;

      if (!preview) return;

      switch (preview.type) {
        case LinkEntityType.Note:
          this.linkedNotes.push(preview);
          break;
        case LinkEntityType.Task:
          this.linkedTasks.push(preview);
          break;
        case LinkEntityType.Transaction:
          this.linkedTransactions.push(preview);
          break;
      }
    });
  }

  toggleSubtask(index: number): void {
    if (!this.task || !this.task.subtasks) return;

    const subtask = this.task.subtasks[index];
    const updatedSubtask = { ...subtask, isDone: !subtask.isDone };

    // Optimistic update
    const originalSubtasks = [...this.task.subtasks];
    this.task.subtasks[index] = updatedSubtask;

    this.taskService.updateSubtask(this.task.id, subtask.id, { title: subtask.title, isDone: updatedSubtask.isDone })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (updated) => {
           // Update with server response if needed, but optimistic update usually enough for UI
           if (this.task) {
             this.task.subtasks[index] = updated;
           }
        },
        error: (err) => {
          console.error('❌ Failed to update subtask', err);
          // Revert optimistic update
          if (this.task) {
            this.task.subtasks = originalSubtasks;
          }
          alert(this.translate.instant('TASKS.DETAIL.ERRORS.UPDATE_SUBTASK'));
        }
      });
  }

  toggleTag(tagId: string): void {
    // In a real app, this would toggle the tag on the task
    // For now, we'll just log it
    console.log('Toggle tag:', tagId);
  }

  editTask(): void {
    if (this.task) {
      this.router.navigate(['/tasks', this.task.id, 'edit']);
    }
  }

  getPriorityColor(priority: TaskPriority): string {
    switch (priority) {
      case 'High': return 'var(--danger)';
      case 'Medium': return 'var(--warning)';
      case 'Low': return 'var(--info)';
      default: return 'var(--text-secondary)';
    }
  }

  getPriorityIcon(priority: TaskPriority): string {
    switch (priority) {
      case 'High': return '🔴';
      case 'Medium': return '🟡';
      case 'Low': return '🔵';
      default: return '⚪';
    }
  }

  getStatusColor(status: TasksStatus): string {
    switch (status) {
      case 'Done': return 'var(--success)';
      case 'InProgress': return 'var(--primary)';
      case 'Todo': return 'var(--text-secondary)';
      default: return 'var(--text-secondary)';
    }
  }

  getStatusIcon(status: TasksStatus): string {
    switch (status) {
      case 'Done': return '✅';
      case 'InProgress': return '⏳';
      case 'Todo': return '📝';
      default: return '❓';
    }
  }

  formatDate(date: string | Date): string {
    return new Date(date).toLocaleDateString(this.translate.currentLang, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  renderMarkdown(content: string | null | undefined): string {
    if (!content) return '';
    try {
      return marked(content) as string;
    } catch (e) {
      console.error('Markdown render error', e);
      return content;
    }
  }
}
