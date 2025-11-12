import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, throwError, map, catchError, tap } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  Transaction,
  Category,
  Currency,
  Budget,
  FinancialGoal,
  FinanceStats,
  CreateTransactionRequest,
  UpdateTransactionRequest,
  CreateCategoryRequest,
  UpdateCategoryRequest,
  CreateBudgetRequest,
  UpdateBudgetRequest,
  CreateGoalRequest,
  UpdateGoalRequest,
  UpdateGoalAmountRequest,
  TransactionFilter,
  BudgetFilter,
  GoalFilter,
  PaginatedResult,
  BudgetOverspentResponse
} from '../models/finance.models';

@Injectable({ providedIn: 'root' })
export class FinanceService {
  private http = inject(HttpClient);
  private readonly BASE_URL = `${environment.apiUrl}/finance`;
  private readonly CURRENCIES_URL = `${environment.apiUrl}/currencies`;

  // ============================================
  // Transactions
  // ============================================

  /** Get transactions with filtering + pagination */
  getTransactions(filter?: TransactionFilter): Observable<PaginatedResult<Transaction>> {
    let params = new HttpParams();

    if (filter) {
      if (filter.search) params = params.set('search', filter.search);
      if (filter.type) params = params.set('type', filter.type);
      if (filter.categoryId) params = params.set('categoryId', filter.categoryId);
      if (filter.currencyCode) params = params.set('currencyCode', filter.currencyCode);
      if (filter.tagIds?.length) params = params.set('tagIds', filter.tagIds.join(','));
      if (filter.isArchived !== undefined) params = params.set('isArchived', String(filter.isArchived));
      if (filter.dateFrom) params = params.set('dateFrom', this.toIsoDate(filter.dateFrom));
      if (filter.dateTo) params = params.set('dateTo', this.toIsoDate(filter.dateTo));
      if (filter.page) params = params.set('page', String(filter.page));
      if (filter.pageSize) params = params.set('pageSize', String(filter.pageSize));
    }

    return this.http.get<PaginatedResult<Transaction>>(`${this.BASE_URL}/transactions`, { params }).pipe(
      map(result => this.convertTransactionPagedDates(result)),
      tap(result => console.log('💰 Transactions fetched:', result)),
      catchError(this.handleError)
    );
  }

  /** Get single transaction */
  getTransaction(id: string): Observable<Transaction> {
    return this.http.get<Transaction>(`${this.BASE_URL}/transactions/${id}`).pipe(
      map(tx => this.convertTransactionDates(tx)),
      tap(tx => console.log('💰 Transaction fetched:', tx)),
      catchError(this.handleError)
    );
  }

  /** Create transaction */
  createTransaction(dto: CreateTransactionRequest): Observable<Transaction> {
    console.log('📤 FinanceService.createTransaction:', dto);
    return this.http.post<Transaction>(`${this.BASE_URL}/transactions`, dto).pipe(
      map(tx => this.convertTransactionDates(tx)),
      tap(tx => console.log('✅ Transaction created:', tx)),
      catchError(this.handleError)
    );
  }

  /** Update transaction */
  updateTransaction(id: string, dto: UpdateTransactionRequest): Observable<Transaction> {
    return this.http.put<Transaction>(`${this.BASE_URL}/transactions/${id}`, dto).pipe(
      map(tx => this.convertTransactionDates(tx)),
      tap(tx => console.log('✅ Transaction updated:', tx)),
      catchError(this.handleError)
    );
  }

  /** Archive transaction */
  archiveTransaction(id: string): Observable<void> {
    return this.http.post<void>(`${this.BASE_URL}/transactions/${id}/archive`, {}).pipe(
      tap(() => console.log('✅ Transaction archived:', id)),
      catchError(this.handleError)
    );
  }

  /** Restore archived transaction */
  restoreTransaction(id: string): Observable<void> {
    return this.http.post<void>(`${this.BASE_URL}/transactions/${id}/restore`, {}).pipe(
      tap(() => console.log('✅ Transaction restored:', id)),
      catchError(this.handleError)
    );
  }

  // ============================================
  // Categories
  // ============================================

  /** Get all categories (user + system) */
  getCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(`${this.BASE_URL}/categories`).pipe(
      tap(list => console.log('📂 Categories fetched:', list)),
      catchError(this.handleError)
    );
  }

  /** Get single category */
  getCategory(id: string): Observable<Category> {
    return this.http.get<Category>(`${this.BASE_URL}/categories/${id}`).pipe(
      tap(cat => console.log('📂 Category fetched:', cat)),
      catchError(this.handleError)
    );
  }

  /** Create category */
  createCategory(dto: CreateCategoryRequest): Observable<Category> {
    return this.http.post<Category>(`${this.BASE_URL}/categories`, dto).pipe(
      tap(cat => console.log('✅ Category created:', cat)),
      catchError(this.handleError)
    );
  }

  /** Update category */
  updateCategory(id: string, dto: UpdateCategoryRequest): Observable<Category> {
    return this.http.put<Category>(`${this.BASE_URL}/categories/${id}`, dto).pipe(
      tap(cat => console.log('✅ Category updated:', cat)),
      catchError(this.handleError)
    );
  }

  /** Delete category */
  deleteCategory(id: string): Observable<void> {
    return this.http.delete<void>(`${this.BASE_URL}/categories/${id}`).pipe(
      tap(() => console.log('✅ Category deleted:', id)),
      catchError(this.handleError)
    );
  }

  // ============================================
  // Budgets
  // ============================================

  /** Get budgets with filtering */
  getBudgets(filter?: BudgetFilter): Observable<Budget[]> {
    let params = new HttpParams();

    if (filter) {
      if (filter.isArchived !== undefined) params = params.set('isArchived', String(filter.isArchived));
      if (filter.categoryId) params = params.set('categoryId', filter.categoryId);
      if (filter.currencyCode) params = params.set('currencyCode', filter.currencyCode);
      if (filter.dateFrom) params = params.set('dateFrom', this.toIsoDate(filter.dateFrom));
      if (filter.dateTo) params = params.set('dateTo', this.toIsoDate(filter.dateTo));
    }

    return this.http.get<Budget[]>(`${this.BASE_URL}/budgets`, { params }).pipe(
      map(budgets => budgets.map(b => this.convertBudgetDates(b))),
      tap(list => console.log('📊 Budgets fetched:', list)),
      catchError(this.handleError)
    );
  }

  /** Get single budget */
  getBudget(id: string): Observable<Budget> {
    return this.http.get<Budget>(`${this.BASE_URL}/budgets/${id}`).pipe(
      map(b => this.convertBudgetDates(b)),
      tap(b => console.log('📊 Budget fetched:', b)),
      catchError(this.handleError)
    );
  }

  /** Get budget transactions */
  getBudgetTransactions(budgetId: string): Observable<Transaction[]> {
    return this.http.get<Transaction[]>(`${this.BASE_URL}/budgets/${budgetId}/transactions`).pipe(
      map(transactions => transactions.map(t => this.convertTransactionDates(t))),
      tap(t => console.log('💰 Budget transactions fetched:', t)),
      catchError(this.handleError)
    );
  }

  /** Create budget */
  createBudget(dto: CreateBudgetRequest): Observable<Budget> {
    return this.http.post<Budget>(`${this.BASE_URL}/budgets`, dto).pipe(
      map(b => this.convertBudgetDates(b)),
      tap(b => console.log('✅ Budget created:', b)),
      catchError(this.handleError)
    );
  }

  /** Update budget */
  updateBudget(id: string, dto: UpdateBudgetRequest): Observable<Budget> {
    return this.http.put<Budget>(`${this.BASE_URL}/budgets/${id}`, dto).pipe(
      map(b => this.convertBudgetDates(b)),
      tap(b => console.log('✅ Budget updated:', b)),
      catchError(this.handleError)
    );
  }

  /** Delete budget */
  /** Delete budget */
  deleteBudget(id: string): Observable<void> {
    return this.http.delete<void>(`${this.BASE_URL}/budgets/${id}`).pipe(
      tap(() => console.log('✅ Budget deleted:', id)),
      catchError(this.handleError)
    );
  }

  /** Archive budget */
  archiveBudget(id: string): Observable<void> {
    return this.http.post<void>(`${this.BASE_URL}/budgets/${id}/archive`, {}).pipe(
      tap(() => console.log('📦 Budget archived:', id)),
      catchError(this.handleError)
    );
  }

  /** Restore archived budget */
  restoreBudget(id: string): Observable<void> {
    return this.http.post<void>(`${this.BASE_URL}/budgets/${id}/restore`, {}).pipe(
      tap(() => console.log('♻️ Budget restored:', id)),
      catchError(this.handleError)
    );
  }

  /** Check if budget is overspent */
  isBudgetOverspent(id: string): Observable<BudgetOverspentResponse> {
    return this.http.get<BudgetOverspentResponse>(`${this.BASE_URL}/budgets/${id}/overspent`).pipe(
      tap(result => console.log('📊 Budget overspent check:', result)),
      catchError(this.handleError)
    );
  }

  // ============================================
  // Financial Goals
  // ============================================

  /** Get financial goals with filtering */
  getGoals(filter?: GoalFilter): Observable<FinancialGoal[]> {
    let params = new HttpParams();

    if (filter) {
      if (filter.isCompleted !== undefined) params = params.set('isCompleted', String(filter.isCompleted));
      if (filter.isArchived !== undefined) params = params.set('isArchived', String(filter.isArchived));
      if (filter.currencyCode) params = params.set('currencyCode', filter.currencyCode);
      if (filter.deadlineFrom) params = params.set('deadlineFrom', this.toIsoDate(filter.deadlineFrom));
      if (filter.deadlineTo) params = params.set('deadlineTo', this.toIsoDate(filter.deadlineTo));
    }

    return this.http.get<FinancialGoal[]>(`${this.BASE_URL}/goals`, { params }).pipe(
      map(goals => goals.map(g => this.convertGoalDates(g))),
      tap(list => console.log('🎯 Goals fetched:', list)),
      catchError(this.handleError)
    );
  }

  /** Get single goal */
  getGoal(id: string): Observable<FinancialGoal> {
    return this.http.get<FinancialGoal>(`${this.BASE_URL}/goals/${id}`).pipe(
      map(g => this.convertGoalDates(g)),
      tap(g => console.log('🎯 Goal fetched:', g)),
      catchError(this.handleError)
    );
  }

  /** Get goal transactions */
  getGoalTransactions(goalId: string): Observable<Transaction[]> {
    return this.http.get<Transaction[]>(`${this.BASE_URL}/goals/${goalId}/transactions`).pipe(
      map(transactions => transactions.map(t => this.convertTransactionDates(t))),
      tap(t => console.log('💰 Goal transactions fetched:', t)),
      catchError(this.handleError)
    );
  }

  /** Create financial goal */
  createGoal(dto: CreateGoalRequest): Observable<FinancialGoal> {
    return this.http.post<FinancialGoal>(`${this.BASE_URL}/goals`, dto).pipe(
      map(g => this.convertGoalDates(g)),
      tap(g => console.log('✅ Goal created:', g)),
      catchError(this.handleError)
    );
  }

  /** Update financial goal */
  updateGoal(id: string, dto: UpdateGoalRequest): Observable<FinancialGoal> {
    return this.http.put<FinancialGoal>(`${this.BASE_URL}/goals/${id}`, dto).pipe(
      map(g => this.convertGoalDates(g)),
      tap(g => console.log('✅ Goal updated:', g)),
      catchError(this.handleError)
    );
  }

  /** Delete financial goal */
  deleteGoal(id: string): Observable<void> {
    return this.http.delete<void>(`${this.BASE_URL}/goals/${id}`).pipe(
      tap(() => console.log('✅ Goal deleted:', id)),
      catchError(this.handleError)
    );
  }

  /** Archive financial goal */
  archiveGoal(id: string): Observable<void> {
    return this.http.post<void>(`${this.BASE_URL}/goals/${id}/archive`, {}).pipe(
      tap(() => console.log('✅ Goal archived:', id)),
      catchError(this.handleError)
    );
  }

  /** Restore archived financial goal */
  restoreGoal(id: string): Observable<void> {
    return this.http.post<void>(`${this.BASE_URL}/goals/${id}/restore`, {}).pipe(
      tap(() => console.log('✅ Goal restored:', id)),
      catchError(this.handleError)
    );
  }

  /** Add amount to goal progress */
  addGoalAmount(id: string, dto: UpdateGoalAmountRequest): Observable<FinancialGoal> {
    return this.http.post<FinancialGoal>(`${this.BASE_URL}/goals/${id}/add-amount`, dto).pipe(
      map(g => this.convertGoalDates(g)),
      tap(g => console.log('✅ Amount added to goal:', g)),
      catchError(this.handleError)
    );
  }

  /** Subtract amount from goal progress */
  subtractGoalAmount(id: string, dto: UpdateGoalAmountRequest): Observable<FinancialGoal> {
    return this.http.post<FinancialGoal>(`${this.BASE_URL}/goals/${id}/subtract-amount`, dto).pipe(
      map(g => this.convertGoalDates(g)),
      tap(g => console.log('✅ Amount subtracted from goal:', g)),
      catchError(this.handleError)
    );
  }

  /** Set current amount for goal */
  setGoalAmount(id: string, dto: UpdateGoalAmountRequest): Observable<FinancialGoal> {
    return this.http.post<FinancialGoal>(`${this.BASE_URL}/goals/${id}/set-amount`, dto).pipe(
      map(g => this.convertGoalDates(g)),
      tap(g => console.log('✅ Goal amount set:', g)),
      catchError(this.handleError)
    );
  }

  // ============================================
  // Statistics
  // ============================================

  /** Get financial statistics for custom period */
  getStats(periodStart: string | Date, periodEnd: string | Date, currencyCode?: string): Observable<FinanceStats> {
    let params = new HttpParams()
      .set('periodStart', this.toIsoDate(periodStart))
      .set('periodEnd', this.toIsoDate(periodEnd));

    if (currencyCode) {
      params = params.set('currencyCode', currencyCode);
    }

    return this.http.get<FinanceStats>(`${this.BASE_URL}/stats`, { params }).pipe(
      map(stats => this.convertStatsDates(stats)),
      tap(stats => console.log('📊 Finance stats fetched:', stats)),
      catchError(this.handleError)
    );
  }

  /** Get statistics for current month */
  getCurrentMonthStats(currencyCode?: string): Observable<FinanceStats> {
    let params = new HttpParams();
    if (currencyCode) {
      params = params.set('currencyCode', currencyCode);
    }

    return this.http.get<FinanceStats>(`${this.BASE_URL}/stats/current-month`, { params }).pipe(
      map(stats => this.convertStatsDates(stats)),
      tap(stats => console.log('📊 Current month stats fetched:', stats)),
      catchError(this.handleError)
    );
  }

  /** Get statistics for current year */
  getCurrentYearStats(currencyCode?: string): Observable<FinanceStats> {
    let params = new HttpParams();
    if (currencyCode) {
      params = params.set('currencyCode', currencyCode);
    }

    return this.http.get<FinanceStats>(`${this.BASE_URL}/stats/current-year`, { params }).pipe(
      map(stats => this.convertStatsDates(stats)),
      tap(stats => console.log('📊 Current year stats fetched:', stats)),
      catchError(this.handleError)
    );
  }

  /** Get statistics for last N days */
  getLastDaysStats(days: number = 30, currencyCode?: string): Observable<FinanceStats> {
    let params = new HttpParams().set('days', String(days));
    if (currencyCode) {
      params = params.set('currencyCode', currencyCode);
    }

    return this.http.get<FinanceStats>(`${this.BASE_URL}/stats/last-days`, { params }).pipe(
      map(stats => this.convertStatsDates(stats)),
      tap(stats => console.log('📊 Last days stats fetched:', stats)),
      catchError(this.handleError)
    );
  }

  // ============================================
  // Date Conversion Helpers
  // ============================================

  private convertTransactionPagedDates(result: PaginatedResult<Transaction>): PaginatedResult<Transaction> {
    return { ...result, items: result.items.map(tx => this.convertTransactionDates(tx)) };
  }

  private convertTransactionDates(tx: Transaction): Transaction {
    return {
      ...tx,
      date: new Date(tx.date),
      createdAt: new Date(tx.createdAt),
      updatedAt: new Date(tx.updatedAt)
    };
  }

  private convertBudgetDates(budget: Budget): Budget {
    return {
      ...budget,
      periodStart: new Date(budget.periodStart),
      periodEnd: new Date(budget.periodEnd),
      createdAt: new Date(budget.createdAt),
      updatedAt: budget.updatedAt ? new Date(budget.updatedAt) : undefined
    };
  }

  private convertGoalDates(goal: FinancialGoal): FinancialGoal {
    return {
      ...goal,
      deadline: goal.deadline ? new Date(goal.deadline) : null,
      createdAt: new Date(goal.createdAt),
      updatedAt: new Date(goal.updatedAt),
      completedAt: goal.completedAt ? new Date(goal.completedAt) : null
    };
  }

  private convertStatsDates(stats: FinanceStats): FinanceStats {
    return {
      ...stats,
      periodStart: new Date(stats.periodStart),
      periodEnd: new Date(stats.periodEnd)
    };
  }

  /** Convert string or Date to ISO with timezone Z for backend query */
  private toIsoDate(value: string | Date): string {
    const dateObj = typeof value === 'string' ? new Date(value) : value;
    if (isNaN(dateObj.getTime())) {
      return typeof value === 'string' ? value : '';
    }
    return new Date(Date.UTC(
      dateObj.getUTCFullYear(),
      dateObj.getUTCMonth(),
      dateObj.getUTCDate(),
      dateObj.getUTCHours(),
      dateObj.getUTCMinutes(),
      dateObj.getUTCSeconds()
    )).toISOString();
  }

  // ============================================
  // Currencies
  // ============================================

  /** Get all available currencies */
  getCurrencies(): Observable<Currency[]> {
    return this.http.get<Currency[]>(this.CURRENCIES_URL).pipe(
      tap(currencies => console.log('💱 Currencies fetched:', currencies)),
      catchError(this.handleError)
    );
  }

  // ============================================
  // Error Handling
  // ============================================

  private handleError(error: any): Observable<never> {
    const problemErrors = error?.error?.errors;
    console.error('❌ Finance service error:', {
      status: error.status,
      statusText: error.statusText,
      url: error.url,
      message: error.message,
      server: error.error,
      validation: problemErrors || null
    });

    let message = 'An error occurred';
    if (error.error?.message) message = error.error.message;
    else if (error.message) message = error.message;
    else if (error.status === 0) message = 'Unable to connect to server';
    else if (error.status === 404) message = 'Resource not found';
    else if (error.status === 401) message = 'Unauthorized';
    else if (error.status === 403) message = 'Forbidden';

    // Show first validation error if available
    if (problemErrors) {
      const first = Object.values(problemErrors).flat()[0];
      if (first) message = first as string;
    }

    return throwError(() => new Error(message));
  }
}
