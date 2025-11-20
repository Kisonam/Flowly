import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { Subject, takeUntil } from 'rxjs';

import { FinanceService } from '../../services/finance.service';
import { FinanceStats, Transaction, Currency, Budget } from '../../models/finance.models';
import {
  IncomeExpenseChartComponent,
  IncomeExpenseData,
  CategoryBreakdownChartComponent,
  CategoryBreakdownData,
  BudgetProgressChartComponent,
  BudgetProgressData
} from '../../../../shared/components/charts';

@Component({
  selector: 'app-finance-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    IncomeExpenseChartComponent,
    CategoryBreakdownChartComponent,
    BudgetProgressChartComponent,
    TranslateModule
  ],
  templateUrl: './finance-dashboard.component.html',
  styleUrl: './finance-dashboard.component.scss'
})
export class FinanceDashboardComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private financeService = inject(FinanceService);
  private fb = inject(FormBuilder);

  stats: FinanceStats | null = null;
  recentTransactions: Transaction[] = [];
  currencies: Currency[] = [];
  budgets: Budget[] = [];

  loading = false;
  loadingTransactions = false;
  loadingBudgets = false;
  errorMessage = '';

  // Chart data
  incomeExpenseData: IncomeExpenseData[] = [];
  expenseCategoryData: CategoryBreakdownData[] = [];
  incomeCategoryData: CategoryBreakdownData[] = [];
  budgetProgressData: BudgetProgressData[] = [];

  // Filter form
  filterForm: FormGroup;

  constructor() {
    const now = new Date();
    const firstDayOfMonth = new Date(now.getFullYear(), now.getMonth(), 1);
    const lastDayOfMonth = new Date(now.getFullYear(), now.getMonth() + 1, 0);

    this.filterForm = this.fb.group({
      periodStart: [this.formatDateForInput(firstDayOfMonth)],
      periodEnd: [this.formatDateForInput(lastDayOfMonth)],
      currencyCode: ['UAH']
    });
  }

  ngOnInit(): void {
    this.loadCurrencies();
    this.loadStats();
    this.loadRecentTransactions();
    this.loadBudgets();

    // Listen to filter changes
    this.filterForm.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.loadStats();
        this.loadRecentTransactions();
        this.loadBudgets();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadCurrencies(): void {
    this.financeService.getCurrencies().subscribe({
      next: (currencies) => {
        this.currencies = currencies;
      },
      error: (err) => {
        console.error('Failed to load currencies:', err);
      }
    });
  }

  private loadStats(): void {
    this.loading = true;
    this.errorMessage = '';

    const formValue = this.filterForm.value;
    const periodStart = formValue.periodStart;
    const periodEnd = formValue.periodEnd;
    const currencyCode = formValue.currencyCode || undefined;

    this.financeService.getStats(periodStart, periodEnd, currencyCode)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (stats) => {
          this.stats = stats;
          this.loading = false;

          // Prepare chart data
          this.prepareChartData();
        },
        error: (err) => {
          console.error('Failed to load stats:', err);
          this.errorMessage = 'Не вдалося завантажити статистику';
          this.loading = false;
        }
      });
  }

  private loadRecentTransactions(): void {
    this.loadingTransactions = true;

    const formValue = this.filterForm.value;
    const filter = {
      currencyCode: formValue.currencyCode || undefined
    };

    this.financeService.getTransactions(filter)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          // Get top 5 most recent transactions
          this.recentTransactions = result.items
            .sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime())
            .slice(0, 5);
          this.loadingTransactions = false;
        },
        error: (err) => {
          console.error('Failed to load transactions:', err);
          this.loadingTransactions = false;
        }
      });
  }

  private loadBudgets(): void {
    this.loadingBudgets = true;

    const formValue = this.filterForm.value;
    const filter = {
      currencyCode: formValue.currencyCode || undefined,
      isArchived: false
    };

    this.financeService.getBudgets(filter)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (budgets) => {
          this.budgets = budgets.filter(b => b.isActive);
          this.loadingBudgets = false;
          this.prepareBudgetChartData();
        },
        error: (err) => {
          console.error('Failed to load budgets:', err);
          this.loadingBudgets = false;
        }
      });
  }

  private prepareChartData(): void {
    if (!this.stats) return;

    // Prepare income vs expense timeline data
    this.incomeExpenseData = this.stats.byMonth.map(m => ({
      label: `${this.getMonthName(m.month)} ${m.year}`,
      income: m.totalIncome,
      expense: m.totalExpense
    }));

    // Prepare expense category breakdown data
    this.expenseCategoryData = this.stats.expenseByCategory.map(c => ({
      categoryName: c.categoryName,
      amount: c.totalAmount
    }));

    // Prepare income category breakdown data
    this.incomeCategoryData = this.stats.incomeByCategory.map(c => ({
      categoryName: c.categoryName,
      amount: c.totalAmount
    }));
  }

  private prepareBudgetChartData(): void {
    this.budgetProgressData = this.budgets.map(b => ({
      title: b.title,
      current: b.currentSpent,
      limit: b.limit
    }));
  }

  private getMonthName(month: number): string {
    const monthNames = ['Січ', 'Лют', 'Бер', 'Кві', 'Тра', 'Чер', 'Лип', 'Сер', 'Вер', 'Жов', 'Лис', 'Гру'];
    return monthNames[month - 1] || '';
  }

  private formatDateForInput(date: Date): string {
    return date.toISOString().split('T')[0];
  }

  getTransactionTypeIcon(type: string): string {
    return type === 'Income' ? '📈' : '📉';
  }

  getTransactionTypeClass(type: string): string {
    return type === 'Income' ? 'income' : 'expense';
  }
}
