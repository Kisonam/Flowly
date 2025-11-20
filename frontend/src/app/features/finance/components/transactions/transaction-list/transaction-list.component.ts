import { Component, OnInit, OnDestroy, HostListener, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Subject, debounceTime, takeUntil } from 'rxjs';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ThemeService } from '../../../../../core/services/theme.service';
import { FinanceService } from '../../../services/finance.service';
import { TagsService } from '../../../../../shared/services/tags.service';
import {
  Transaction,
  TransactionType,
  Category,
  TransactionFilter,
  PaginatedResult
} from '../../../models/finance.models';

interface SortOption {
  key: 'date' | 'amount' | 'createdAt';
  label: string;
}

@Component({
  selector: 'app-transaction-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, TranslateModule],
  templateUrl: './transaction-list.component.html',
  styleUrls: ['./transaction-list.component.scss']
})
export class TransactionListComponent implements OnInit, OnDestroy {
  private financeService = inject(FinanceService);
  private tagsService = inject(TagsService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private themeService = inject(ThemeService);
  private translate = inject(TranslateService);
  private destroy$ = new Subject<void>();

  // Data
  transactions: Transaction[] = [];
  categories: Category[] = [];
  tags: { id: string; name: string; color?: string }[] = [];
  page = 1;
  pageSize = 20;
  totalPages = 1;
  totalCount = 0;

  // UI state
  loading = false;
  errorMessage = '';
  empty = false;
  showFilters = true; // Toggle filters visibility
  showQuickActions = false; // Toggle quick actions menu

  // Currency options
  currencies: { code: string; name: string; symbol: string }[] = [];

  // Transaction types
  transactionTypes: { value: TransactionType; label: string; color: string }[] = [
    { value: 'Income', label: 'FINANCE.DASHBOARD.INCOME', color: '#10b981' },
    { value: 'Expense', label: 'FINANCE.DASHBOARD.EXPENSE', color: '#ef4444' }
  ];

  // Sorting
  sortOptions: SortOption[] = [
    { key: 'date', label: 'FINANCE.TRANSACTIONS.TABLE.DATE' },
    { key: 'amount', label: 'FINANCE.TRANSACTIONS.TABLE.AMOUNT' },
    { key: 'createdAt', label: 'FINANCE.TRANSACTIONS.TABLE.CREATED' }
  ];
  currentSort: SortOption | null = { key: 'createdAt', label: 'FINANCE.TRANSACTIONS.TABLE.CREATED' }; // Default sort by creation date
  sortDirection: 'asc' | 'desc' = 'desc';

  // Filter form
  filterForm: FormGroup = this.fb.group({
    search: [''],
    type: [''],
    categoryId: [''],
    currencyCode: [''],
    tagIds: [[] as string[]],
    dateFrom: [''],
    dateTo: [''],
    isArchived: [false]
  });

  ngOnInit(): void {
    this.loadAuxData();
    this.setupFilterListeners();
    this.fetchTransactions();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  @HostListener('document:keydown.escape')
  onEscapePress(): void {
    if (this.showQuickActions) {
      this.closeQuickActions();
    }
  }

  loadAuxData(): void {
    // Load currencies
    this.financeService.getCurrencies().pipe(takeUntil(this.destroy$)).subscribe({
      next: (currencies: any) => {
        this.currencies = currencies;
        console.log('💱 Currencies loaded:', currencies);
      },
      error: (err: any) => console.error('❌ Failed to load currencies', err)
    });

    // Load categories & tags in parallel
    this.financeService.getCategories().pipe(takeUntil(this.destroy$)).subscribe({
      next: (categories: Category[]) => {
        this.categories = categories;
        console.log('📂 Categories loaded:', categories);
      },
      error: (err: any) => console.error('❌ Failed to load categories', err)
    });

    this.tagsService.getTags().pipe(takeUntil(this.destroy$)).subscribe({
      next: (tags: any) => {
        this.tags = tags;
        console.log('🏷️ Tags loaded:', tags);
      },
      error: (err: any) => console.error('❌ Failed to load tags', err)
    });
  }

  setupFilterListeners(): void {
    this.filterForm.valueChanges
      .pipe(debounceTime(300), takeUntil(this.destroy$))
      .subscribe(() => {
        this.page = 1; // reset page on filter change
        this.fetchTransactions();
      });
  }

  buildFilter(): TransactionFilter {
    const v = this.filterForm.value;
    return {
      search: v.search?.trim() || undefined,
      type: v.type || undefined,
      categoryId: v.categoryId || undefined,
      currencyCode: v.currencyCode || undefined,
      tagIds: v.tagIds?.length ? v.tagIds : undefined,
      dateFrom: v.dateFrom || undefined,
      dateTo: v.dateTo || undefined,
      // When checkbox is unchecked (false), show only non-archived (false)
      // When checkbox is checked (true), show only archived (true)
      isArchived: v.isArchived === true ? true : false,
      page: this.page,
      pageSize: this.pageSize
    };
  }

  fetchTransactions(): void {
    this.loading = true;
    this.errorMessage = '';
    const filter = this.buildFilter();

    console.log('🔍 Fetching transactions with filter:', filter);

    this.financeService.getTransactions(filter)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result: PaginatedResult<Transaction>) => {
          console.log('💰 Raw transactions from API:', result);
          console.log('💰 First transaction detail:', result.items[0]);

          this.transactions = this.applySorting(result.items.slice());
          this.totalCount = result.totalCount;
          this.page = result.page;
          this.pageSize = result.pageSize;
          this.totalPages = result.totalPages;
          this.empty = result.items.length === 0;
          this.loading = false;

          console.log('✅ Transactions processed:', this.transactions.length);
          console.log('✅ First transaction after processing:', this.transactions[0]);
        },
        error: (err: any) => {
          console.error('❌ Failed to fetch transactions', err);
          this.errorMessage = err.message || 'Failed to fetch transactions';
          this.loading = false;
        }
      });
  }

  // Sorting
  setSort(option: SortOption): void {
    if (this.currentSort?.key === option.key) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.currentSort = option;
      this.sortDirection = 'desc'; // default desc for transactions
    }
    this.transactions = this.applySorting(this.transactions.slice());
  }

  applySorting(list: Transaction[]): Transaction[] {
    if (!this.currentSort) return list;
    const dir = this.sortDirection === 'asc' ? 1 : -1;

    return list.sort((a, b) => {
      switch (this.currentSort!.key) {
        case 'date': {
          const ad = new Date(a.date).getTime();
          const bd = new Date(b.date).getTime();
          return (bd - ad) * dir; // Fixed: bd - ad for descending by default
        }
        case 'amount':
          return (b.amount - a.amount) * dir; // Fixed: b - a for descending by default
        case 'createdAt': {
          const ad = new Date(a.createdAt).getTime();
          const bd = new Date(b.createdAt).getTime();
          return (bd - ad) * dir; // Fixed: bd - ad for descending by default
        }
        default:
          return 0;
      }
    });
  }

  // Pagination
  prevPage(): void {
    if (this.page > 1) {
      this.page--;
      this.fetchTransactions();
    }
  }

  nextPage(): void {
    if (this.page < this.totalPages) {
      this.page++;
      this.fetchTransactions();
    }
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.page = page;
      this.fetchTransactions();
    }
  }

  // Filters
  toggleTag(tagId: string): void {
    const current = this.filterForm.get('tagIds')?.value || [];
    const index = current.indexOf(tagId);

    if (index > -1) {
      current.splice(index, 1);
    } else {
      current.push(tagId);
    }

    this.filterForm.patchValue({ tagIds: [...current] });
  }

  isTagSelected(tagId: string): boolean {
    const current = this.filterForm.get('tagIds')?.value || [];
    return current.includes(tagId);
  }

  clearFilters(): void {
    this.filterForm.reset({
      search: '',
      type: '',
      categoryId: '',
      currencyCode: '',
      tagIds: [],
      dateFrom: '',
      dateTo: '',
      isArchived: false
    });
  }

  // Actions
  viewTransaction(transaction: Transaction): void {
    this.router.navigate(['/finance/transactions', transaction.id]);
  }

  archiveTransaction(transaction: Transaction): void {
    // TODO: Use a proper dialog service instead of confirm/alert
    if (!confirm(this.translate.instant('FINANCE.TRANSACTIONS.ARCHIVE_CONFIRM', { title: transaction.title }))) return;

    this.financeService.archiveTransaction(transaction.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          console.log('✅ Transaction archived');
          this.fetchTransactions();
        },
        error: (err: any) => {
          console.error('❌ Failed to archive transaction', err);
          alert(this.translate.instant('FINANCE.TRANSACTIONS.ARCHIVE_ERROR', { message: err.message }));
        }
      });
  }

  restoreTransaction(transaction: Transaction): void {
    this.financeService.restoreTransaction(transaction.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          console.log('✅ Transaction restored');
          this.fetchTransactions();
        },
        error: (err: any) => {
          console.error('❌ Failed to restore transaction', err);
          alert(this.translate.instant('FINANCE.TRANSACTIONS.RESTORE_ERROR', { message: err.message }));
        }
      });
  }

  // UI Toggles
  toggleFilters(): void {
    this.showFilters = !this.showFilters;
  }

  toggleQuickActions(): void {
    this.showQuickActions = !this.showQuickActions;
  }

  closeQuickActions(): void {
    this.showQuickActions = false;
  }

  // Helpers
  getTransactionColor(type: TransactionType): string {
    const successColor = this.themeService.getCssVarValue('--success', '#10b981');
    const dangerColor = this.themeService.getCssVarValue('--danger', '#ef4444');
    return type === 'Income' ? successColor : dangerColor;
  }

  getTransactionIcon(type: TransactionType): string {
    return type === 'Income' ? '↑' : '↓';
  }

  getTagColor(tag: { color?: string | null }): string {
    if (tag.color) return tag.color;
    return this.themeService.getCssVarValue('--primary', '#8b5cf6');
  }

  getCategoryColor(category: { color?: string | null }): string {
    if (category.color) return category.color;
    return this.themeService.getCssVarValue('--bg-secondary', '#e5e7eb');
  }

  formatAmount(amount: number, currencyCode: string): string {
    if (amount === null || amount === undefined || isNaN(amount)) {
      return '—';
    }

    try {
      return new Intl.NumberFormat('uk-UA', {
        style: 'currency',
        currency: currencyCode || 'UAH',
        minimumFractionDigits: 0,
        maximumFractionDigits: 2
      }).format(amount);
    } catch (error) {
      console.error('Error formatting amount:', amount, currencyCode, error);
      return `${amount} ${currencyCode || 'UAH'}`;
    }
  }

  formatDate(date: Date | string | null | undefined): string {
    if (!date) return '—';

    try {
      const d = typeof date === 'string' ? new Date(date) : date;

      // Check if date is valid
      if (isNaN(d.getTime())) {
        return '—';
      }

      return new Intl.DateTimeFormat('uk-UA', {
        day: '2-digit',
        month: 'short',
        year: 'numeric'
      }).format(d);
    } catch (error) {
      console.error('Error formatting date:', date, error);
      return '—';
    }
  }

  getCategoryName(categoryId?: string | null): string {
    if (!categoryId) return 'Uncategorized';
    const category = this.categories.find(c => c.id === categoryId);
    return category?.name || 'Unknown';
  }

  getActiveFiltersCount(): number {
    let count = 0;
    const values = this.filterForm.value;

    if (values.search) count++;
    if (values.type) count++;
    if (values.categoryId) count++;
    if (values.currencyCode) count++;
    if (values.tagIds && values.tagIds.length > 0) count++;
    if (values.dateFrom) count++;
    if (values.dateTo) count++;

    return count;
  }

  getPageNumbers(): number[] {
    const pages: number[] = [];
    const maxVisible = 5;

    if (this.totalPages <= maxVisible) {
      for (let i = 1; i <= this.totalPages; i++) {
        pages.push(i);
      }
    } else {
      const start = Math.max(1, this.page - 2);
      const end = Math.min(this.totalPages, start + maxVisible - 1);

      for (let i = start; i <= end; i++) {
        pages.push(i);
      }
    }

    return pages;
  }
}
