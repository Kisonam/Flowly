import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FinanceService } from '../../../services/finance.service';
import { Transaction } from '../../../models/finance.models';
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'app-transaction-detail',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './transaction-detail.component.html',
  styleUrls: ['./transaction-detail.component.scss']
})
export class TransactionDetailComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private financeService = inject(FinanceService);
  private destroy$ = new Subject<void>();

  transaction: Transaction | null = null;
  loading = false;
  errorMessage = '';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadTransaction(id);
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadTransaction(id: string): void {
    this.loading = true;
    this.errorMessage = '';

    this.financeService.getTransaction(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (transaction: Transaction) => {
          this.transaction = transaction;
          this.loading = false;
          console.log('💰 Transaction loaded:', transaction);
        },
        error: (err: any) => {
          console.error('❌ Failed to load transaction', err);
          this.errorMessage = err.message || 'Failed to load transaction';
          this.loading = false;
        }
      });
  }

  getTransactionColor(type: string): string {
    return type === 'Income' ? '#10b981' : '#ef4444';
  }

  getTransactionIcon(type: string): string {
    return type === 'Income' ? '💵' : '💸';
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

  formatDateTime(date: string | Date): string {
    return new Date(date).toLocaleString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  editTransaction(): void {
    if (this.transaction) {
      this.router.navigate(['/finance/transactions', this.transaction.id, 'edit']);
    }
  }

  archiveTransaction(): void {
    if (!this.transaction) return;
    if (!confirm(`Archive transaction "${this.transaction.title}"?`)) return;

    this.financeService.archiveTransaction(this.transaction.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          console.log('✅ Transaction archived');
          this.router.navigate(['/finance/transactions']);
        },
        error: (err: any) => {
          console.error('❌ Failed to archive transaction', err);
          alert('Failed to archive transaction: ' + err.message);
        }
      });
  }

  restoreTransaction(): void {
    if (!this.transaction) return;

    this.financeService.restoreTransaction(this.transaction.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          console.log('✅ Transaction restored');
          this.loadTransaction(this.transaction!.id);
        },
        error: (err: any) => {
          console.error('❌ Failed to restore transaction', err);
          alert('Failed to restore transaction: ' + err.message);
        }
      });
  }

  goBack(): void {
    this.router.navigate(['/finance/transactions']);
  }
}
