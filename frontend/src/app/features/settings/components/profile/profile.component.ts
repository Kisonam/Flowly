import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../../auth/services/auth.service';
import { User } from '../../../auth/models/user.model';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslateModule],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss']
})
export class ProfileComponent implements OnInit {
  private authService = inject(AuthService);
  private fb = inject(FormBuilder);

  currentUser: User | null = null;
  profileForm!: FormGroup;
  passwordForm!: FormGroup;

  isLoadingProfile = false;
  isLoadingPassword = false;
  profileMessage = '';
  passwordMessage = '';
  profileError = '';
  passwordError = '';

  selectedFile: File | null = null;
  avatarPreview: string | null = null;
  isUploadingAvatar = false;

  ngOnInit(): void {
    this.initializeForms();
    this.loadUserProfile();
  }

  private initializeForms(): void {
    this.profileForm = this.fb.group({
      displayName: ['', [Validators.required, Validators.minLength(2)]],
      email: [{ value: '', disabled: true }]
    });

    this.passwordForm = this.fb.group({
      currentPassword: ['', [Validators.required]],
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required]]
    }, { validators: this.passwordMatchValidator });
  }

  private passwordMatchValidator(group: FormGroup): { [key: string]: boolean } | null {
    const newPassword = group.get('newPassword')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    return newPassword === confirmPassword ? null : { passwordMismatch: true };
  }

  private loadUserProfile(): void {
    this.authService.currentUser$.subscribe(user => {
      if (user) {
        this.currentUser = user;
        this.profileForm.patchValue({
          displayName: user.displayName,
          email: user.email
        });
        this.avatarPreview = user.avatarUrl || null;
      }
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      const file = input.files[0];

      if (!file.type.startsWith('image/')) {
        this.profileError = 'Будь ласка, оберіть файл зображення';
        return;
      }

      if (file.size > 5 * 1024 * 1024) {
        this.profileError = 'Розмір файлу не повинен перевищувати 5MB';
        return;
      }

      this.selectedFile = file;

      const reader = new FileReader();
      reader.onload = () => {
        this.avatarPreview = reader.result as string;
      };
      reader.readAsDataURL(file);

      this.uploadAvatar();
    }
  }

  uploadAvatar(): void {
    if (!this.selectedFile) return;

    this.isUploadingAvatar = true;
    this.profileError = '';
    this.profileMessage = '';

    this.authService.uploadAvatar(this.selectedFile).subscribe({
      next: (response) => {
        this.isUploadingAvatar = false;
        this.profileMessage = 'Аватар успішно оновлено';
        this.avatarPreview = response.avatarUrl;
        setTimeout(() => this.profileMessage = '', 3000);
      },
      error: (error) => {
        this.isUploadingAvatar = false;
        this.profileError = error.message || 'Не вдалося завантажити аватар';
      }
    });
  }

  deleteAvatar(): void {
    if (!confirm('Ви впевнені, що хочете видалити аватар?')) {
      return;
    }

    this.isUploadingAvatar = true;
    this.profileError = '';
    this.profileMessage = '';

    this.authService.deleteAvatar().subscribe({
      next: () => {
        this.isUploadingAvatar = false;
        this.avatarPreview = null;
        this.selectedFile = null;
        this.profileMessage = 'Аватар успішно видалено';
        setTimeout(() => this.profileMessage = '', 3000);
      },
      error: (error) => {
        this.isUploadingAvatar = false;
        this.profileError = error.message || 'Не вдалося видалити аватар';
      }
    });
  }

  updateProfile(): void {
    if (this.profileForm.invalid) {
      return;
    }

    this.isLoadingProfile = true;
    this.profileError = '';
    this.profileMessage = '';

    const { displayName } = this.profileForm.value;

    this.authService.updateProfile({ displayName }).subscribe({
      next: () => {
        this.isLoadingProfile = false;
        this.profileMessage = 'Профіль успішно оновлено';
        setTimeout(() => this.profileMessage = '', 3000);
      },
      error: (error) => {
        this.isLoadingProfile = false;
        this.profileError = error.message || 'Не вдалося оновити профіль';
      }
    });
  }

  changePassword(): void {
    if (this.passwordForm.invalid) {
      return;
    }

    this.isLoadingPassword = true;
    this.passwordError = '';
    this.passwordMessage = '';

    const { currentPassword, newPassword, confirmPassword } = this.passwordForm.value;

    this.authService.changePassword({
      currentPassword,
      newPassword,
      confirmNewPassword: confirmPassword
    }).subscribe({
      next: () => {
        this.isLoadingPassword = false;
        this.passwordMessage = 'Пароль успішно змінено. Будь ласка, увійдіть знову на всіх пристроях.';
        this.passwordForm.reset();
        setTimeout(() => this.passwordMessage = '', 5000);
      },
      error: (error) => {
        this.isLoadingPassword = false;
        this.passwordError = error.message || 'Не вдалося змінити пароль';
      }
    });
  }

  get displayNameError(): string {
    const control = this.profileForm.get('displayName');
    if (control?.hasError('required') && control.touched) {
      return "Ім'я обов'язкове";
    }
    if (control?.hasError('minlength') && control.touched) {
      return "Ім'я має бути не менше 2 символів";
    }
    return '';
  }

  get currentPasswordError(): string {
    const control = this.passwordForm.get('currentPassword');
    if (control?.hasError('required') && control.touched) {
      return 'Поточний пароль обов\'язковий';
    }
    return '';
  }

  get newPasswordError(): string {
    const control = this.passwordForm.get('newPassword');
    if (control?.hasError('required') && control.touched) {
      return 'Новий пароль обов\'язковий';
    }
    if (control?.hasError('minlength') && control.touched) {
      return 'Пароль має бути не менше 8 символів';
    }
    return '';
  }

  get confirmPasswordError(): string {
    const control = this.passwordForm.get('confirmPassword');
    if (control?.hasError('required') && control.touched) {
      return 'Підтвердження пароля обов\'язкове';
    }
    if (this.passwordForm.hasError('passwordMismatch') && control?.touched) {
      return 'Паролі не співпадають';
    }
    return '';
  }
}
