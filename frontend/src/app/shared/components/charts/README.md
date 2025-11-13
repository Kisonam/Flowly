# Chart Components Documentation

## Overview
This folder contains reusable chart components built with Chart.js for displaying financial data in the Flowly application.

## Components

### 1. IncomeExpenseChartComponent
**Location:** `income-expense-chart/income-expense-chart.component.ts`

**Purpose:** Display income vs expense data over time using line or bar charts.

**Inputs:**
- `data: IncomeExpenseData[]` - Array of data points with label, income, and expense values
- `chartType: 'bar' | 'line'` - Type of chart (default: 'bar')
- `title: string` - Chart title (default: 'Доходи vs Витрати')
- `height: string` - Chart height (default: '300px')
- `showLegend: boolean` - Show/hide legend (default: true)
- `showTitle: boolean` - Show/hide title (default: true)
- `responsive: boolean` - Enable responsive behavior (default: true)
- `incomeLabel: string` - Income dataset label (default: 'Дохід')
- `expenseLabel: string` - Expense dataset label (default: 'Витрати')
- `incomeColor: string` - Income bar/line color (default: 'rgba(72, 187, 120, 0.6)')
- `expenseColor: string` - Expense bar/line color (default: 'rgba(245, 101, 101, 0.6)')

**Example:**
```html
<app-income-expense-chart
  [data]="incomeExpenseData"
  [chartType]="'bar'"
  [title]="'Доходи vs Витрати'"
  [height]="'350px'"
></app-income-expense-chart>
```

**Data Interface:**
```typescript
export interface IncomeExpenseData {
  label: string;      // Time period label (e.g., "Січ 2024")
  income: number;     // Income amount for this period
  expense: number;    // Expense amount for this period
}
```

---

### 2. CategoryBreakdownChartComponent
**Location:** `category-breakdown-chart/category-breakdown-chart.component.ts`

**Purpose:** Display category-wise breakdown of financial data using pie or doughnut charts.

**Inputs:**
- `data: CategoryBreakdownData[]` - Array of category data with names and amounts
- `chartType: 'pie' | 'doughnut'` - Type of chart (default: 'doughnut')
- `title: string` - Chart title (default: 'Розподіл по категоріях')
- `height: string` - Chart height (default: '300px')
- `showLegend: boolean` - Show/hide legend (default: true)
- `showTitle: boolean` - Show/hide title (default: true)
- `responsive: boolean` - Enable responsive behavior (default: true)
- `legendPosition: 'top' | 'bottom' | 'left' | 'right'` - Legend position (default: 'right')
- `showPercentage: boolean` - Show percentage in legend labels (default: true)

**Example:**
```html
<app-category-breakdown-chart
  [data]="expenseCategoryData"
  [chartType]="'doughnut'"
  [title]="'Витрати по категоріях'"
  [height]="'350px'"
  [legendPosition]="'right'"
></app-category-breakdown-chart>
```

**Data Interface:**
```typescript
export interface CategoryBreakdownData {
  categoryName: string;  // Name of the category
  amount: number;        // Total amount for this category
  color?: string;        // Optional custom color (auto-generated if not provided)
}
```

---

### 3. BudgetProgressChartComponent
**Location:** `budget-progress-chart/budget-progress-chart.component.ts`

**Purpose:** Display budget progress using horizontal bar charts with current spending vs limits.

**Inputs:**
- `data: BudgetProgressData[]` - Array of budget data with title, current, and limit values
- `title: string` - Chart title (default: 'Прогрес бюджетів')
- `height: string` - Chart height (default: '300px')
- `showLegend: boolean` - Show/hide legend (default: true)
- `showTitle: boolean` - Show/hide title (default: true)
- `responsive: boolean` - Enable responsive behavior (default: true)
- `showPercentage: boolean` - Show detailed percentage info below chart (default: true)
- `warningThreshold: number` - Percentage to trigger warning color (default: 80)
- `dangerThreshold: number` - Percentage to trigger danger color (default: 100)

**Example:**
```html
<app-budget-progress-chart
  [data]="budgetProgressData"
  [title]="'Прогрес бюджетів'"
  [height]="'400px'"
  [showPercentage]="true"
></app-budget-progress-chart>
```

**Data Interface:**
```typescript
export interface BudgetProgressData {
  title: string;    // Budget title
  current: number;  // Current spending amount
  limit: number;    // Budget limit
  color?: string;   // Optional custom color (auto-generated based on progress if not provided)
}
```

**Color Logic:**
- Green: Progress < 80% (safe)
- Yellow/Orange: Progress >= 80% and < 100% (warning)
- Red: Progress >= 100% (danger/exceeded)

---

## Features

### Responsive Design
All chart components are fully responsive and adapt to different screen sizes:
- Desktop: Full-featured charts with all elements visible
- Tablet: Optimized layout with adjusted font sizes
- Mobile: Compact view with essential information

### Empty State Handling
All components gracefully handle empty data by displaying a "Немає даних для відображення" message.

### Dynamic Updates
Charts automatically update when input data changes using Angular's `OnChanges` lifecycle hook.

### Customization
Each component provides extensive customization options through `@Input()` properties, allowing for flexible usage across different parts of the application.

## Usage in Finance Dashboard

The chart components are currently integrated into the Finance Dashboard (`features/finance/components/finance-dashboard`) to display:

1. **Income vs Expense Timeline** - Bar chart showing monthly trends
2. **Expense Category Breakdown** - Doughnut chart showing expense distribution
3. **Income Category Breakdown** - Doughnut chart showing income distribution
4. **Budget Progress** - Horizontal bar chart showing budget utilization

## Future Use Cases

These reusable components can be easily integrated into:
- Home page dashboard (main statistics overview)
- Individual budget detail pages
- Financial goal tracking pages
- Transaction analytics pages
- Export/reporting features

## Technical Details

### Dependencies
- Chart.js v4.5.1
- Angular v19.2.0
- TypeScript

### Performance Considerations
- Charts are destroyed and recreated only when necessary
- Uses `AfterViewInit` to ensure DOM is ready before chart creation
- Implements proper cleanup in `ngOnDestroy` to prevent memory leaks

### Browser Compatibility
Supports all modern browsers that are compatible with Chart.js 4.x and Angular 19.

## Maintenance

### Adding New Chart Types
To add a new chart type:
1. Create a new component folder under `/shared/components/charts/`
2. Implement the component with Chart.js
3. Export from `index.ts`
4. Follow the existing pattern for consistency

### Updating Chart.js
When updating Chart.js, ensure all components are tested as the API may change between major versions.
