import { Component, OnInit, OnDestroy, ViewChild, ElementRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { Chart, ChartConfiguration, registerables } from 'chart.js';

import { FinanceService } from '../../services/finance.service';
import { FinanceStats, Transaction, Currency } from '../../models/finance.models';

// Register Chart.js components
Chart.register(...registerables);

@Component({
  selector: 'app-finance-dashboard',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './finance-dashboard.component.html',
  styleUrl: './finance-dashboard.component.scss'
})
export class FinanceDashboardComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private financeService = inject(FinanceService);
  private fb = inject(FormBuilder);

  @ViewChild('incomeExpenseChart') incomeExpenseChartRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('expensePieChart') expensePieChartRef!: ElementRef<HTMLCanvasElement>;

  stats: FinanceStats | null = null;
  recentTransactions: Transaction[] = [];
  currencies: Currency[] = [];

  loading = false;
  loadingTransactions = false;
  errorMessage = '';

  // Charts
  incomeExpenseChart?: Chart;
  expensePieChart?: Chart;

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

    // Listen to filter changes
    this.filterForm.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.loadStats();
        this.loadRecentTransactions();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.destroyCharts();
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

          // Delay chart creation to ensure DOM is ready
          setTimeout(() => {
            this.createCharts();
          }, 100);
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

  private createCharts(): void {
    if (!this.stats) return;

    this.destroyCharts();
    this.createIncomeExpenseChart();
    this.createExpensePieChart();
  }

  private createIncomeExpenseChart(): void {
    if (!this.stats || !this.incomeExpenseChartRef) return;

    const ctx = this.incomeExpenseChartRef.nativeElement.getContext('2d');
    if (!ctx) return;

    const labels = this.stats.byMonth.map(m => `${this.getMonthName(m.month)} ${m.year}`);
    const incomeData = this.stats.byMonth.map(m => m.totalIncome);
    const expenseData = this.stats.byMonth.map(m => m.totalExpense);

    const config: ChartConfiguration = {
      type: 'bar',
      data: {
        labels: labels,
        datasets: [
          {
            label: 'Дохід',
            data: incomeData,
            backgroundColor: 'rgba(72, 187, 120, 0.6)',
            borderColor: 'rgba(72, 187, 120, 1)',
            borderWidth: 1
          },
          {
            label: 'Витрати',
            data: expenseData,
            backgroundColor: 'rgba(245, 101, 101, 0.6)',
            borderColor: 'rgba(245, 101, 101, 1)',
            borderWidth: 1
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            position: 'top',
          },
          title: {
            display: true,
            text: 'Доходи vs Витрати'
          }
        },
        scales: {
          y: {
            beginAtZero: true
          }
        }
      }
    };

    this.incomeExpenseChart = new Chart(ctx, config);
  }

  private createExpensePieChart(): void {
    if (!this.stats || !this.expensePieChartRef || !this.stats.expenseByCategory.length) return;

    const ctx = this.expensePieChartRef.nativeElement.getContext('2d');
    if (!ctx) return;

    const labels = this.stats.expenseByCategory.map(c => c.categoryName);
    const data = this.stats.expenseByCategory.map(c => c.totalAmount);
    const colors = this.generateColors(this.stats.expenseByCategory.length);

    const config: ChartConfiguration = {
      type: 'pie',
      data: {
        labels: labels,
        datasets: [{
          data: data,
          backgroundColor: colors,
          borderWidth: 2,
          borderColor: '#fff'
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            position: 'right',
          },
          title: {
            display: true,
            text: 'Витрати по категоріях'
          },
          tooltip: {
            callbacks: {
              label: (context) => {
                const label = context.label || '';
                const value = context.parsed || 0;
                const total = (context.dataset.data as number[]).reduce((a, b) => a + b, 0);
                const percentage = ((value / total) * 100).toFixed(1);
                return `${label}: ${value.toFixed(2)} (${percentage}%)`;
              }
            }
          }
        }
      }
    };

    this.expensePieChart = new Chart(ctx, config);
  }

  private destroyCharts(): void {
    if (this.incomeExpenseChart) {
      this.incomeExpenseChart.destroy();
      this.incomeExpenseChart = undefined;
    }
    if (this.expensePieChart) {
      this.expensePieChart.destroy();
      this.expensePieChart = undefined;
    }
  }

  private generateColors(count: number): string[] {
    const colors = [
      'rgba(255, 99, 132, 0.6)',
      'rgba(54, 162, 235, 0.6)',
      'rgba(255, 206, 86, 0.6)',
      'rgba(75, 192, 192, 0.6)',
      'rgba(153, 102, 255, 0.6)',
      'rgba(255, 159, 64, 0.6)',
      'rgba(199, 199, 199, 0.6)',
      'rgba(83, 102, 255, 0.6)',
      'rgba(255, 99, 255, 0.6)',
      'rgba(99, 255, 132, 0.6)'
    ];
    return colors.slice(0, count);
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
