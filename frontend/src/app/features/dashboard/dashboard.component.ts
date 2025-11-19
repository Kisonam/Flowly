import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { DashboardService } from './services/dashboard.service';
import { DashboardData } from './models/dashboard.models';
import { FinanceSummaryComponent } from './widgets/finance-summary/finance-summary.component';
import { UpcomingTasksComponent } from './widgets/upcoming-tasks/upcoming-tasks.component';
import { RecentNotesComponent } from './widgets/recent-notes/recent-notes.component';
import { ActivityStatsComponent } from './widgets/activity-stats/activity-stats.component';
import { ThemeService, ThemeMode } from '../../core/services/theme.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    ActivityStatsComponent,
    FinanceSummaryComponent,
    UpcomingTasksComponent,
    RecentNotesComponent
  ],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {
  private dashboardService = inject(DashboardService);
  private router = inject(Router);
  protected themeService = inject(ThemeService);

  dashboardData: DashboardData | null = null;
  loading = true;
  error: string | null = null;
  currentTheme: ThemeMode = 'normal';

  ngOnInit(): void {
    this.loadDashboard();

    // Subscribe to theme changes
    this.themeService.currentTheme$.subscribe(theme => {
      this.currentTheme = theme;
    });
  }

  loadDashboard(): void {
    this.loading = true;
    this.error = null;

    this.dashboardService.getDashboard().subscribe({
      next: (data) => {
        this.dashboardData = data;
        this.loading = false;
      },
      error: (err) => {
        this.error = err.message || 'Failed to load dashboard';
        this.loading = false;
        console.error('Dashboard error:', err);
      }
    });
  }

  // Quick create actions
  createTransaction(): void {
    this.router.navigate(['/finance/transactions/new']);
  }

  createTask(): void {
    this.router.navigate(['/tasks/new']);
  }

  createNote(): void {
    this.router.navigate(['/notes/new']);
  }

  createBudget(): void {
    this.router.navigate(['/finance/budgets/new']);
  }
}
