import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ViewportScroller } from '@angular/common';
import { ActivityStats } from '../../models/dashboard.models';

@Component({
  selector: 'app-activity-stats',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './activity-stats.component.html',
  styleUrls: ['./activity-stats.component.scss']
})
export class ActivityStatsComponent {
  @Input() stats: ActivityStats | null = null;

  private router = inject(Router);
  private viewportScroller = inject(ViewportScroller);

  getProductivityColor(): string {
    if (!this.stats) return '#6b7280';

    const score = this.stats.productivityScore;
    if (score >= 80) return '#10b981'; // Excellent - green
    if (score >= 60) return '#3b82f6'; // High - blue
    if (score >= 40) return '#f59e0b'; // Medium - orange
    return '#ef4444'; // Low - red
  }

  getProductivityIcon(): string {
    if (!this.stats) return '📊';

    const level = this.stats.productivityLevel;
    switch (level) {
      case 'Excellent': return '🚀';
      case 'High': return '⭐';
      case 'Medium': return '📈';
      default: return '📊';
    }
  }

  navigateToTasks(): void {
    // Scroll to upcoming tasks section on the same page
    setTimeout(() => {
      this.viewportScroller.scrollToAnchor('upcoming-tasks');
    }, 100);
  }

  navigateToNotes(): void {
    // Scroll to recent notes section on the same page
    setTimeout(() => {
      this.viewportScroller.scrollToAnchor('recent-notes');
    }, 100);
  }

  navigateToTransactions(): void {
    // Navigate to finance dashboard
    this.router.navigate(['/finance/dashboard']);
  }
}
