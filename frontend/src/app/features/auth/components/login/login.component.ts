

import { Component, inject, OnInit, OnDestroy, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { GoogleAuthService } from '../../services/google-auth.service';

import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, TranslateModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit, OnDestroy, AfterViewInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private googleAuthService = inject(GoogleAuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private destroy$ = new Subject<void>();

  loginForm!: FormGroup;
  isLoading = false;
  errorMessage = '';
  showPassword = false;
  returnUrl = '/dashboard';

  ngOnInit(): void {
    this.initForm();

    const q = this.route.snapshot.queryParams['returnUrl'] as string | undefined;
    if (q && q.trim().length > 0) {
      
      if (q === '/' || q.startsWith('/auth')) {
        this.returnUrl = '/dashboard';
      } else {
        this.returnUrl = q;
      }
    } else {
      this.returnUrl = '/dashboard';
    }

    this.googleAuthService.initialize()
      .then(() => {
        
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
    
    setTimeout(() => {
      const buttonContainer = document.getElementById('googleSignInButton');
      if (buttonContainer && this.googleAuthService.isAvailable()) {
        try {
          this.googleAuthService.renderButton(buttonContainer, {
            type: 'standard',
            theme: 'outline',
            size: 'large',
            text: 'signin_with',
            shape: 'rectangular',
            width: buttonContainer.offsetWidth || 300
          });
          
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
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      rememberMe: [false]
    });
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.markFormGroupTouched(this.loginForm);
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.authService.login(this.loginForm.value)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          console.log('✅ Login successful, redirecting to:', this.returnUrl);
          this.router.navigateByUrl(this.returnUrl);
        },
        error: (error) => {
          console.error('❌ Login failed:', error);
          this.errorMessage = error.message || 'Login failed. Please check your credentials.';
          this.isLoading = false;
        }
      });
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  private handleGoogleCredential(credential: string): void {
    console.log('✅ Google credential received');
    this.isLoading = true;

    this.authService.loginWithGoogle(credential)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          console.log('✅ Google login successful, redirecting to:', this.returnUrl);
          this.isLoading = false;
          this.router.navigateByUrl(this.returnUrl);
        },
        error: (error) => {
          console.error('❌ Google login failed:', error);
          this.errorMessage = error.message || 'Google login failed. Please try again.';
          this.isLoading = false;
        }
      });
  }

  loginWithGoogle(): void {
    if (!this.googleAuthService.isAvailable()) {
      this.errorMessage = 'Google Sign-In is not available. Please refresh the page.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    try {
      this.googleAuthService.showOneTap();
    } catch (error) {
      console.error('Failed to show Google prompt:', error);
      this.errorMessage = 'Failed to open Google Sign-In. Please try again.';
      this.isLoading = false;
    }
  }  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();

      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }

  get email() { return this.loginForm.get('email'); }
  get password() { return this.loginForm.get('password'); }
}
