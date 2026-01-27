import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FinanceService } from '../../../services/finance.service';
import { FinancialGoal, Transaction } from '../../../models/finance.models';
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'app-goal-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslateModule],
  templateUrl: './goal-detail.component.html',
  styleUrls: ['./goal-detail.component.scss']
})
export class GoalDetailComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private financeService = inject(FinanceService);
  private translate = inject(TranslateService);
  private destroy$ = new Subject<void>();

  goal: FinancialGoal | null = null;
  transactions: Transaction[] = [];
  loading = false;
  loadingTransactions = false;
  errorMessage = '';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadGoal(id);
      this.loadTransactions(id);
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadGoal(id: string): void {
    this.loading = true;
    this.errorMessage = '';

    this.financeService.getGoal(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (goal: FinancialGoal) => {
          this.goal = goal;
          this.loading = false;
          console.log('🎯 Goal loaded:', goal);
        },
        error: (err: any) => {
          console.error('❌ Failed to load goal', err);
          this.errorMessage = err.message || 'Failed to load goal';
          this.loading = false;
        }
      });
  }

  loadTransactions(goalId: string): void {
    this.loadingTransactions = true;

    this.financeService.getGoalTransactions(goalId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (transactions: Transaction[]) => {
          this.transactions = transactions;
          this.loadingTransactions = false;
          console.log('💰 Goal transactions loaded:', transactions);
        },
        error: (err: any) => {
          console.error('❌ Failed to load goal transactions', err);
          this.loadingTransactions = false;
        }
      });
  }

  getProgressPercentage(): number {
    if (!this.goal || this.goal.targetAmount === 0) return 0;
    return Math.min((this.goal.currentAmount / this.goal.targetAmount) * 100, 100);
  }

  getProgressColor(): string {
    const percentage = this.getProgressPercentage();
    if (percentage >= 100) return '#10b981'; 
    if (percentage >= 75) return '#8b5cf6'; 
    if (percentage >= 50) return '#3b82f6'; 
    return '#6b7280'; 
  }

  getRemainingAmount(): number {
    if (!this.goal) return 0;
    return Math.max(this.goal.targetAmount - this.goal.currentAmount, 0);
  }

  isCompleted(): boolean {
    if (!this.goal) return false;
    return this.goal.currentAmount >= this.goal.targetAmount;
  }

  isOverdue(): boolean {
    if (!this.goal || !this.goal.deadline) return false;
    return new Date(this.goal.deadline) < new Date() && !this.isCompleted();
  }

  getDaysRemaining(): number | null {
    if (!this.goal || !this.goal.deadline) return null;
    const now = new Date();
    const deadline = new Date(this.goal.deadline);
    const diffTime = deadline.getTime() - now.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    return diffDays;
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

  editGoal(): void {
    if (this.goal) {
      this.router.navigate(['/finance/goals', this.goal.id, 'edit']);
    }
  }

  goBack(): void {
    this.router.navigate(['/finance/goals']);
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
