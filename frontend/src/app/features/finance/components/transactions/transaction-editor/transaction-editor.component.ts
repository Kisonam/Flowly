import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, of, switchMap, takeUntil, tap } from 'rxjs';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ThemeService } from '../../../../../core/services/theme.service';
import { DialogService } from '../../../../../core/services/dialog.service';
import { FinanceService } from '../../../services/finance.service';
import { TagsService } from '../../../../../shared/services/tags.service';
import { LinkSelectorComponent } from '../../../../../shared/components/link-selector/link-selector.component';
import { TagManagerComponent } from '../../../../../shared/components/tag-manager/tag-manager.component';
import { Link, LinkEntityType } from '../../../../../shared/models/link.models';
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
  imports: [CommonModule, ReactiveFormsModule, LinkSelectorComponent, TagManagerComponent, TranslateModule],
  templateUrl: './transaction-editor.component.html',
  styleUrls: ['./transaction-editor.component.scss']
})
export class TransactionEditorComponent implements OnInit, OnDestroy {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private financeService = inject(FinanceService);
  private tagsService = inject(TagsService);
  private themeService = inject(ThemeService);
  private translate = inject(TranslateService);
  private dialogService = inject(DialogService);
  private destroy$ = new Subject<void>();

  isEdit = false;
  transactionId: string | null = null;
  transaction?: Transaction;

  loading = false;
  saving = false;
  error = '';

  LinkEntityType = LinkEntityType;

  categories: Category[] = [];
  budgets: any[] = []; 
  filteredBudgets: any[] = []; 
  goals: any[] = []; 
  filteredGoals: any[] = []; 
  tags: { id: string; name: string; color?: string }[] = [];
  selectedTagIds: string[] = [];
  currencies: { code: string; name: string; symbol: string }[] = [];

  readonly transactionTypes: { value: TransactionType; label: string; icon: string }[] = [
    { value: 'Income', label: 'FINANCE.DASHBOARD.INCOME', icon: '↑' },
    { value: 'Expense', label: 'FINANCE.DASHBOARD.EXPENSE', icon: '↓' }
  ];

  form: FormGroup = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', Validators.maxLength(1000)],
    amount: [0, [Validators.required, Validators.min(0.01)]],
    type: ['Expense' as TransactionType, Validators.required],
    date: ['', Validators.required],
    categoryId: [''],
    budgetId: [''],
    goalId: [''],
    currencyCode: ['', Validators.required] 
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
    
    this.financeService.getCurrencies()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (currencies) => {
          this.currencies = currencies;
          console.log('💱 Currencies loaded:', currencies);

          if (!this.form.get('currencyCode')?.value && currencies.length > 0) {
            const defaultCurrency = currencies.find(c => c.code === 'UAH') || currencies[0];
            this.form.patchValue({ currencyCode: defaultCurrency.code });
          }
        },
        error: (err) => {
          console.error('❌ Failed to load currencies', err);
        }
      });

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

    this.financeService.getBudgets({ isArchived: false })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (budgets) => {
          this.budgets = budgets;
          console.log('💼 Budgets loaded:', budgets);
          
          this.filterBudgets();
        },
        error: (err) => {
          console.error('❌ Failed to load budgets', err);
        }
      });

    this.financeService.getGoals({ isArchived: false })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (goals) => {
          this.goals = goals;
          console.log('🎯 Goals loaded:', goals);
          
          this.filterGoals();
        },
        error: (err) => {
          console.error('❌ Failed to load goals', err);
        }
      });

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
    
    if (!this.isEdit) {
      const today = new Date().toISOString().split('T')[0];
      this.form.patchValue({ date: today });
    }

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

    this.filteredBudgets = this.budgets.filter(
      budget => budget.currencyCode === currencyCode && budget.daysRemaining >= 0
    );

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
    const type = this.form.get('type')?.value;

    let goals = this.goals.filter(
      goal => goal.currencyCode === currencyCode
    );

    if (type === 'Income') {
      goals = goals.filter(goal => !goal.isCompleted);
    }

    this.filteredGoals = goals;

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

  onTagsChanged(tagIds: string[]): void {
    this.selectedTagIds = tagIds;
  }

  loadTags(): void {
    this.tagsService.getTags()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (tags) => {
          this.tags = tags || [];
        },
        error: (err) => {
          console.error('Failed to load tags:', err);
          this.tags = [];
        }
      });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.markFormGroupTouched(this.form);
      this.error = 'Please fill in all required fields correctly';
      return;
    }

    const formValue = this.form.value;
    if (formValue.type === 'Expense' && formValue.goalId) {
      const goalId = formValue.goalId.trim();
      if (goalId) {
        const selectedGoal = this.filteredGoals.find(g => g.id === goalId);
        if (selectedGoal) {
          const amount = Math.abs(Number(formValue.amount));
          if (selectedGoal.currentAmount < amount) {
            const available = selectedGoal.currentAmount.toFixed(2);
            const required = amount.toFixed(2);
            alert(this.translate.instant('FINANCE.EDITOR.GOAL_INSUFFICIENT', {
              available: available,
              required: required,
              currency: selectedGoal.currencyCode
            }));
            this.error = this.translate.instant('FINANCE.EDITOR.ERRORS.INSUFFICIENT_FUNDS', {
              available: available,
              currency: selectedGoal.currencyCode
            });
            return;
          }
        }
      }
    }

    this.saving = true;
    this.error = '';

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

    const amount = Math.abs(Number(formValue.amount));

    console.log('📝 Building CreateDTO:', {
      formAmount: formValue.amount,
      parsedAmount: amount,
      type: formValue.type,
      budgetId,
      goalId
    });

    return {
      title: formValue.title.trim(),
      description: formValue.description?.trim() || undefined,
      amount: amount,
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

    const amount = Math.abs(Number(formValue.amount));

    console.log('📝 Building UpdateDTO:', {
      formAmount: formValue.amount,
      parsedAmount: amount,
      type: formValue.type,
      budgetId,
      goalId
    });

    return {
      title: formValue.title.trim(),
      description: formValue.description?.trim() || undefined,
      amount: amount,
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

    const title = this.transaction?.title || '';
    this.dialogService.confirmTranslated('COMMON.CONFIRM.ARCHIVE_TRANSACTION', { title })
      .pipe(takeUntil(this.destroy$))
      .subscribe(confirmed => {
        if (!confirmed || !this.transactionId) return;

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
      });
  }

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
    if (errors['required']) return this.translate.instant('FINANCE.EDITOR.ERRORS.REQUIRED');
    if (errors['min']) return this.translate.instant('FINANCE.EDITOR.ERRORS.MIN', { min: errors['min'].min });
    if (errors['max']) return this.translate.instant('FINANCE.EDITOR.ERRORS.MAX', { max: errors['max'].max });
    if (errors['maxlength']) return this.translate.instant('FINANCE.EDITOR.ERRORS.MAX_LENGTH', { length: errors['maxlength'].requiredLength });

    return this.translate.instant('FINANCE.EDITOR.ERRORS.INVALID');
  }

  getCurrencySymbol(code: string): string {
    const currency = this.currencies.find(c => c.code === code);
    return currency?.symbol || code;
  }

  getTransactionColor(type: TransactionType): string {
    const successColor = this.themeService.getCssVarValue('--success', '#10b981');
    const dangerColor = this.themeService.getCssVarValue('--danger', '#ef4444');
    return type === 'Income' ? successColor : dangerColor;
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

  getSelectedGoalAvailableAmount(): number | null {
    const goalId = this.form.get('goalId')?.value;
    if (!goalId) return null;

    const selectedGoal = this.filteredGoals.find(g => g.id === goalId);
    return selectedGoal ? selectedGoal.currentAmount : null;
  }

  isGoalFundsSufficient(): boolean | null {
    const type = this.form.get('type')?.value;
    const amount = this.form.get('amount')?.value;
    const availableAmount = this.getSelectedGoalAvailableAmount();

    if (type !== 'Expense' || availableAmount === null || !amount) {
      return null;
    }

    return availableAmount >= Math.abs(Number(amount));
  }

  onLinkCreated(link: Link): void {
    console.log('✅ Link created:', link);
  }

  onLinkDeleted(linkId: string): void {
    console.log('🗑️ Link deleted:', linkId);
  }
}
