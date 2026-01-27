

import { Tag } from '../../tasks/models/task.models';

export type TransactionType = 'Income' | 'Expense';

export interface Currency {
  code: string;
  name: string;
  symbol: string;
}

export interface Category {
  id: string;
  name: string;
  userId?: string | null;
  color?: string | null;
  icon?: string | null;
}

export interface BudgetSummary {
  id: string;
  title: string;
  currencyCode: string;
}

export interface GoalSummary {
  id: string;
  title: string;
  currencyCode: string;
}

export interface Transaction {
  id: string;
  title: string;
  description?: string | null;
  amount: number;
  type: TransactionType;
  date: string | Date;
  categoryId?: string | null;
  category?: Category | null;
  budgetId?: string | null;
  budget?: BudgetSummary | null;
  goalId?: string | null;
  goal?: GoalSummary | null;
  currencyCode: string;
  tags: Tag[];
  isArchived: boolean;
  createdAt: string | Date;
  updatedAt: string | Date;
}

export interface Budget {
  id: string;
  title: string;
  description?: string | null;
  periodStart: string | Date;
  periodEnd: string | Date;
  limit: number;
  currencyCode: string;
  categoryId?: string | null;
  currentSpent: number;
  createdAt: string | Date;
  updatedAt?: string | Date | null;
  isArchived: boolean;
  archivedAt?: string | Date | null;
  
  category?: Category | null;
  
  remainingAmount: number;
  progressPercentage: number;
  isExceeded: boolean;
  isActive: boolean;
  daysRemaining: number;
}

export interface FinancialGoal {
  id: string;
  title: string;
  description?: string | null;
  targetAmount: number;
  currentAmount: number;
  currencyCode: string;
  deadline?: string | Date | null;
  isArchived: boolean;
  createdAt: string | Date;
  updatedAt: string | Date;
  completedAt?: string | Date | null;
  
  progressPercentage: number;
  isCompleted: boolean;
  isOverdue: boolean;
  isDeadlineApproaching: boolean;
}

export interface CategoryStats {
  categoryId?: string | null;
  categoryName: string;
  totalAmount: number;
  transactionCount: number;
  percentage: number;
}

export interface MonthStats {
  year: number;
  month: number;
  totalIncome: number;
  totalExpense: number;
  balance: number;
  transactionCount: number;
}

export interface FinanceStats {
  periodStart: string | Date;
  periodEnd: string | Date;
  currencyCode?: string | null;
  totalIncome: number;
  totalExpense: number;
  balance: number;
  averageIncome: number;
  averageExpense: number;
  totalTransactionCount: number; 
  incomeByCategory: CategoryStats[];
  expenseByCategory: CategoryStats[];
  byMonth: MonthStats[];
}

export interface CreateTransactionRequest {
  title: string;
  description?: string;
  amount: number;
  type: TransactionType;
  date: string; 
  categoryId?: string;
  budgetId?: string;
  goalId?: string;
  currencyCode: string;
  tagIds?: string[];
}

export interface UpdateTransactionRequest {
  title: string;
  description?: string;
  amount: number;
  type: TransactionType;
  date: string; 
  categoryId?: string;
  budgetId?: string;
  goalId?: string;
  currencyCode: string;
  tagIds?: string[];
}

export interface CreateCategoryRequest {
  name: string;
  color?: string;
  icon?: string;
}

export interface UpdateCategoryRequest {
  name: string;
  color?: string;
  icon?: string;
}

export interface CreateBudgetRequest {
  title: string;
  description?: string;
  periodStart: string; 
  periodEnd: string; 
  limit: number;
  currencyCode: string;
  categoryId?: string;
}

export interface UpdateBudgetRequest {
  title: string;
  description?: string;
  periodStart: string; 
  periodEnd: string; 
  limit: number;
  currencyCode: string;
  categoryId?: string;
}

export interface CreateGoalRequest {
  title: string;
  description?: string;
  targetAmount: number;
  currentAmount?: number;
  currencyCode: string;
  deadline?: string; 
}

export interface UpdateGoalRequest {
  title: string;
  description?: string;
  targetAmount: number;
  currencyCode: string;
  deadline?: string; 
}

export interface UpdateGoalAmountRequest {
  amount: number;
}

export interface TransactionFilter {
  search?: string;
  type?: TransactionType;
  categoryId?: string;
  currencyCode?: string;
  tagIds?: string[];
  isArchived?: boolean;
  dateFrom?: string; 
  dateTo?: string; 
  page?: number;
  pageSize?: number;
}

export interface BudgetFilter {
  isArchived?: boolean;
  categoryId?: string;
  currencyCode?: string;
  dateFrom?: string; 
  dateTo?: string; 
}

export interface GoalFilter {
  isCompleted?: boolean;
  isArchived?: boolean;
  currencyCode?: string;
  deadlineFrom?: string; 
  deadlineTo?: string; 
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
}

export interface BudgetOverspentResponse {
  budgetId: string;
  isOverspent: boolean;
}
