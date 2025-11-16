import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Subject, debounceTime, takeUntil } from 'rxjs';
import { ThemeService } from '../../../../../core/services/theme.service';

import { FinanceService } from '../../../services/finance.service';
import { Budget, BudgetFilter, Currency } from '../../../models/finance.models';

@Component({
  selector: 'app-budget-list',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: './budget-list.component.html',
  styleUrls: ['./budget-list.component.scss']
})
export class BudgetListComponent implements OnInit, OnDestroy {
  private financeService = inject(FinanceService);
  private router = inject(Router);
  private fb = inject(FormBuilder);
  private themeService = inject(ThemeService);
  private destroy$ = new Subject<void>();

  budgets: Budget[] = [];
  loading = false;
  errorMessage = '';
  empty = false;

  // Available currencies for filter (loaded from backend)
  currencies: Currency[] = [];
  loadingCurrencies = false;

  // Filter form
  filterForm: FormGroup;

  constructor() {
    this.filterForm = this.fb.group({
      search: [''], // Search by title
      status: ['active'], // 'all', 'active', 'archived'
      currencyCode: [''], // Filter by currency
      minAmount: [''], // Minimum limit amount
      maxAmount: [''], // Maximum limit amount
      periodStart: [''], // Start date
      periodEnd: [''] // End date
    });
  }
  ngOnInit(): void {
    this.loadCurrencies();
    this.setupFilterListener();
    this.fetchBudgets();
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
        this.fetchBudgets();
      });
  }

  fetchBudgets(): void {
    this.loading = true;
    this.errorMessage = '';
    const filter = this.buildFilter();

    this.financeService.getBudgets(filter)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (budgets: Budget[]) => {
          this.budgets = budgets;
          this.empty = budgets.length === 0;
          this.loading = false;
          console.log('💼 Budgets loaded:', budgets);
        },
        error: (err: any) => {
          console.error('❌ Failed to fetch budgets', err);
          this.errorMessage = err.message || 'Failed to fetch budgets';
          this.loading = false;
        }
      });
  }

  buildFilter(): BudgetFilter {
    const formValue = this.filterForm.value;

    let isArchived: boolean | undefined = undefined;

    if (formValue.status === 'active') {
      // Show only non-archived budgets (regardless of period)
      isArchived = false;
    } else if (formValue.status === 'archived') {
      // Show only archived budgets
      isArchived = true;
    } else {
      // 'all' - show everything
      isArchived = undefined;
    }

    const filter: BudgetFilter = {
      isArchived: isArchived,
      currencyCode: formValue.currencyCode || undefined,
      dateFrom: formValue.periodStart || undefined,
      dateTo: formValue.periodEnd || undefined
    };

    console.log('🔍 Budget filter:', filter);
    return filter;
  }

  // Client-side filtering for search and amount
  get filteredBudgets(): Budget[] {
    let filtered = [...this.budgets];
    const formValue = this.filterForm.value;

    // Search by title, description, or category name
    if (formValue.search) {
      const search = formValue.search.toLowerCase();
      filtered = filtered.filter(b =>
        b.title.toLowerCase().includes(search) ||
        b.description?.toLowerCase().includes(search) ||
        b.category?.name?.toLowerCase().includes(search)
      );
    }

    // Filter by min amount
    if (formValue.minAmount) {
      const min = Number(formValue.minAmount);
      filtered = filtered.filter(b => b.limit >= min);
    }

    // Filter by max amount
    if (formValue.maxAmount) {
      const max = Number(formValue.maxAmount);
      filtered = filtered.filter(b => b.limit <= max);
    }

    return filtered;
  }  // Actions
  viewBudget(budget: Budget): void {
    this.router.navigate(['/finance/budgets', budget.id]);
  }

  editBudget(budget: Budget): void {
    this.router.navigate(['/finance/budgets', budget.id, 'edit']);
  }

  deleteBudget(budget: Budget): void {
    const action = budget.isArchived ? 'delete permanently' : 'archive';
    if (!confirm(`Are you sure you want to ${action} budget "${budget.title}"?`)) return;

    const request = budget.isArchived
      ? this.financeService.deleteBudget(budget.id)
      : this.financeService.archiveBudget(budget.id);

    request
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          console.log(budget.isArchived ? '✅ Budget deleted permanently' : '📦 Budget archived');
          this.fetchBudgets();
        },
        error: (err: any) => {
          console.error('❌ Failed to delete/archive budget', err);
          alert('Failed to process budget: ' + err.message);
        }
      });
  }

  restoreBudget(budget: Budget): void {
    this.financeService.restoreBudget(budget.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          console.log('♻️ Budget restored');
          this.fetchBudgets();
        },
        error: (err: any) => {
          console.error('❌ Failed to restore budget', err);
          alert('Failed to restore budget: ' + err.message);
        }
      });
  }

  // Helpers
  getProgressPercentage(budget: Budget): number {
    return budget.progressPercentage;
  }

  isOverspent(budget: Budget): boolean {
    return budget.isExceeded;
  }

  getProgressColor(budget: Budget): string {
    const dangerColor = this.themeService.getCssVarValue('--danger', '#ef4444');
    const warningColor = this.themeService.getCssVarValue('--warning', '#f59e0b');
    const successColor = this.themeService.getCssVarValue('--success', '#10b981');

    if (budget.isExceeded) return dangerColor;
    if (budget.progressPercentage >= 80) return warningColor;
    return successColor;
  }

  getCategoryColor(category: any): string {
    // In low-stimulus mode, always use theme variable
    const currentTheme = this.themeService.getCurrentTheme();
    if (currentTheme === 'low-stimulus') {
      return this.themeService.getCssVarValue('--secondary-light', '#d1d5db');
    }
    // In normal mode, use category color or fallback
    if (category?.color) {
      return category.color;
    }
    return this.themeService.getCssVarValue('--secondary-light', '#d1d5db');
  }

  formatCurrency(amount: number, currencyCode: string): string {
    try {
      return new Intl.NumberFormat('uk-UA', {
        style: 'currency',
        currency: currencyCode
      }).format(amount);
    } catch {
      return `${amount} ${currencyCode}`;
    }
  }

  getDaysRemaining(budget: Budget): string {
    if (budget.daysRemaining < 0) return 'Expired';
    if (budget.daysRemaining === 0) return 'Today';
    if (budget.daysRemaining === 1) return '1 day left';
    return `${budget.daysRemaining} days left`;
  }

  formatDate(date: string | Date | undefined): string {
    if (!date) return '—';
    try {
      const d = new Date(date);
      if (isNaN(d.getTime())) return '—';
      return new Intl.DateTimeFormat('uk-UA', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
      }).format(d);
    } catch {
      return '—';
    }
  }

  clearFilters(): void {
    this.filterForm.reset({
      search: '',
      status: 'active',
      currencyCode: '',
      minAmount: '',
      maxAmount: '',
      periodStart: '',
      periodEnd: ''
    });
  }
}
