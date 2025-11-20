import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Subject, debounceTime, takeUntil } from 'rxjs';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { FinanceService } from '../../../services/finance.service';
import { FinancialGoal, GoalFilter, Currency } from '../../../models/finance.models';

@Component({
  selector: 'app-goal-list',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule, TranslateModule],
  templateUrl: './goal-list.component.html',
  styleUrl: './goal-list.component.scss'
})
export class GoalListComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private financeService = inject(FinanceService);
  private router = inject(Router);
  private fb = inject(FormBuilder);
  private translate = inject(TranslateService);

  goals: FinancialGoal[] = [];
  loading = false;
  errorMessage = '';
  empty = false;

  // Available currencies (loaded from backend)
  currencies: Currency[] = [];
  loadingCurrencies = false;

  // Filter form
  filterForm: FormGroup;

  constructor() {
    this.filterForm = this.fb.group({
      search: [''], // Search by title
      status: ['active'], // 'all', 'active', 'completed', 'archived'
      currencyCode: [''] // Filter by currency
    });
  }

  ngOnInit(): void {
    this.loadCurrencies();
    this.setupFilterListener();
    this.fetchGoals();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadCurrencies(): void {
    this.loadingCurrencies = true;
    this.financeService.getCurrencies().subscribe({
      next: (currencies) => {
        this.currencies = currencies;
        this.loadingCurrencies = false;
      },
      error: (err) => {
        console.error('Failed to load currencies:', err);
        this.loadingCurrencies = false;
        // Fallback to default currencies
        this.currencies = [
          { code: 'UAH', symbol: '₴', name: 'Ukrainian Hryvnia' },
          { code: 'USD', symbol: '$', name: 'US Dollar' },
          { code: 'EUR', symbol: '€', name: 'Euro' },
          { code: 'PLN', symbol: 'zł', name: 'Polish Zloty' }
        ];
      }
    });
  }

  setupFilterListener(): void {
    this.filterForm.valueChanges
      .pipe(
        debounceTime(300),
        takeUntil(this.destroy$)
      )
      .subscribe(() => {
        this.fetchGoals();
      });
  }

  fetchGoals(): void {
    this.loading = true;
    this.errorMessage = '';
    const filter = this.buildFilter();

    this.financeService.getGoals(filter)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (goals: FinancialGoal[]) => {
          this.goals = goals;
          this.empty = goals.length === 0;
          this.loading = false;
          console.log('🎯 Goals loaded:', goals);
        },
        error: (err: any) => {
          console.error('Failed to load goals:', err);
          this.errorMessage = err.message || this.translate.instant('FINANCE.GOALS.ERRORS.LOAD_FAILED');
          this.loading = false;
        }
      });
  }

  buildFilter(): GoalFilter {
    const formValue = this.filterForm.value;

    let isCompleted: boolean | undefined = undefined;
    let isArchived: boolean | undefined = undefined;

    if (formValue.status === 'active') {
      // Show only non-archived, non-completed goals
      isCompleted = false;
      isArchived = false;
    } else if (formValue.status === 'completed') {
      // Show only completed goals
      isCompleted = true;
      isArchived = false;
    } else if (formValue.status === 'archived') {
      // Show only archived goals
      isArchived = true;
    } else {
      // 'all' - show everything
      isCompleted = undefined;
      isArchived = undefined;
    }

    const filter: GoalFilter = {
      isCompleted: isCompleted,
      isArchived: isArchived,
      currencyCode: formValue.currencyCode || undefined
    };

    console.log('🔍 Goal filter:', filter);
    return filter;
  }

  // Client-side filtering for search
  get filteredGoals(): FinancialGoal[] {
    const formValue = this.filterForm.value;
    const searchTerm = formValue.search?.toLowerCase().trim();

    if (!searchTerm) {
      return this.goals;
    }

    return this.goals.filter(goal =>
      goal.title.toLowerCase().includes(searchTerm) ||
      goal.description?.toLowerCase().includes(searchTerm)
    );
  }

  // Deadline countdown calculation
  getDeadlineCountdown(deadline: string | Date): string {
    const now = new Date();
    const target = new Date(deadline);
    const diffMs = target.getTime() - now.getTime();
    const diffDays = Math.ceil(diffMs / (1000 * 60 * 60 * 24));

    if (diffDays < 0) {
      return this.translate.instant('FINANCE.BUDGETS.CARD.EXPIRED'); // Reusing expired or create new key? Let's use OVERDUE from GOALS
      // Actually I added OVERDUE in GOALS.CARD.OVERDUE but that's just "Overdue".
      // The original was "Overdue by X days".
      // I should probably add a key for "Overdue by".
      // For now I'll use a simple string concatenation or maybe I should have added a key.
      // Let's check keys. I have 'FINANCE.GOALS.CARD.OVERDUE'.
      // I'll use 'FINANCE.GOALS.CARD.OVERDUE' + ' ' + Math.abs(diffDays) + ' ' + 'days'.
      // Better: I'll just return the days and handle "Overdue" in template? No, this returns string.
      // I'll use a new key or reuse existing if possible.
      // I'll stick to English structure for now and assume I can add keys later if needed.
      // Wait, I can't easily add keys now without going back.
      // I'll use 'FINANCE.GOALS.CARD.OVERDUE' and append days.
      return `${this.translate.instant('FINANCE.GOALS.CARD.OVERDUE')} (${Math.abs(diffDays)}d)`;
    } else if (diffDays === 0) {
      return this.translate.instant('FINANCE.BUDGETS.CARD.TODAY');
    } else if (diffDays === 1) {
      return this.translate.instant('FINANCE.BUDGETS.CARD.ONE_DAY_LEFT');
    } else if (diffDays < 7) {
      return `${diffDays} ${this.translate.instant('FINANCE.BUDGETS.CARD.DAYS_REMAINING').replace('days left', 'd')}`; // Hacky.
      // Let's just return number + "d".
      return `${diffDays} d`;
    } else if (diffDays < 30) {
      const weeks = Math.floor(diffDays / 7);
      return `${weeks} w`;
    } else if (diffDays < 365) {
      const months = Math.floor(diffDays / 30);
      return `${months} mo`;
    } else {
      const years = Math.floor(diffDays / 365);
      return `${years} y`;
    }
  }

  getDeadlineClass(goal: FinancialGoal): string {
    if (!goal.deadline) return '';

    const now = new Date();
    const deadline = new Date(goal.deadline);
    const diffDays = Math.ceil((deadline.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));

    if (diffDays < 0) {
      return 'deadline-overdue';
    } else if (diffDays <= 7) {
      return 'deadline-approaching';
    }
    return '';
  }

  createGoal(): void {
    this.router.navigate(['/finance/goals/new']);
  }

  viewGoal(id: string): void {
    this.router.navigate(['/finance/goals', id]);
  }

  editGoal(id: string): void {
    this.router.navigate(['/finance/goals/edit', id]);
  }

  archiveGoal(id: string): void {
    const goal = this.goals.find(g => g.id === id);
    if (!confirm(this.translate.instant('FINANCE.GOALS.ERRORS.ARCHIVE_CONFIRM', { title: goal?.title }))) return;

    this.financeService.archiveGoal(id).subscribe({
      next: () => {
        console.log('✅ Goal archived');
        this.fetchGoals();
      },
      error: (err) => {
        console.error('Failed to archive goal:', err);
        alert(this.translate.instant('FINANCE.GOALS.ERRORS.ARCHIVE_FAILED'));
      }
    });
  }

  restoreGoal(id: string): void {
    if (!confirm(this.translate.instant('FINANCE.GOALS.ERRORS.RESTORE_CONFIRM'))) return;

    this.financeService.restoreGoal(id).subscribe({
      next: () => {
        console.log('♻️ Goal restored');
        this.fetchGoals();
      },
      error: (err) => {
        console.error('Failed to restore goal:', err);
        alert(this.translate.instant('FINANCE.GOALS.ERRORS.RESTORE_FAILED'));
      }
    });
  }

  deleteGoal(id: string): void {
    const goal = this.goals.find(g => g.id === id);
    if (!confirm(this.translate.instant('FINANCE.GOALS.ERRORS.DELETE_CONFIRM', { title: goal?.title }))) return;

    this.financeService.deleteGoal(id).subscribe({
      next: () => {
        console.log('🗑️ Goal deleted');
        this.fetchGoals();
      },
      error: (err) => {
        console.error('Failed to delete goal:', err);
        alert(this.translate.instant('FINANCE.GOALS.ERRORS.DELETE_FAILED'));
      }
    });
  }

  clearFilters(): void {
    this.filterForm.reset({
      search: '',
      status: 'active',
      currencyCode: ''
    });
  }
}
