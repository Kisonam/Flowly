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
  @Input() goal?: FinancialGoal | null = null; 
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
    
    this.goalId = this.route.snapshot.paramMap.get('id') || undefined;
    this.isEditMode = !!this.goalId;

    this.initForm();
    this.loadCurrencies();

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

    this.goalForm.get('currentAmount')?.disable();
  }

  private loadCurrencies(): void {
    this.loadingCurrencies = true;

    if (this.goalForm) {
      this.goalForm.get('currencyCode')?.disable();
    }

    this.financeService.getCurrencies().subscribe({
      next: (currencies) => {
        this.currencies = currencies;
        this.loadingCurrencies = false;
        
        if (this.goalForm) {
          this.goalForm.get('currencyCode')?.enable();
        }
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

    if (this.isEditMode) {
      this.goalForm.get('currentAmount')?.disable();
    }
  }

  private formatDateForInput(date?: string | Date | null): string | null {
    if (!date) return null;
    const dateObj = typeof date === 'string' ? new Date(date) : date;
    if (isNaN(dateObj.getTime())) return null;
    return dateObj.toISOString().split('T')[0]; 
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
      return; 
    }

    this.saving = true;
    const formValue = this.goalForm.getRawValue(); 
    console.log('Form raw value:', formValue);

    if (this.isEditMode && this.goalId) {
      
      const updateRequest: UpdateGoalRequest = {
        title: formValue.title,
        description: formValue.description || undefined,
        targetAmount: formValue.targetAmount,
        currencyCode: formValue.currencyCode,
        deadline: formValue.deadline || undefined
      };

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
      
      const createRequest: CreateGoalRequest = {
        title: formValue.title,
        description: formValue.description || undefined,
        targetAmount: formValue.targetAmount,
        currentAmount: formValue.currentAmount || 0,
        currencyCode: formValue.currencyCode,
        deadline: formValue.deadline || undefined
      };

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
    
    if (this.cancel.observers.length > 0) {
      this.cancel.emit();
    } else {
      this.router.navigate(['/finance/goals']);
    }
  }

  getError(controlName: string): string | null {
    const control = this.goalForm.get(controlName);
    if (!control || !control.errors || !control.touched) return null;

    if (control.errors['required']) return this.translate.instant('FINANCE.GOALS.EDITOR.ERRORS.REQUIRED');
    if (control.errors['min']) return this.translate.instant('FINANCE.GOALS.EDITOR.ERRORS.MIN_VALUE', { min: control.errors['min'].min });
    if (control.errors['maxlength']) return this.translate.instant('FINANCE.EDITOR.ERRORS.MAX_LENGTH', { max: control.errors['maxlength'].requiredLength }); 

    return this.translate.instant('FINANCE.GOALS.EDITOR.ERRORS.INVALID');
  }

  get minDate(): string {
    return new Date().toISOString().split('T')[0]; 
  }
}
