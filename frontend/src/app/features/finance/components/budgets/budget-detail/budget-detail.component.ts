import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FinanceService } from '../../../services/finance.service';
import { Budget, Transaction } from '../../../models/finance.models';
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'app-budget-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslateModule],
  templateUrl: './budget-detail.component.html',
  styleUrls: ['./budget-detail.component.scss']
})
export class BudgetDetailComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private financeService = inject(FinanceService);
  private translate = inject(TranslateService);
  private destroy$ = new Subject<void>();

  budget: Budget | null = null;
  transactions: Transaction[] = [];
  loading = false;
  loadingTransactions = false;
  errorMessage = '';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadBudget(id);
      this.loadTransactions(id);
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadBudget(id: string): void {
    this.loading = true;
    this.errorMessage = '';

    this.financeService.getBudget(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (budget: Budget) => {
          this.budget = budget;
          this.loading = false;
          console.log('💼 Budget loaded:', budget);
        },
        error: (err: any) => {
          console.error('❌ Failed to load budget', err);
          this.errorMessage = err.message || this.translate.instant('FINANCE.BUDGETS.ERRORS.LOAD_FAILED');
          this.loading = false;
        }
      });
  }

  loadTransactions(budgetId: string): void {
    this.loadingTransactions = true;

    this.financeService.getBudgetTransactions(budgetId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (transactions: Transaction[]) => {
          this.transactions = transactions;
          this.loadingTransactions = false;
          console.log('💰 Budget transactions loaded:', transactions);
        },
        error: (err: any) => {
          console.error('❌ Failed to load budget transactions', err);
          this.loadingTransactions = false;
        }
      });
  }

  getProgressPercentage(): number {
    if (!this.budget) return 0;
    return Math.min((this.budget.currentSpent / this.budget.limit) * 100, 100);
  }

  getProgressColor(): string {
    const percentage = this.getProgressPercentage();
    if (percentage >= 100) return '#ef4444';
    if (percentage >= 80) return '#f59e0b';
    return '#10b981';
  }

  getRemainingAmount(): number {
    if (!this.budget) return 0;
    return Math.max(this.budget.limit - this.budget.currentSpent, 0);
  }

  isOverBudget(): boolean {
    if (!this.budget) return false;
    return this.budget.currentSpent > this.budget.limit;
  }

  formatAmount(amount: number, currency: string): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currency
    }).format(amount);
  }

  formatDate(date: string | Date): string {
    return new Date(date).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }

  editBudget(): void {
    if (this.budget) {
      this.router.navigate(['/finance/budgets', this.budget.id, 'edit']);
    }
  }

  deleteBudget(): void {
    if (!this.budget) return;

    const confirmMessage = this.budget.isArchived
      ? this.translate.instant('FINANCE.BUDGETS.ERRORS.DELETE_CONFIRM')
      : this.translate.instant('FINANCE.BUDGETS.ERRORS.ARCHIVE_CONFIRM');

    if (!confirm(confirmMessage)) return;

    const request = this.budget.isArchived
      ? this.financeService.deleteBudget(this.budget.id)
      : this.financeService.archiveBudget(this.budget.id);

    request
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          console.log(this.budget?.isArchived ? '✅ Budget deleted permanently' : '📦 Budget archived');
          this.router.navigate(['/finance/budgets']);
        },
        error: (err: any) => {
          console.error('❌ Failed to delete/archive budget', err);
          alert(this.translate.instant('FINANCE.BUDGETS.ERRORS.DELETE_FAILED') + ': ' + err.message);
        }
      });
  }

  restoreBudget(): void {
    if (!this.budget) return;

    if (!confirm(this.translate.instant('FINANCE.BUDGETS.ERRORS.RESTORE_CONFIRM'))) return;

    this.financeService.restoreBudget(this.budget.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          console.log('✅ Budget restored');
          this.loadBudget(this.budget!.id);
        },
        error: (err: any) => {
          console.error('❌ Failed to restore budget', err);
          alert(this.translate.instant('FINANCE.BUDGETS.ERRORS.RESTORE_FAILED') + ': ' + err.message);
        }
      });
  }

  goBack(): void {
    this.router.navigate(['/finance/budgets']);
  }

  viewTransaction(transactionId: string): void {
    this.router.navigate(['/finance/transactions', transactionId]);
  }

  getTransactionTypeIcon(type: string): string {
    return type === 'Income' ? '💰' : '💸';
  }

  getTransactionTypeColor(type: string): string {
    return type === 'Income' ? '#10b981' : '#ef4444';
  }
}
