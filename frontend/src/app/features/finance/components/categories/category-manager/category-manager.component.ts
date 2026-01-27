import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FinanceService } from '../../../services/finance.service';
import { Category, CreateCategoryRequest, UpdateCategoryRequest } from '../../../models/finance.models';

@Component({
  selector: 'app-category-manager',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, TranslateModule],
  templateUrl: './category-manager.component.html',
  styleUrls: ['./category-manager.component.scss']
})
export class CategoryManagerComponent implements OnInit, OnDestroy {
  private readonly financeService = inject(FinanceService);
  private readonly fb = inject(FormBuilder);
  private readonly translate = inject(TranslateService);
  private readonly destroy$ = new Subject<void>();

  categories: Category[] = [];
  loading = false;
  errorMessage = '';

  showForm = false;
  editMode = false;
  currentCategoryId: string | null = null;
  categoryForm: FormGroup;

  selectedColor = '#10b981';
  selectedIcon = '📁';

  readonly ICON_OPTIONS = [
    '🍔', '🚗', '🛒', '🎬', '💡', '🏥', '📚', '✈️', '📈', '💰', '💼', '🏢',
    '🏠', '🎮', '☕', '🎵', '📱', '💻', '🎨', '⚽', '🎁', '🔧', '📦', '🌟'
  ];

  readonly COLOR_OPTIONS = [
    '#10b981', '#3b82f6', '#8b5cf6', '#f59e0b', '#ef4444', '#ec4899',
    '#06b6d4', '#84cc16', '#f97316', '#14b8a6', '#6366f1', '#a855f7'
  ];

  constructor() {
    this.categoryForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      icon: ['📁'],
      color: ['#10b981']
    });
  }

  ngOnInit(): void {
    this.loadCategories();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadCategories(): void {
    this.loading = true;
    this.errorMessage = '';

    this.financeService.getCategories()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (categories: Category[]) => {
          this.categories = categories;
          this.loading = false;
          console.log('📂 Categories loaded:', categories);
        },
        error: (err: any) => {
          console.error('❌ Failed to load categories', err);
          this.errorMessage = err.message || this.translate.instant('FINANCE.CATEGORIES.ERRORS.LOAD_FAILED');
          this.loading = false;
        }
      });
  }

  openCreateForm(): void {
    this.editMode = false;
    this.currentCategoryId = null;
    this.categoryForm.reset({
      name: '',
      icon: '📁',
      color: '#10b981'
    });
    this.selectedIcon = '📁';
    this.selectedColor = '#10b981';
    this.showForm = true;
  }

  openEditForm(category: Category): void {
    if (this.isPredefinedCategory(category)) {
      alert(this.translate.instant('FINANCE.CATEGORIES.ERRORS.PREDEFINED_EDIT'));
      return;
    }

    this.editMode = true;
    this.currentCategoryId = category.id;
    this.categoryForm.patchValue({
      name: category.name,
      icon: category.icon || '📁',
      color: category.color || '#10b981'
    });
    this.selectedIcon = category.icon || '📁';
    this.selectedColor = category.color || '#10b981';
    this.showForm = true;
  }

  closeForm(): void {
    this.showForm = false;
    this.categoryForm.reset();
    this.editMode = false;
    this.currentCategoryId = null;
  }

  selectIcon(icon: string): void {
    this.selectedIcon = icon;
    this.categoryForm.patchValue({ icon });
  }

  selectColor(color: string): void {
    this.selectedColor = color;
    this.categoryForm.patchValue({ color });
  }

  onSubmit(): void {
    if (this.categoryForm.invalid) {
      this.markFormGroupTouched(this.categoryForm);
      return;
    }

    if (this.editMode && this.currentCategoryId) {
      this.updateCategory();
    } else {
      this.createCategory();
    }
  }

  createCategory(): void {
    const request: CreateCategoryRequest = {
      name: this.categoryForm.value.name.trim(),
      icon: this.categoryForm.value.icon,
      color: this.categoryForm.value.color
    };

    this.financeService.createCategory(request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          console.log('✅ Category created');
          this.loadCategories();
          this.closeForm();
        },
        error: (err: any) => {
          console.error('❌ Failed to create category', err);
          alert(this.translate.instant('FINANCE.CATEGORIES.ERRORS.CREATE_FAILED') + ': ' + (err.error?.message || err.message));
        }
      });
  }

  updateCategory(): void {
    if (!this.currentCategoryId) return;

    const request: UpdateCategoryRequest = {
      name: this.categoryForm.value.name.trim(),
      icon: this.categoryForm.value.icon,
      color: this.categoryForm.value.color
    };

    this.financeService.updateCategory(this.currentCategoryId, request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          console.log('✅ Category updated');
          this.loadCategories();
          this.closeForm();
        },
        error: (err: any) => {
          console.error('❌ Failed to update category', err);
          alert(this.translate.instant('FINANCE.CATEGORIES.ERRORS.UPDATE_FAILED') + ': ' + (err.error?.message || err.message));
        }
      });
  }

  deleteCategory(category: Category): void {
    if (this.isPredefinedCategory(category)) {
      alert(this.translate.instant('FINANCE.CATEGORIES.ERRORS.PREDEFINED_DELETE'));
      return;
    }

    if (!confirm(this.translate.instant('FINANCE.CATEGORIES.ERRORS.DELETE_CONFIRM', { name: category.name }))) {
      return;
    }

    this.financeService.deleteCategory(category.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          console.log('✅ Category deleted');
          this.loadCategories();
        },
        error: (err: any) => {
          console.error('❌ Failed to delete category', err);
          alert(this.translate.instant('FINANCE.CATEGORIES.ERRORS.DELETE_FAILED') + ': ' + (err.error?.message || err.message));
        }
      });
  }

  isPredefinedCategory(category: Category): boolean {
    
    return category.userId === null || category.userId === undefined;
  }

  getFieldError(fieldName: string): string {
    const field = this.categoryForm.get(fieldName);
    if (!field || !field.touched || !field.errors) return '';

    if (field.errors['required']) return this.translate.instant('FINANCE.CATEGORIES.ERRORS.REQUIRED');
    if (field.errors['maxlength']) {
      const maxLength = field.errors['maxlength'].requiredLength;
      return this.translate.instant('FINANCE.CATEGORIES.ERRORS.MAX_LENGTH', { length: maxLength });
    }
    return '';
  }

  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();
    });
  }
}
