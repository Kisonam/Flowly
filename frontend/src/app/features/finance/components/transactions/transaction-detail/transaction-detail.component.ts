import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FinanceService } from '../../../services/finance.service';
import { Transaction } from '../../../models/finance.models';
import { LinkService } from '../../../../../shared/services/link.service';
import { Link, LinkEntityType, EntityPreview } from '../../../../../shared/models/link.models';
import { Subject, takeUntil } from 'rxjs';
import { ThemeService } from '../../../../../core/services/theme.service';

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
  private linkService = inject(LinkService);
  private themeService = inject(ThemeService);
  private destroy$ = new Subject<void>();

  transaction: Transaction | null = null;
  loading = false;
  errorMessage = '';

  // Links
  links: Link[] = [];
  linkedNotes: EntityPreview[] = [];
  linkedTasks: EntityPreview[] = [];
  linkedTransactions: EntityPreview[] = [];

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
          this.loadLinks();
        },
        error: (err: any) => {
          console.error('❌ Failed to load transaction', err);
          this.errorMessage = err.message || 'Failed to load transaction';
          this.loading = false;
        }
      });
  }

  loadLinks(): void {
    if (!this.transaction) return;

    this.linkService.getLinksForEntity(LinkEntityType.Transaction, this.transaction.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (links) => {
          this.links = links;
          this.processLinks(links);
          console.log('🔗 Links loaded:', links);
        },
        error: (err) => {
          console.error('❌ Failed to load links', err);
        }
      });
  }

  processLinks(links: Link[]): void {
    this.linkedNotes = [];
    this.linkedTasks = [];
    this.linkedTransactions = [];

    links.forEach(link => {
      // Get the preview of the entity we're linked to (the "other" entity)
      const preview = link.fromType === LinkEntityType.Transaction && link.fromId === this.transaction?.id
        ? link.toPreview
        : link.fromPreview;

      if (!preview) return;

      switch (preview.type) {
        case LinkEntityType.Note:
          this.linkedNotes.push(preview);
          break;
        case LinkEntityType.Task:
          this.linkedTasks.push(preview);
          break;
        case LinkEntityType.Transaction:
          this.linkedTransactions.push(preview);
          break;
      }
    });
  }

  getTransactionColor(type: string): string {
    const successColor = this.themeService.getCssVarValue('--success', '#10b981');
    const dangerColor = this.themeService.getCssVarValue('--danger', '#ef4444');
    return type === 'Income' ? successColor : dangerColor;
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

  navigateToBudget(budgetId: string): void {
    this.router.navigate(['/finance/budgets', budgetId]);
  }

  navigateToGoal(goalId: string): void {
    this.router.navigate(['/finance/goals', goalId]);
  }
}
