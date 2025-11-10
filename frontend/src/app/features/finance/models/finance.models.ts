// Finance feature TypeScript models mirroring backend DTOs

import { Tag } from '../../tasks/models/task.models';

// ============================================
// Enums
// ============================================

export type TransactionType = 'Income' | 'Expense';

// ============================================
// Entities
// ============================================

export interface Category {
  id: string;
  name: string;
  userId?: string | null;
  color?: string | null;
  icon?: string | null;
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
  limitAmount: number;
  currentSpent: number;
  categoryId?: string | null;
  category?: Category | null;
  currencyCode: string;
  periodStart: string | Date;
  periodEnd: string | Date;
  createdAt: string | Date;
  updatedAt: string | Date;
  // Computed properties from backend
  remainingAmount: number;
  progressPercentage: number;
  isExceeded: boolean;
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
  // Computed properties from backend
  progressPercentage: number;
  isCompleted: boolean;
  isOverdue: boolean;
  isDeadlineApproaching: boolean;
}

// ============================================
// Statistics
// ============================================

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
  transactionCount: number;
  incomeByCategory: CategoryStats[];
  expenseByCategory: CategoryStats[];
  byMonth: MonthStats[];
}

// ============================================
// Create / Update DTOs
// ============================================

export interface CreateTransactionRequest {
  title: string;
  description?: string;
  amount: number;
  type: TransactionType;
  date: string; // ISO date - matches backend DTO
  categoryId?: string;
  currencyCode: string;
  tagIds?: string[];
}

export interface UpdateTransactionRequest {
  title: string;
  description?: string;
  amount: number;
  type: TransactionType;
  date: string; // ISO date - matches backend DTO
  categoryId?: string;
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
  limitAmount: number;
  categoryId?: string;
  currencyCode: string;
  periodStart: string; // ISO date
  periodEnd: string; // ISO date
}

export interface UpdateBudgetRequest {
  title: string;
  description?: string;
  limitAmount: number;
  categoryId?: string;
  currencyCode: string;
  periodStart: string; // ISO date
  periodEnd: string; // ISO date
}

export interface CreateGoalRequest {
  title: string;
  description?: string;
  targetAmount: number;
  currentAmount?: number;
  currencyCode: string;
  deadline?: string; // ISO date
}

export interface UpdateGoalRequest {
  title: string;
  description?: string;
  targetAmount: number;
  currencyCode: string;
  deadline?: string; // ISO date
}

export interface UpdateGoalAmountRequest {
  amount: number;
}

// ============================================
// Filtering & Pagination
// ============================================

export interface TransactionFilter {
  search?: string;
  type?: TransactionType;
  categoryId?: string;
  currencyCode?: string;
  tagIds?: string[];
  isArchived?: boolean;
  dateFrom?: string; // ISO date
  dateTo?: string; // ISO date
  page?: number;
  pageSize?: number;
}

export interface BudgetFilter {
  isActive?: boolean;
  categoryId?: string;
  currencyCode?: string;
  dateFrom?: string; // ISO date
  dateTo?: string; // ISO date
}

export interface GoalFilter {
  isCompleted?: boolean;
  isArchived?: boolean;
  currencyCode?: string;
  deadlineFrom?: string; // ISO date
  deadlineTo?: string; // ISO date
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

// ============================================
// Specialized Responses
// ============================================

export interface BudgetOverspentResponse {
  budgetId: string;
  isOverspent: boolean;
}
