import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { UpcomingTask } from '../../models/dashboard.models';

@Component({
  selector: 'app-upcoming-tasks',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule],
  templateUrl: './upcoming-tasks.component.html',
  styleUrls: ['./upcoming-tasks.component.scss']
})
export class UpcomingTasksComponent {
  @Input() set tasks(value: UpcomingTask[]) {
    console.log('📋 UpcomingTasksComponent received tasks:', value);
    this._tasks = value;
  }
  get tasks(): UpcomingTask[] {
    return this._tasks;
  }
  private _tasks: UpcomingTask[] = [];

  getPriorityClass(priority: string): string {
    switch (priority) {
      case 'High': return 'priority-high';
      case 'Medium': return 'priority-medium';
      case 'Low': return 'priority-low';
      default: return '';
    }
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Done': return 'status-done';
      case 'InProgress': return 'status-in-progress';
      case 'Todo': return 'status-todo';
      default: return '';
    }
  }

  formatDueDate(dateString: string | undefined): string {
    if (!dateString) return 'No due date';

    const date = new Date(dateString);
    const now = new Date();
    const diffTime = date.getTime() - now.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

    if (diffDays < 0) return 'Overdue';
    if (diffDays === 0) return 'Today';
    if (diffDays === 1) return 'Tomorrow';
    if (diffDays <= 7) return `In ${diffDays} days`;

    return date.toLocaleDateString();
  }
}
