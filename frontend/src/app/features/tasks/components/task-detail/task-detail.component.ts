import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink, RouterModule } from '@angular/router';
import { filter, switchMap, takeUntil, tap } from 'rxjs';
import { Subject } from 'rxjs';
import { marked } from 'marked';

import { TasksService } from '../../services/tasks.service';
import { TagsService } from '../../../../shared/services/tags.service';
import { Task, Tag, Subtask } from '../../models/task.models';
import { LinkService } from '../../../../shared/services/link.service';
import { Link, LinkEntityType } from '../../../../shared/models/link.models';

@Component({
  selector: 'app-task-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterModule],
  templateUrl: './task-detail.component.html',
  styleUrls: ['./task-detail.component.scss']
})
export class TaskDetailComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private tasksService = inject(TasksService);
  private tagsService = inject(TagsService);
  private linkService = inject(LinkService);
  private destroy$ = new Subject<void>();

  taskId = '';
  task?: Task;
  loading = false;
  error = '';

  // Rendered markdown HTML for description
  descriptionHtml = '';

  // Tags
  allTags: { id: string; name: string; color?: string }[] = [];

  // Links
  links: Link[] = [];
  linkedNotes: any[] = [];
  linkedTasks: any[] = [];
  linkedTransactions: any[] = [];

  ngOnInit(): void {
    // Read id from route and load data
    this.route.paramMap
      .pipe(
        takeUntil(this.destroy$),
        filter(pm => pm.has('id')),
        tap(pm => (this.taskId = pm.get('id') || '')),
        tap(() => (this.loading = true)),
        switchMap(() => this.tasksService.getTask(this.taskId))
      )
      .subscribe({
        next: (task) => {
          this.task = task;
          this.descriptionHtml = this.renderMarkdown(task.description || '');
          this.loading = false;

          // Load links for this task
          this.loadLinks();
        },
        error: (err) => {
          this.error = err?.message || 'Failed to load task';
          this.loading = false;
        }
      });

    // Load tags list
    this.tagsService.getTags().pipe(takeUntil(this.destroy$)).subscribe({
      next: (tags) => (this.allTags = tags as any),
      error: (err) => console.error('Failed to load tags', err)
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // Subtasks inline toggle
  toggleSubtask(sub: Subtask): void {
    if (!this.task) return;
    const dto = { title: sub.title, isDone: !sub.isDone };
    this.tasksService.updateSubtask(this.task.id, sub.id, dto).pipe(takeUntil(this.destroy$)).subscribe({
      next: (updated) => {
        // Update local list
        if (!this.task) return;
        this.task.subtasks = this.task.subtasks.map(s => s.id === updated.id ? { ...updated } : s);
      },
      error: (err) => console.error('Failed to update subtask', err)
    });
  }

  // Tags toggle
  hasTag(tagId: string): boolean {
    return !!this.task?.tags.some(t => t.id === tagId);
  }

  toggleTag(tag: Tag): void {
    if (!this.task) return;
    const selected = this.hasTag(tag.id);
    const req$ = selected
      ? this.tasksService.removeTag(this.task.id, tag.id)
      : this.tasksService.addTag(this.task.id, tag.id);

    req$.pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        if (!this.task) return;
        if (selected) this.task.tags = this.task.tags.filter(t => t.id !== tag.id);
        else this.task.tags = [...this.task.tags, tag];
      },
      error: (err) => console.error('Failed to toggle tag', err)
    });
  }

  // =====================
  // Links
  // =====================
  private loadLinks(): void {
    if (!this.taskId) return;

    this.linkService.getLinksForTask(this.taskId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (links) => {
          this.links = links;
          this.processLinks(links);
        },
        error: (err) => {
          console.error('Failed to load links:', err);
        }
      });
  }

  private processLinks(links: Link[]): void {
    this.linkedNotes = [];
    this.linkedTasks = [];
    this.linkedTransactions = [];

    links.forEach(link => {
      // Get the "other" entity preview
      const preview = link.fromType === LinkEntityType.Task && link.fromId === this.taskId
        ? link.toPreview
        : link.fromPreview;

      if (!preview) return;

      const item = {
        id: preview.id,
        title: preview.title,
        snippet: preview.snippet,
        type: preview.type
      };

      switch (preview.type) {
        case LinkEntityType.Task:
          this.linkedTasks.push(item);
          break;
        case LinkEntityType.Transaction:
          this.linkedTransactions.push(item);
          break;
        case LinkEntityType.Note:
          this.linkedNotes.push(item);
          break;
      }
    });
  }

  // =====================
  // Markdown rendering
  // =====================
  private renderMarkdown(md: string): string {
    try {
      const raw = marked(md || '') as string;
      return this.replaceReferenceTokens(raw);
    } catch {
      return '<p>Error rendering content</p>';
    }
  }

  private replaceReferenceTokens(html: string): string {
    const refRegex = /\[\[(note|tx):([A-Za-z0-9\-]{6,})\|?([^\]]*)\]\]/g;
    return html.replace(refRegex, (_m, type: string, id: string, label: string) => {
      const kind = type === 'note' ? 'Note' : 'Tx';
      const text = label?.trim().length ? label.trim() : `${kind} ${id.substring(0,6)}…`;
      const cls = type === 'note' ? 'ref-pill task' : 'ref-pill tx';
      return `<span class="${cls}" data-id="${this.escapeHtml(id)}" data-type="${this.escapeHtml(type)}">${this.escapeHtml(text)}</span>`;
    });
  }

  private escapeHtml(str: string): string {
    return str
      .replace(/&/g,'&amp;')
      .replace(/</g,'&lt;')
      .replace(/>/g,'&gt;')
      .replace(/"/g,'&quot;')
      .replace(/'/g,'&#039;');
  }
}
