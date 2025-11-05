// frontend/src/app/features/auth/components/register/register.component.ts

import { Component, inject, OnInit, OnDestroy, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { GoogleAuthService } from '../../services/google-auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent implements OnInit, OnDestroy, AfterViewInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private googleAuthService = inject(GoogleAuthService);
  private router = inject(Router);
  private destroy$ = new Subject<void>();

  registerForm!: FormGroup;
  isLoading = false;
  errorMessage = '';
  showPassword = false;
  showConfirmPassword = false;

  ngOnInit(): void {
    this.initForm();

    // Initialize Google Auth and subscribe to responses
    this.googleAuthService.initialize()
      .then(() => {
        // Subscribe to Google credential responses
        this.googleAuthService.getCredentialResponse()
          .pipe(takeUntil(this.destroy$))
          .subscribe({
            next: (credential) => this.handleGoogleCredential(credential),
            error: (error) => {
              console.error('❌ Google authentication failed:', error);
              this.errorMessage = 'Failed to authenticate with Google. Please try again.';
              this.isLoading = false;
            }
          });
      })
      .catch(error => {
        console.warn('Failed to initialize Google Auth:', error);
      });
  }

  ngAfterViewInit(): void {
    // Render Google Sign-Up button after view is initialized
    setTimeout(() => {
      const buttonContainer = document.getElementById('googleSignUpButton');
      if (buttonContainer && this.googleAuthService.isAvailable()) {
        try {
          this.googleAuthService.renderButton(buttonContainer, {
            type: 'standard',
            theme: 'outline',
            size: 'large',
            text: 'signup_with',
            shape: 'rectangular',
            width: buttonContainer.offsetWidth || 300
          });
          // Hide fallback button if Google button rendered successfully
          const fallbackButton = document.querySelector('.google-fallback-button') as HTMLElement;
          if (fallbackButton) {
            fallbackButton.style.display = 'none';
          }
        } catch (error) {
          console.error('Failed to render Google button:', error);
        }
      }
    }, 100);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initForm(): void {
    this.registerForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      displayName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      password: ['', [
        Validators.required,
        Validators.minLength(8),
        this.passwordValidator
      ]],
      confirmPassword: ['', [Validators.required]]
    }, {
      validators: this.passwordMatchValidator
    });
  }

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.markFormGroupTouched(this.registerForm);
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.authService.register(this.registerForm.value)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          console.log('✅ Registration successful');
          this.router.navigate(['/']);
        },
        error: (error) => {
          console.error('❌ Registration failed:', error);
          this.errorMessage = error.message || 'Registration failed. Please try again.';
          this.isLoading = false;
        }
      });
  }

  private handleGoogleCredential(credential: string): void {
    console.log('✅ Google credential received');
    this.isLoading = true;

    // Send credential to backend (registration through Google)
    this.authService.loginWithGoogle(credential)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          console.log('✅ Google registration successful, redirecting to dashboard');
          this.isLoading = false;
          this.router.navigate(['/dashboard']);
        },
        error: (error) => {
          console.error('❌ Google registration failed:', error);
          this.errorMessage = error.message || 'Google registration failed. Please try again.';
          this.isLoading = false;
        }
      });
  }

  registerWithGoogle(): void {
    if (!this.googleAuthService.isAvailable()) {
      this.errorMessage = 'Google Sign-In is not available. Please refresh the page.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    // Trigger Google prompt
    try {
      this.googleAuthService.showOneTap();
    } catch (error) {
      console.error('Failed to show Google prompt:', error);
      this.errorMessage = 'Failed to open Google Sign-In. Please try again.';
      this.isLoading = false;
    }
  }  togglePasswordVisibility(field: 'password' | 'confirmPassword'): void {
    if (field === 'password') {
      this.showPassword = !this.showPassword;
    } else {
      this.showConfirmPassword = !this.showConfirmPassword;
    }
  }

  // Custom Validators
  private passwordValidator(control: AbstractControl): ValidationErrors | null {
    const value = control.value;
    if (!value) return null;

    const hasUpperCase = /[A-Z]/.test(value);
    const hasLowerCase = /[a-z]/.test(value);
    const hasNumeric = /[0-9]/.test(value);
    const hasSpecialChar = /[!@#$%^&*(),.?":{}|<>]/.test(value);

    const valid = hasUpperCase && hasLowerCase && hasNumeric && hasSpecialChar;

    if (!valid) {
      return {
        passwordStrength: {
          hasUpperCase,
          hasLowerCase,
          hasNumeric,
          hasSpecialChar
        }
      };
    }

    return null;
  }

  private passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password');
    const confirmPassword = control.get('confirmPassword');

    if (!password || !confirmPassword) return null;

    return password.value === confirmPassword.value ? null : { passwordMismatch: true };
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

  // Helper methods for template
  get email() { return this.registerForm.get('email'); }
  get displayName() { return this.registerForm.get('displayName'); }
  get password() { return this.registerForm.get('password'); }
  get confirmPassword() { return this.registerForm.get('confirmPassword'); }
  get passwordMismatch() { return this.registerForm.errors?.['passwordMismatch']; }
}
