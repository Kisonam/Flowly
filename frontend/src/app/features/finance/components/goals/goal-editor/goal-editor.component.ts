import { Component, EventEmitter, Input, OnInit, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FinancialGoal, CreateGoalRequest, UpdateGoalRequest, Currency } from '../../../models/finance.models';
import { FinanceService } from '../../../services/finance.service';

@Component({
  selector: 'app-goal-editor',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule],
  templateUrl: './goal-editor.component.html',
  styleUrl: './goal-editor.component.scss'
})
export class GoalEditorComponent implements OnInit {
  @Input() goal?: FinancialGoal | null = null; // For editing existing goal
  @Output() save = new EventEmitter<CreateGoalRequest | UpdateGoalRequest>();
  @Output() cancel = new EventEmitter<void>();

  private fb = inject(FormBuilder);
  private financeService = inject(FinanceService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private translate = inject(TranslateService);

  goalForm!: FormGroup;
  isEditMode = false;
  currencies: Currency[] = [];
  loadingCurrencies = false;
  saving = false;
  goalId?: string;

  ngOnInit(): void {
    // Check if we have a goal ID from route params
    this.goalId = this.route.snapshot.paramMap.get('id') || undefined;
    this.isEditMode = !!this.goalId;

    this.initForm();
    this.loadCurrencies();

    // If edit mode, load the goal data
    if (this.goalId) {
      this.loadGoal(this.goalId);
    }
  }

  private loadGoal(id: string): void {
    this.financeService.getGoals({ isArchived: false }).subscribe({
      next: (goals) => {
        const goal = goals.find(g => g.id === id);
        if (goal) {
          this.goal = goal;
          this.patchFormWithGoal(goal);
        } else {
          console.error('Goal not found');
          this.router.navigate(['/finance/goals']);
        }
      },
      error: (err) => {
        console.error('Failed to load goal:', err);
        this.router.navigate(['/finance/goals']);
      }
    });
  }

  private patchFormWithGoal(goal: FinancialGoal): void {
    this.goalForm.patchValue({
      title: goal.title,
      description: goal.description,
      targetAmount: goal.targetAmount,
      currentAmount: goal.currentAmount,
      currencyCode: goal.currencyCode,
      deadline: this.formatDateForInput(goal.deadline)
    });

    // In edit mode, disable currentAmount
    this.goalForm.get('currentAmount')?.disable();
  }

  private loadCurrencies(): void {
    this.loadingCurrencies = true;

    // Disable currency select while loading
    if (this.goalForm) {
      this.goalForm.get('currencyCode')?.disable();
    }

    this.financeService.getCurrencies().subscribe({
      next: (currencies) => {
        this.currencies = currencies;
        this.loadingCurrencies = false;
        // Enable currency select after loading
        if (this.goalForm) {
          this.goalForm.get('currencyCode')?.enable();
        }
      },
      error: (err) => {
        console.error('Failed to load currencies:', err);
        this.loadingCurrencies = false;
        // Fallback to default currencies if API fails
        this.currencies = [
          { code: 'UAH', symbol: '₴', name: 'Ukrainian Hryvnia' },
          { code: 'USD', symbol: '$', name: 'US Dollar' },
          { code: 'EUR', symbol: '€', name: 'Euro' },
          { code: 'PLN', symbol: 'zł', name: 'Polish Zloty' }
        ];
        // Enable currency select even after error
        if (this.goalForm) {
          this.goalForm.get('currencyCode')?.enable();
        }
      }
    });
  }

  private initForm(): void {
    this.goalForm = this.fb.group({
      title: [this.goal?.title || '', [Validators.required, Validators.maxLength(200)]],
      description: [this.goal?.description || ''],
      targetAmount: [this.goal?.targetAmount || null, [Validators.required, Validators.min(0.01)]],
      currentAmount: [this.goal?.currentAmount || 0, [Validators.min(0)]],
      currencyCode: [this.goal?.currencyCode || 'UAH', Validators.required],
      deadline: [this.formatDateForInput(this.goal?.deadline)]
    });

    // In edit mode, disable currentAmount (should be updated via separate endpoint)
    if (this.isEditMode) {
      this.goalForm.get('currentAmount')?.disable();
    }
  }

  private formatDateForInput(date?: string | Date | null): string | null {
    if (!date) return null;
    const dateObj = typeof date === 'string' ? new Date(date) : date;
    if (isNaN(dateObj.getTime())) return null;
    return dateObj.toISOString().split('T')[0]; // Format: YYYY-MM-DD
  }

  onSubmit(): void {
    console.log('onSubmit called');
    console.log('Form valid:', this.goalForm.valid);
    console.log('Form value:', this.goalForm.value);

    if (this.goalForm.invalid) {
      this.goalForm.markAllAsTouched();
      console.log('Form is invalid, marking as touched');
      return;
    }

    if (this.saving) {
      console.log('Already saving, returning');
      return; // Prevent double submission
    }

    this.saving = true;
    const formValue = this.goalForm.getRawValue(); // getRawValue() includes disabled fields
    console.log('Form raw value:', formValue);

    if (this.isEditMode && this.goalId) {
      // Update existing goal (without currentAmount)
      const updateRequest: UpdateGoalRequest = {
        title: formValue.title,
        description: formValue.description || undefined,
        targetAmount: formValue.targetAmount,
        currencyCode: formValue.currencyCode,
        deadline: formValue.deadline || undefined
      };

      // Emit for parent components if used
      if (this.save.observers.length > 0) {
        this.save.emit(updateRequest);
      }

      this.financeService.updateGoal(this.goalId, updateRequest).subscribe({
        next: () => {
          console.log('Goal updated successfully');
          this.saving = false;
          this.router.navigate(['/finance/goals']);
        },
        error: (err) => {
          console.error('Failed to update goal:', err);
          this.saving = false;
          alert(this.translate.instant('FINANCE.GOALS.ERRORS.UPDATE_FAILED'));
        }
      });
    } else {
      // Create new goal
      const createRequest: CreateGoalRequest = {
        title: formValue.title,
        description: formValue.description || undefined,
        targetAmount: formValue.targetAmount,
        currentAmount: formValue.currentAmount || 0,
        currencyCode: formValue.currencyCode,
        deadline: formValue.deadline || undefined
      };

      // Emit for parent components if used
      if (this.save.observers.length > 0) {
        this.save.emit(createRequest);
      }

      this.financeService.createGoal(createRequest).subscribe({
        next: () => {
          console.log('Goal created successfully');
          this.saving = false;
          this.router.navigate(['/finance/goals']);
        },
        error: (err) => {
          console.error('Failed to create goal:', err);
          this.saving = false;
          alert(this.translate.instant('FINANCE.GOALS.ERRORS.CREATE_FAILED'));
        }
      });
    }
  }

  onCancel(): void {
    // Navigate back or emit cancel event
    if (this.cancel.observers.length > 0) {
      this.cancel.emit();
    } else {
      this.router.navigate(['/finance/goals']);
    }
  }

  // Helper to get form control errors
  getError(controlName: string): string | null {
    const control = this.goalForm.get(controlName);
    if (!control || !control.errors || !control.touched) return null;

    if (control.errors['required']) return this.translate.instant('FINANCE.GOALS.EDITOR.ERRORS.REQUIRED');
    if (control.errors['min']) return this.translate.instant('FINANCE.GOALS.EDITOR.ERRORS.MIN_VALUE', { min: control.errors['min'].min });
    if (control.errors['maxlength']) return this.translate.instant('FINANCE.EDITOR.ERRORS.MAX_LENGTH', { max: control.errors['maxlength'].requiredLength }); // Reusing generic editor error if available or create new.
    // I don't have MAX_LENGTH in GOALS.EDITOR.ERRORS. I'll use generic INVALID or add it.
    // I added FINANCE.EDITOR.ERRORS.MAX_LENGTH in budget editor task. I can reuse it if it's in FINANCE.EDITOR.
    // Let's check en.json. Yes, FINANCE.EDITOR.ERRORS.MAX_LENGTH exists.
    // Wait, FINANCE.EDITOR is for Transaction Editor? No, it's generic FINANCE.EDITOR.
    // Actually, I added FINANCE.EDITOR.ERRORS.MAX_LENGTH in budget editor task.
    // Let's use FINANCE.EDITOR.ERRORS.MAX_LENGTH.
    
    return this.translate.instant('FINANCE.GOALS.EDITOR.ERRORS.INVALID');
  }

  get minDate(): string {
    return new Date().toISOString().split('T')[0]; // Today
  }
}
