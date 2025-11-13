/**
 * Chart Components Usage Examples
 *
 * This file contains example code snippets for using the reusable chart components
 */

import { Component } from '@angular/core';
import {
  IncomeExpenseChartComponent,
  IncomeExpenseData,
  CategoryBreakdownChartComponent,
  CategoryBreakdownData,
  BudgetProgressChartComponent,
  BudgetProgressData
} from './index';

@Component({
  selector: 'app-charts-example',
  standalone: true,
  imports: [
    IncomeExpenseChartComponent,
    CategoryBreakdownChartComponent,
    BudgetProgressChartComponent
  ],
  template: `
    <!-- Income vs Expense Bar Chart -->
    <app-income-expense-chart
      [data]="incomeExpenseData"
      [chartType]="'bar'"
      [title]="'Monthly Income vs Expenses'"
      [height]="'400px'"
    ></app-income-expense-chart>

    <!-- Income vs Expense Line Chart -->
    <app-income-expense-chart
      [data]="incomeExpenseData"
      [chartType]="'line'"
      [title]="'Income Trend'"
      [height]="'300px'"
      [incomeColor]="'rgba(72, 187, 120, 0.6)'"
      [expenseColor]="'rgba(245, 101, 101, 0.6)'"
    ></app-income-expense-chart>

    <!-- Category Breakdown Pie Chart -->
    <app-category-breakdown-chart
      [data]="categoryData"
      [chartType]="'pie'"
      [title]="'Expense Categories'"
      [height]="'350px'"
      [legendPosition]="'bottom'"
    ></app-category-breakdown-chart>

    <!-- Category Breakdown Doughnut Chart -->
    <app-category-breakdown-chart
      [data]="categoryData"
      [chartType]="'doughnut'"
      [title]="'Income Sources'"
      [height]="'350px'"
      [legendPosition]="'right'"
      [showPercentage]="true"
    ></app-category-breakdown-chart>

    <!-- Budget Progress Chart -->
    <app-budget-progress-chart
      [data]="budgetData"
      [title]="'Budget Status'"
      [height]="'500px'"
      [showPercentage]="true"
      [warningThreshold]="75"
      [dangerThreshold]="100"
    ></app-budget-progress-chart>
  `
})
export class ChartsExampleComponent {

  // Example 1: Income vs Expense data
  incomeExpenseData: IncomeExpenseData[] = [
    { label: 'Січ 2024', income: 50000, expense: 35000 },
    { label: 'Лют 2024', income: 55000, expense: 38000 },
    { label: 'Бер 2024', income: 52000, expense: 40000 },
    { label: 'Кві 2024', income: 58000, expense: 36000 },
    { label: 'Тра 2024', income: 60000, expense: 42000 },
    { label: 'Чер 2024', income: 62000, expense: 39000 }
  ];

  // Example 2: Category breakdown data
  categoryData: CategoryBreakdownData[] = [
    { categoryName: 'Продукти', amount: 15000 },
    { categoryName: 'Транспорт', amount: 5000 },
    { categoryName: 'Розваги', amount: 8000 },
    { categoryName: 'Комунальні', amount: 4000 },
    { categoryName: 'Здоров\'я', amount: 3000 },
    { categoryName: 'Одяг', amount: 6000 }
  ];

  // Example 3: Category data with custom colors
  categoryDataWithColors: CategoryBreakdownData[] = [
    { categoryName: 'Зарплата', amount: 50000, color: 'rgba(72, 187, 120, 0.8)' },
    { categoryName: 'Фріланс', amount: 10000, color: 'rgba(54, 162, 235, 0.8)' },
    { categoryName: 'Інвестиції', amount: 5000, color: 'rgba(255, 206, 86, 0.8)' }
  ];

  // Example 4: Budget progress data
  budgetData: BudgetProgressData[] = [
    { title: 'Харчування', current: 12000, limit: 15000 },      // 80% - Warning
    { title: 'Транспорт', current: 4500, limit: 5000 },         // 90% - Warning
    { title: 'Розваги', current: 9500, limit: 8000 },           // 118% - Exceeded
    { title: 'Комунальні', current: 3000, limit: 4000 },        // 75% - Safe
    { title: 'Здоров\'я', current: 1500, limit: 3000 }          // 50% - Safe
  ];

  // Example 5: Empty data (for testing empty states)
  emptyData: IncomeExpenseData[] = [];

  // Example 6: Dynamic data update
  updateChartData(): void {
    // Simulate fetching new data
    this.incomeExpenseData = [
      { label: 'Lip 2024', income: 65000, expense: 41000 },
      { label: 'Сер 2024', income: 63000, expense: 43000 },
      { label: 'Вер 2024', income: 67000, expense: 40000 }
    ];
  }
}

// ============================================================
// TypeScript Examples - Data Preparation
// ============================================================

/**
 * Example: Convert backend stats to chart data
 */
export function convertStatsToChartData(stats: any): IncomeExpenseData[] {
  return stats.byMonth.map((m: any) => ({
    label: `${getMonthName(m.month)} ${m.year}`,
    income: m.totalIncome,
    expense: m.totalExpense
  }));
}

/**
 * Example: Convert category stats to chart data
 */
export function convertCategoriesToChartData(categories: any[]): CategoryBreakdownData[] {
  return categories.map(c => ({
    categoryName: c.categoryName,
    amount: c.totalAmount,
    color: c.color // Optional: use category's predefined color
  }));
}

/**
 * Example: Convert budgets to chart data
 */
export function convertBudgetsToChartData(budgets: any[]): BudgetProgressData[] {
  return budgets
    .filter(b => b.isActive && !b.isArchived)
    .map(b => ({
      title: b.title,
      current: b.currentSpent,
      limit: b.limit
    }));
}

/**
 * Helper function to get month name
 */
function getMonthName(month: number): string {
  const monthNames = ['Січ', 'Лют', 'Бер', 'Кві', 'Тра', 'Чер', 'Лип', 'Сер', 'Вер', 'Жов', 'Лис', 'Гру'];
  return monthNames[month - 1] || '';
}

// ============================================================
// Advanced Usage Examples
// ============================================================

/**
 * Example: Responsive chart heights
 */
export class ResponsiveChartsComponent {
  getChartHeight(): string {
    const width = window.innerWidth;
    if (width < 576) return '250px';      // Mobile
    if (width < 768) return '300px';      // Tablet
    return '400px';                        // Desktop
  }
}

/**
 * Example: Conditional chart rendering
 */
export class ConditionalChartsComponent {
  showIncomeChart = true;
  showExpenseChart = true;
  showBudgetChart = false;

  toggleChart(chartName: string): void {
    switch (chartName) {
      case 'income':
        this.showIncomeChart = !this.showIncomeChart;
        break;
      case 'expense':
        this.showExpenseChart = !this.showExpenseChart;
        break;
      case 'budget':
        this.showBudgetChart = !this.showBudgetChart;
        break;
    }
  }
}

/**
 * Example: Chart with loading state
 */
export class ChartsWithLoadingComponent {
  isLoading = true;
  chartData: IncomeExpenseData[] = [];

  async loadChartData(): Promise<void> {
    this.isLoading = true;
    try {
      // Simulate API call
      const response = await fetch('/api/stats');
      const data = await response.json();
      this.chartData = convertStatsToChartData(data);
    } catch (error) {
      console.error('Failed to load chart data:', error);
    } finally {
      this.isLoading = false;
    }
  }
}

/**
 * Example: Multiple currency support
 */
export class MultiCurrencyChartsComponent {
  currentCurrency = 'UAH';
  chartData: IncomeExpenseData[] = [];

  updateCurrency(currency: string): void {
    this.currentCurrency = currency;
    // Reload chart data with new currency
    this.loadChartData(currency);
  }

  loadChartData(currency: string): void {
    // Fetch and update chart data for the selected currency
  }
}
