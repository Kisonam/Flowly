export interface DashboardData {
  activityStats: ActivityStats;
  financeStats: FinanceStats;
  multiCurrencyFinanceStats: MultiCurrencyFinanceStats;
  upcomingTasks: UpcomingTask[];
  recentNotes: RecentNote[];
}

export interface ActivityStats {
  activeTasksCount: number;
  completedTasksCount: number;
  notesCount: number;
  transactionsCount: number;
  productivityScore: number;
  productivityLevel: string;
  productivityBreakdown: ProductivityBreakdown;
}

export interface ProductivityBreakdown {
  taskCompletionRate: number;
  notesActivityScore: number;
  financialTrackingScore: number;
  tasksCreatedThisMonth: number;
  tasksCompletedThisMonth: number;
  notesCreatedThisMonth: number;
  transactionsThisMonth: number;
}

export interface FinanceStats {
  totalIncome: number;
  totalExpense: number;
  netAmount: number;
  currencyCode: string;
  periodStart: string;
  periodEnd: string;
  incomeByCategory: CategoryStats[];
  expenseByCategory: CategoryStats[];
  byMonth: MonthStats[];
  averageDailyIncome: number;
  averageDailyExpense: number;
  totalTransactionCount: number;
}

export interface CategoryStats {
  categoryId?: string;
  categoryName: string;
  totalAmount: number;
  transactionCount: number;
  percentage: number;
}

export interface MonthStats {
  year: number;
  month: number;
  monthName: string;
  totalIncome: number;
  totalExpense: number;
  netAmount: number;
  transactionCount: number;
}

export interface UpcomingTask {
  id: string;
  title: string;
  dueDate?: string;
  status: TaskStatus;
  priority: TaskPriority;
  color?: string;
  isOverdue: boolean;
}

export type TaskStatus = 'Todo' | 'InProgress' | 'Done';
export type TaskPriority = 'Low' | 'Medium' | 'High';

export interface RecentNote {
  id: string;
  title: string;
  createdAt: string;
  updatedAt: string;
}

export interface MultiCurrencyFinanceStats {
  periodStart: string;
  periodEnd: string;
  totalTransactionCount: number;
  byCurrency: CurrencyStats[];
  byMonth: MonthStats[];
  availableCurrencies: string[];
}

export interface CurrencyStats {
  currencyCode: string;
  totalIncome: number;
  totalExpense: number;
  netAmount: number;
  transactionCount: number;
  incomeByCategory: CategoryStats[];
  expenseByCategory: CategoryStats[];
}
