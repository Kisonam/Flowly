

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

  incomeExpenseData: IncomeExpenseData[] = [
    { label: 'Січ 2024', income: 50000, expense: 35000 },
    { label: 'Лют 2024', income: 55000, expense: 38000 },
    { label: 'Бер 2024', income: 52000, expense: 40000 },
    { label: 'Кві 2024', income: 58000, expense: 36000 },
    { label: 'Тра 2024', income: 60000, expense: 42000 },
    { label: 'Чер 2024', income: 62000, expense: 39000 }
  ];

  categoryData: CategoryBreakdownData[] = [
    { categoryName: 'Продукти', amount: 15000 },
    { categoryName: 'Транспорт', amount: 5000 },
    { categoryName: 'Розваги', amount: 8000 },
    { categoryName: 'Комунальні', amount: 4000 },
    { categoryName: 'Здоров\'я', amount: 3000 },
    { categoryName: 'Одяг', amount: 6000 }
  ];

  categoryDataWithColors: CategoryBreakdownData[] = [
    { categoryName: 'Зарплата', amount: 50000, color: 'rgba(72, 187, 120, 0.8)' },
    { categoryName: 'Фріланс', amount: 10000, color: 'rgba(54, 162, 235, 0.8)' },
    { categoryName: 'Інвестиції', amount: 5000, color: 'rgba(255, 206, 86, 0.8)' }
  ];

  budgetData: BudgetProgressData[] = [
    { title: 'Харчування', current: 12000, limit: 15000 },      
    { title: 'Транспорт', current: 4500, limit: 5000 },         
    { title: 'Розваги', current: 9500, limit: 8000 },           
    { title: 'Комунальні', current: 3000, limit: 4000 },        
    { title: 'Здоров\'я', current: 1500, limit: 3000 }          
  ];

  emptyData: IncomeExpenseData[] = [];

  updateChartData(): void {
    
    this.incomeExpenseData = [
      { label: 'Lip 2024', income: 65000, expense: 41000 },
      { label: 'Сер 2024', income: 63000, expense: 43000 },
      { label: 'Вер 2024', income: 67000, expense: 40000 }
    ];
  }
}

export function convertStatsToChartData(stats: any): IncomeExpenseData[] {
  return stats.byMonth.map((m: any) => ({
    label: `${getMonthName(m.month)} ${m.year}`,
    income: m.totalIncome,
    expense: m.totalExpense
  }));
}

export function convertCategoriesToChartData(categories: any[]): CategoryBreakdownData[] {
  return categories.map(c => ({
    categoryName: c.categoryName,
    amount: c.totalAmount,
    color: c.color 
  }));
}

export function convertBudgetsToChartData(budgets: any[]): BudgetProgressData[] {
  return budgets
    .filter(b => b.isActive && !b.isArchived)
    .map(b => ({
      title: b.title,
      current: b.currentSpent,
      limit: b.limit
    }));
}

function getMonthName(month: number): string {
  const monthNames = ['Січ', 'Лют', 'Бер', 'Кві', 'Тра', 'Чер', 'Лип', 'Сер', 'Вер', 'Жов', 'Лис', 'Гру'];
  return monthNames[month - 1] || '';
}

export class ResponsiveChartsComponent {
  getChartHeight(): string {
    const width = window.innerWidth;
    if (width < 576) return '250px';      
    if (width < 768) return '300px';      
    return '400px';                        
  }
}

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

export class ChartsWithLoadingComponent {
  isLoading = true;
  chartData: IncomeExpenseData[] = [];

  async loadChartData(): Promise<void> {
    this.isLoading = true;
    try {
      
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

export class MultiCurrencyChartsComponent {
  currentCurrency = 'UAH';
  chartData: IncomeExpenseData[] = [];

  updateCurrency(currency: string): void {
    this.currentCurrency = currency;
    
    this.loadChartData(currency);
  }

  loadChartData(currency: string): void {
    
  }
}
