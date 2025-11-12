import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, of, switchMap, takeUntil, tap } from 'rxjs';
import { FinanceService } from '../../../services/finance.service';
import { TagsService } from '../../../../../shared/services/tags.service';
import {
  Transaction,
  TransactionType,
  Category,
  CreateTransactionRequest,
  UpdateTransactionRequest
} from '../../../models/finance.models';

@Component({
  selector: 'app-transaction-editor',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './transaction-editor.component.html',
  styleUrls: ['./transaction-editor.component.scss']
})
export class TransactionEditorComponent implements OnInit, OnDestroy {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private financeService = inject(FinanceService);
  private tagsService = inject(TagsService);
  private destroy$ = new Subject<void>();

  isEdit = false;
  transactionId: string | null = null;
  transaction?: Transaction;

  loading = false;
  saving = false;
  error = '';

  // Data
  categories: Category[] = [];
  budgets: any[] = []; // All budgets list
  filteredBudgets: any[] = []; // Budgets filtered by currency and type
  goals: any[] = []; // All goals list
  filteredGoals: any[] = []; // Goals filtered by currency
  tags: { id: string; name: string; color?: string }[] = [];
  selectedTagIds: string[] = [];

  // Options
  readonly transactionTypes: { value: TransactionType; label: string; icon: string }[] = [
    { value: 'Income', label: 'Income', icon: '↑' },
    { value: 'Expense', label: 'Expense', icon: '↓' }
  ];

  readonly currencies = [
    { code: 'UAH', symbol: '₴', name: 'Ukrainian Hryvnia' },
    { code: 'USD', symbol: '$', name: 'US Dollar' },
    { code: 'EUR', symbol: '€', name: 'Euro' },
    { code: 'GBP', symbol: '£', name: 'British Pound' }
  ];

  // Form
  form: FormGroup = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', Validators.maxLength(1000)],
    amount: [0, [Validators.required, Validators.min(0.01)]],
    type: ['Expense' as TransactionType, Validators.required],
    date: ['', Validators.required],
    categoryId: [''],
    budgetId: [''],
    goalId: [''],
    currencyCode: ['UAH', Validators.required]
  });

  ngOnInit(): void {
    this.loadAuxData();
    this.setupFormListeners();
    this.loadTransaction();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadAuxData(): void {
    // Load categories
    this.financeService.getCategories()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (categories) => {
          this.categories = categories;
          console.log('📂 Categories loaded:', categories);
        },
        error: (err) => {
          console.error('❌ Failed to load categories', err);
        }
      });

    // Load budgets (active only)
    this.financeService.getBudgets({ isArchived: false })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (budgets) => {
          this.budgets = budgets;
          console.log('💼 Budgets loaded:', budgets);
          // Filter budgets after loading
          this.filterBudgets();
        },
        error: (err) => {
          console.error('❌ Failed to load budgets', err);
        }
      });

    // Load goals (active, non-archived only)
    this.financeService.getGoals({ isArchived: false })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (goals) => {
          this.goals = goals;
          console.log('🎯 Goals loaded:', goals);
          // Filter goals after loading
          this.filterGoals();
        },
        error: (err) => {
          console.error('❌ Failed to load goals', err);
        }
      });

    // Load tags
    this.tagsService.getTags()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (tags) => {
          this.tags = tags;
          console.log('🏷️ Tags loaded:', tags);
        },
        error: (err) => {
          console.error('❌ Failed to load tags', err);
        }
      });
  }

  setupFormListeners(): void {
    // Set today's date as default if creating new transaction
    if (!this.isEdit) {
      const today = new Date().toISOString().split('T')[0];
      this.form.patchValue({ date: today });
    }

    // Filter budgets and goals when currency or type changes
    this.form.get('currencyCode')?.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.filterBudgets();
        this.filterGoals();
      });

    this.form.get('type')?.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.filterBudgets();
        this.filterGoals();
      });
  }

  filterBudgets(): void {
    const currencyCode = this.form.get('currencyCode')?.value;

    // Both Income and Expense transactions can be linked to budgets
    // Income adds to budget, Expense subtracts from budget

    // Filter budgets by currency
    this.filteredBudgets = this.budgets.filter(
      budget => budget.currencyCode === currencyCode
    );

    // Clear budget selection if current budget doesn't match currency
    const currentBudgetId = this.form.get('budgetId')?.value;
    if (currentBudgetId) {
      const budgetStillValid = this.filteredBudgets.some(b => b.id === currentBudgetId);
      if (!budgetStillValid) {
        this.form.patchValue({ budgetId: '' });
      }
    }
  }

  filterGoals(): void {
    const currencyCode = this.form.get('currencyCode')?.value;

    // Both Income and Expense transactions can be linked to goals
    // Income adds to goal (contributes), Expense withdraws from goal

    // Filter goals by currency
    this.filteredGoals = this.goals.filter(
      goal => goal.currencyCode === currencyCode
    );

    // Clear goal selection if current goal doesn't match currency
    const currentGoalId = this.form.get('goalId')?.value;
    if (currentGoalId) {
      const goalStillValid = this.filteredGoals.some(g => g.id === currentGoalId);
      if (!goalStillValid) {
        this.form.patchValue({ goalId: '' });
      }
    }
  }

  loadTransaction(): void {
    this.route.paramMap
      .pipe(
        takeUntil(this.destroy$),
        tap(pm => {
          const id = pm.get('id');
          this.isEdit = !!id;
          this.transactionId = id;
        }),
        switchMap(() => {
          if (!this.isEdit || !this.transactionId) return of(null);
          this.loading = true;
          return this.financeService.getTransaction(this.transactionId);
        })
      )
      .subscribe({
        next: (transaction) => {
          if (transaction) {
            this.transaction = transaction;
            this.patchForm(transaction);
            this.loading = false;
          }
        },
        error: (err) => {
          console.error('❌ Failed to load transaction', err);
          this.error = err.message || 'Failed to load transaction';
          this.loading = false;
        }
      });
  }

  patchForm(transaction: Transaction): void {
    let dateStr = '';

    try {
      if (typeof transaction.date === 'string') {
        dateStr = transaction.date.split('T')[0];
      } else if (transaction.date) {
        const d = new Date(transaction.date);
        if (!isNaN(d.getTime())) {
          dateStr = d.toISOString().split('T')[0];
        }
      }
    } catch (error) {
      console.error('Error parsing transaction date:', transaction.date, error);
      // Default to today if date is invalid
      dateStr = new Date().toISOString().split('T')[0];
    }

    this.form.patchValue({
      title: transaction.title,
      description: transaction.description || '',
      amount: transaction.amount,
      type: transaction.type,
      date: dateStr,
      categoryId: transaction.categoryId || '',
      budgetId: transaction.budgetId || '',
      goalId: transaction.goalId || '',
      currencyCode: transaction.currencyCode
    });

    this.selectedTagIds = (transaction.tags || []).map(t => t.id);
  }

  // Tag Management
  toggleTag(tagId: string): void {
    const index = this.selectedTagIds.indexOf(tagId);
    if (index > -1) {
      this.selectedTagIds.splice(index, 1);
    } else {
      this.selectedTagIds.push(tagId);
    }
  }

  isTagSelected(tagId: string): boolean {
    return this.selectedTagIds.includes(tagId);
  }

  // Form Actions
  onSubmit(): void {
    if (this.form.invalid) {
      this.markFormGroupTouched(this.form);
      this.error = 'Please fill in all required fields correctly';
      return;
    }

    this.saving = true;
    this.error = '';

    const formValue = this.form.value;
    const dto = this.isEdit ? this.buildUpdateDto(formValue) : this.buildCreateDto(formValue);

    const operation = this.isEdit
      ? this.financeService.updateTransaction(this.transactionId!, dto as UpdateTransactionRequest)
      : this.financeService.createTransaction(dto as CreateTransactionRequest);

    operation.pipe(takeUntil(this.destroy$)).subscribe({
      next: (transaction) => {
        console.log('✅ Transaction saved:', transaction);
        this.saving = false;
        this.router.navigate(['/finance/transactions']);
      },
      error: (err) => {
        console.error('❌ Failed to save transaction', err);
        this.error = err.message || 'Failed to save transaction';
        this.saving = false;
      }
    });
  }

  buildCreateDto(formValue: any): CreateTransactionRequest {
    const categoryId = formValue.categoryId?.trim();
    const budgetId = formValue.budgetId?.trim();
    const goalId = formValue.goalId?.trim();

    return {
      title: formValue.title.trim(),
      description: formValue.description?.trim() || undefined,
      amount: Number(formValue.amount),
      type: formValue.type,
      date: formValue.date,
      categoryId: categoryId || undefined,
      budgetId: budgetId || undefined,
      goalId: goalId || undefined,
      currencyCode: formValue.currencyCode,
      tagIds: this.selectedTagIds.length > 0 ? this.selectedTagIds : undefined
    };
  }

  buildUpdateDto(formValue: any): UpdateTransactionRequest {
    const categoryId = formValue.categoryId?.trim();
    const budgetId = formValue.budgetId?.trim();
    const goalId = formValue.goalId?.trim();

    return {
      title: formValue.title.trim(),
      description: formValue.description?.trim() || undefined,
      amount: Number(formValue.amount),
      type: formValue.type,
      date: formValue.date,
      categoryId: categoryId || undefined,
      budgetId: budgetId || undefined,
      goalId: goalId || undefined,
      currencyCode: formValue.currencyCode,
      tagIds: this.selectedTagIds.length > 0 ? this.selectedTagIds : undefined
    };
  }

  onCancel(): void {
    this.router.navigate(['/finance/transactions']);
  }

  onDelete(): void {
    if (!this.isEdit || !this.transactionId) return;

    if (!confirm('Are you sure you want to archive this transaction?')) return;

    this.financeService.archiveTransaction(this.transactionId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          console.log('✅ Transaction archived');
          this.router.navigate(['/finance/transactions']);
        },
        error: (err) => {
          console.error('❌ Failed to archive transaction', err);
          this.error = err.message || 'Failed to archive transaction';
        }
      });
  }

  // Helpers
  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();
      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }

  getFieldError(fieldName: string): string {
    const control = this.form.get(fieldName);
    if (!control || !control.touched || !control.errors) return '';

    const errors = control.errors;
    if (errors['required']) return 'This field is required';
    if (errors['min']) return `Minimum value is ${errors['min'].min}`;
    if (errors['max']) return `Maximum value is ${errors['max'].max}`;
    if (errors['maxlength']) return `Maximum length is ${errors['maxlength'].requiredLength} characters`;

    return 'Invalid value';
  }

  getCurrencySymbol(code: string): string {
    const currency = this.currencies.find(c => c.code === code);
    return currency?.symbol || code;
  }

  getTransactionColor(type: TransactionType): string {
    return type === 'Income' ? '#10b981' : '#ef4444';
  }

  formatPreviewAmount(): string {
    const amount = this.form.get('amount')?.value || 0;
    const currency = this.form.get('currencyCode')?.value || 'UAH';
    const type = this.form.get('type')?.value as TransactionType;

    const formatted = new Intl.NumberFormat('uk-UA', {
      style: 'currency',
      currency: currency,
      minimumFractionDigits: 0,
      maximumFractionDigits: 2
    }).format(amount);

    return type === 'Income' ? `+${formatted}` : `-${formatted}`;
  }
}
