import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FinanceService } from '../../../services/finance.service';
import { Budget, Category, Currency, CreateBudgetRequest, UpdateBudgetRequest } from '../../../models/finance.models';

@Component({
  selector: 'app-budget-editor',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule],
  templateUrl: './budget-editor.component.html',
  styleUrl: './budget-editor.component.scss'
})
export class BudgetEditorComponent implements OnInit {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private financeService = inject(FinanceService);
  private translate = inject(TranslateService);

  budgetForm!: FormGroup;
  budgetId: string | null = null;
  isEditMode = false;
  isLoading = false;
  isSaving = false;
  errorMessage = '';

  categories: Category[] = [];
  loadingCategories = false;

  currencies: Currency[] = [];
  loadingCurrencies = false;

  ngOnInit(): void {
    this.initializeForm();
    this.loadCategories();
    this.loadCurrencies();

    this.budgetId = this.route.snapshot.paramMap.get('id');
    this.isEditMode = !!this.budgetId;

    if (this.isEditMode && this.budgetId) {
      this.loadBudget(this.budgetId);
    }
  }
  private initializeForm(): void {
    this.budgetForm = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(100)]],
      description: ['', Validators.maxLength(500)],
      limit: ['', [Validators.required, Validators.min(0.01)]],
      currencyCode: ['UAH', Validators.required],
      categoryId: [''],
      periodStart: ['', Validators.required],
      periodEnd: ['', Validators.required]
    }, { validators: this.dateRangeValidator });
  }

  private dateRangeValidator(group: FormGroup): { [key: string]: boolean } | null {
    const startDate = group.get('periodStart')?.value;
    const endDate = group.get('periodEnd')?.value;

    if (startDate && endDate) {
      const start = new Date(startDate);
      const end = new Date(endDate);

      if (start >= end) {
        return { dateRangeInvalid: true };
      }
    }

    return null;
  }  private loadCategories(): void {
    this.loadingCategories = true;
    this.financeService.getCategories().subscribe({
      next: (categories) => {
        this.categories = categories;
        this.loadingCategories = false;
      },
      error: (error) => {
        console.error('Failed to load categories:', error);
        this.errorMessage = this.translate.instant('FINANCE.BUDGETS.ERRORS.LOAD_FAILED');
        this.loadingCategories = false;
      }
    });
  }

  private loadCurrencies(): void {
    this.loadingCurrencies = true;
    this.financeService.getCurrencies().subscribe({
      next: (currencies) => {
        this.currencies = currencies;
        this.loadingCurrencies = false;
      },
      error: (err) => {
        console.error('Failed to load currencies:', err);
        this.loadingCurrencies = false;
        
        this.currencies = [
          { code: 'UAH', symbol: '₴', name: 'Ukrainian Hryvnia' },
          { code: 'USD', symbol: '$', name: 'US Dollar' },
          { code: 'EUR', symbol: '€', name: 'Euro' },
          { code: 'PLN', symbol: 'zł', name: 'Polish Zloty' }
        ];
      }
    });
  }

  private loadBudget(id: string): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.financeService.getBudget(id).subscribe({
      next: (budget: Budget) => {
        this.patchForm(budget);
        this.isLoading = false;
      },
      error: (error: any) => {
        console.error('Failed to load budget:', error);
        this.errorMessage = this.translate.instant('FINANCE.BUDGETS.ERRORS.LOAD_FAILED');
        this.isLoading = false;
      }
    });
  }

  private patchForm(budget: Budget): void {
    
    const startDate = this.formatDateForInput(budget.periodStart);
    const endDate = this.formatDateForInput(budget.periodEnd);

    this.budgetForm.patchValue({
      title: budget.title,
      description: budget.description || '',
      limit: budget.limit,
      currencyCode: budget.currencyCode,
      categoryId: budget.category?.id || '',
      periodStart: startDate,
      periodEnd: endDate
    });
  }

  private formatDateForInput(date: string | Date): string {
    try {
      const d = typeof date === 'string' ? new Date(date) : date;
      if (isNaN(d.getTime())) {
        return '';
      }
      const year = d.getFullYear();
      const month = String(d.getMonth() + 1).padStart(2, '0');
      const day = String(d.getDate()).padStart(2, '0');
      return `${year}-${month}-${day}`;
    } catch (error) {
      console.error('Error formatting date:', error);
      return '';
    }
  }

  onSubmit(): void {
    if (this.budgetForm.invalid) {
      this.budgetForm.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';

    if (this.isEditMode && this.budgetId) {
      this.updateBudget();
    } else {
      this.createBudget();
    }
  }

  private createBudget(): void {
    const request: CreateBudgetRequest = {
      title: this.budgetForm.value.title.trim(),
      description: this.budgetForm.value.description?.trim() || undefined,
      periodStart: this.budgetForm.value.periodStart,
      periodEnd: this.budgetForm.value.periodEnd,
      limit: Number(this.budgetForm.value.limit),
      currencyCode: this.budgetForm.value.currencyCode,
      categoryId: this.budgetForm.value.categoryId || undefined
    };

    this.financeService.createBudget(request).subscribe({
      next: () => {
        this.router.navigate(['/finance/budgets']);
      },
      error: (error) => {
        console.error('Failed to create budget:', error);
        this.errorMessage = error.error?.message || this.translate.instant('FINANCE.CATEGORIES.ERRORS.CREATE_FAILED');
        this.isSaving = false;
      }
    });
  }

  private updateBudget(): void {
    if (!this.budgetId) return;

    const request: UpdateBudgetRequest = {
      title: this.budgetForm.value.title.trim(),
      description: this.budgetForm.value.description?.trim() || undefined,
      periodStart: this.budgetForm.value.periodStart,
      periodEnd: this.budgetForm.value.periodEnd,
      limit: Number(this.budgetForm.value.limit),
      currencyCode: this.budgetForm.value.currencyCode,
      categoryId: this.budgetForm.value.categoryId || undefined
    };

    this.financeService.updateBudget(this.budgetId, request).subscribe({
      next: () => {
        this.router.navigate(['/finance/budgets']);
      },
      error: (error) => {
        console.error('Failed to update budget:', error);
        this.errorMessage = error.error?.message || this.translate.instant('FINANCE.CATEGORIES.ERRORS.UPDATE_FAILED');
        this.isSaving = false;
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/finance/budgets']);
  }

  getCategoryColor(categoryId: string): string {
    const category = this.categories.find(c => c.id === categoryId);
    return category?.color || '#6b7280';
  }

  getCategoryIcon(categoryId: string): string {
    const category = this.categories.find(c => c.id === categoryId);
    return category?.icon || '📊';
  }

  getCategoryName(categoryId: string): string {
    const category = this.categories.find(c => c.id === categoryId);
    return category?.name || '';
  }

  getCurrencySymbol(code: string): string {
    return this.currencies.find(c => c.code === code)?.symbol || code;
  }

  hasError(field: string): boolean {
    const control = this.budgetForm.get(field);
    return !!(control && control.invalid && (control.dirty || control.touched));
  }

  getErrorMessage(field: string): string {
    const control = this.budgetForm.get(field);
    if (!control || !control.errors) return '';

    if (control.errors['required']) {
      return this.translate.instant('FINANCE.EDITOR.ERRORS.REQUIRED');
    }
    if (control.errors['min']) {
      return this.translate.instant('FINANCE.EDITOR.ERRORS.MIN_VALUE', { min: control.errors['min'].min });
    }
    if (control.errors['maxlength']) {
      return this.translate.instant('FINANCE.EDITOR.ERRORS.MAX_LENGTH', { length: control.errors['maxlength'].requiredLength });
    }

    return this.translate.instant('FINANCE.EDITOR.ERRORS.INVALID');
  }

  hasDateRangeError(): boolean {
    return !!(this.budgetForm.errors?.['dateRangeInvalid'] &&
             (this.budgetForm.get('periodStart')?.touched || this.budgetForm.get('periodEnd')?.touched));
  }
}
