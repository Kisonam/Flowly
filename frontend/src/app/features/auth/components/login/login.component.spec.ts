// frontend/src/app/features/auth/components/login/login.component.spec.ts

import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { LoginComponent } from './login.component';
import { AuthService } from '../../services/auth.service';
import { GoogleAuthService } from '../../services/google-auth.service';
import { TranslateModule } from '@ngx-translate/core';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let googleAuthServiceSpy: jasmine.SpyObj<GoogleAuthService>;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    // Create spies for dependencies
    const authSpy = jasmine.createSpyObj('AuthService', ['login', 'loginWithGoogle']);
    const googleSpy = jasmine.createSpyObj('GoogleAuthService', [
      'initialize',
      'isAvailable',
      'getCredentialResponse',
      'renderButton',
      'showOneTap'
    ]);
    const routerSpyObj = jasmine.createSpyObj('Router', ['navigateByUrl', 'navigate', 'createUrlTree', 'serializeUrl'], {
      events: of()
    });
    routerSpyObj.createUrlTree.and.returnValue({} as any);
    routerSpyObj.serializeUrl.and.returnValue('');

    // Mock ActivatedRoute
    const activatedRouteMock = {
      snapshot: {
        queryParams: {}
      }
    };

    await TestBed.configureTestingModule({
      imports: [
        LoginComponent,
        ReactiveFormsModule,
        RouterModule.forRoot([]),
        TranslateModule.forRoot()
      ],
      providers: [
        { provide: AuthService, useValue: authSpy },
        { provide: GoogleAuthService, useValue: googleSpy },
        { provide: Router, useValue: routerSpyObj },
        { provide: ActivatedRoute, useValue: activatedRouteMock }
      ],
      schemas: [NO_ERRORS_SCHEMA]
    }).compileComponents();

    authServiceSpy = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
    googleAuthServiceSpy = TestBed.inject(GoogleAuthService) as jasmine.SpyObj<GoogleAuthService>;
    routerSpy = TestBed.inject(Router) as jasmine.SpyObj<Router>;

    // Setup default Google Auth mock behavior
    googleAuthServiceSpy.initialize.and.returnValue(Promise.resolve());
    googleAuthServiceSpy.getCredentialResponse.and.returnValue(of('mock-credential'));
    googleAuthServiceSpy.isAvailable.and.returnValue(false);

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    // Call detectChanges() to initialize the component
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ============================================
  // TEST 1: Form Validation - Email
  // ============================================
  describe('Form Validation - Email', () => {
    it('should mark email as invalid when empty', () => {
      // Arrange
      const emailControl = component.loginForm.get('email');

      // Act
      emailControl?.setValue('');
      emailControl?.markAsTouched();

      // Assert
      expect(emailControl?.invalid).toBe(true);
      expect(emailControl?.hasError('required')).toBe(true);
    });

    it('should mark email as invalid when format is incorrect', () => {
      // Arrange
      const emailControl = component.loginForm.get('email');

      // Act - Test various invalid email formats
      const invalidEmails = [
        'notanemail',
        '@nodomain.com',
        'spaces in@email.com',
        'double@@domain.com',
        'no-at-sign.com'
      ];

      invalidEmails.forEach(invalidEmail => {
        emailControl?.setValue(invalidEmail);
        emailControl?.markAsTouched();

        // Assert
        expect(emailControl?.invalid).toBe(true, `Email "${invalidEmail}" should be invalid`);
        expect(emailControl?.hasError('email')).toBe(true, `Email "${invalidEmail}" should have email error`);
      });
    });

    it('should mark email as valid when format is correct', () => {
      // Arrange
      const emailControl = component.loginForm.get('email');

      // Act
      const validEmails = [
        'test@example.com',
        'user.name@domain.co.uk',
        'user+tag@example.com',
        'test123@test-domain.com'
      ];

      validEmails.forEach(validEmail => {
        emailControl?.setValue(validEmail);
        emailControl?.markAsTouched();

        // Assert
        expect(emailControl?.valid).toBe(true, `Email "${validEmail}" should be valid`);
        expect(emailControl?.hasError('email')).toBe(false, `Email "${validEmail}" should not have email error`);
      });
    });
  });

  // ============================================
  // TEST 2: Form Validation - Password
  // ============================================
  describe('Form Validation - Password', () => {
    it('should mark password as invalid when empty', () => {
      // Arrange
      const passwordControl = component.loginForm.get('password');

      // Act
      passwordControl?.setValue('');
      passwordControl?.markAsTouched();

      // Assert
      expect(passwordControl?.invalid).toBe(true);
      expect(passwordControl?.hasError('required')).toBe(true);
    });

    it('should mark password as invalid when length is less than 8 characters', () => {
      // Arrange
      const passwordControl = component.loginForm.get('password');

      // Act
      const shortPasswords = ['1234567', 'Pass12!', 'abc'];

      shortPasswords.forEach(shortPassword => {
        passwordControl?.setValue(shortPassword);
        passwordControl?.markAsTouched();

        // Assert
        expect(passwordControl?.invalid).toBe(true, `Password "${shortPassword}" should be invalid`);
        expect(passwordControl?.hasError('minlength')).toBe(true, `Password "${shortPassword}" should have minlength error`);
      });
    });

    it('should mark password as valid when length is 8 or more characters', () => {
      // Arrange
      const passwordControl = component.loginForm.get('password');

      // Act
      const validPasswords = [
        'Password123!',
        '12345678',
        'LongPassword'
      ];

      validPasswords.forEach(validPassword => {
        passwordControl?.setValue(validPassword);
        passwordControl?.markAsTouched();

        // Assert
        expect(passwordControl?.valid).toBe(true, `Password "${validPassword}" should be valid`);
      });
    });
  });

  // ============================================
  // TEST 3: Form Validation - Overall Form
  // ============================================
  describe('Form Validation - Overall Form', () => {
    it('should mark form as invalid when both email and password are empty', () => {
      // Arrange
      component.loginForm.get('email')?.setValue('');
      component.loginForm.get('password')?.setValue('');

      // Act
      component.loginForm.markAllAsTouched();

      // Assert
      expect(component.loginForm.invalid).toBe(true);
    });

    it('should mark form as invalid when email is valid but password is empty', () => {
      // Arrange
      component.loginForm.get('email')?.setValue('test@example.com');
      component.loginForm.get('password')?.setValue('');

      // Act
      component.loginForm.markAllAsTouched();

      // Assert
      expect(component.loginForm.invalid).toBe(true);
    });

    it('should mark form as invalid when password is valid but email is empty', () => {
      // Arrange
      component.loginForm.get('email')?.setValue('');
      component.loginForm.get('password')?.setValue('Password123!');

      // Act
      component.loginForm.markAllAsTouched();

      // Assert
      expect(component.loginForm.invalid).toBe(true);
    });

    it('should mark form as valid when both email and password are valid', () => {
      // Arrange
      component.loginForm.get('email')?.setValue('test@example.com');
      component.loginForm.get('password')?.setValue('Password123!');

      // Act
      component.loginForm.markAllAsTouched();

      // Assert
      expect(component.loginForm.valid).toBe(true);
    });

    it('should not submit form when form is invalid', () => {
      // Arrange
      component.loginForm.get('email')?.setValue('invalid-email');
      component.loginForm.get('password')?.setValue('short');

      // Act
      component.onSubmit();

      // Assert
      expect(authServiceSpy.login).not.toHaveBeenCalled();
      // Form controls should be marked as touched after invalid submit
      expect(component.loginForm.get('email')?.touched).toBe(true);
      expect(component.loginForm.get('password')?.touched).toBe(true);
    });

    it('should submit form when form is valid', () => {
      // Arrange
      component.loginForm.get('email')?.setValue('test@example.com');
      component.loginForm.get('password')?.setValue('Password123!');
      authServiceSpy.login.and.returnValue(of({
        accessToken: 'token',
        refreshToken: 'refresh',
        tokenType: 'Bearer',
        expiresIn: 3600,
        user: {
          id: '123',
          email: 'test@example.com',
          displayName: 'Test',
          preferredTheme: 'Normal' as any,
          createdAt: new Date().toISOString()
        }
      }));

      // Act
      component.onSubmit();

      // Assert
      expect(authServiceSpy.login).toHaveBeenCalledWith({
        email: 'test@example.com',
        password: 'Password123!',
        rememberMe: false
      });
    });
  });

  // ============================================
  // TEST 4: Login Behavior
  // ============================================
  describe('Login Behavior', () => {
    it('should set isLoading to true when submitting form', () => {
      // Arrange
      component.loginForm.get('email')?.setValue('test@example.com');
      component.loginForm.get('password')?.setValue('Password123!');
      authServiceSpy.login.and.returnValue(of({
        accessToken: 'token',
        refreshToken: 'refresh',
        tokenType: 'Bearer',
        expiresIn: 3600,
        user: {
          id: '123',
          email: 'test@example.com',
          displayName: 'Test',
          preferredTheme: 'Normal' as any,
          createdAt: new Date().toISOString()
        }
      }));

      // Act
      component.onSubmit();

      // Assert
      expect(component.isLoading).toBe(true);
    });

    it('should navigate to dashboard on successful login', fakeAsync(() => {
      // Arrange
      component.loginForm.get('email')?.setValue('test@example.com');
      component.loginForm.get('password')?.setValue('Password123!');
      authServiceSpy.login.and.returnValue(of({
        accessToken: 'token',
        refreshToken: 'refresh',
        tokenType: 'Bearer',
        expiresIn: 3600,
        user: {
          id: '123',
          email: 'test@example.com',
          displayName: 'Test',
          preferredTheme: 'Normal' as any,
          createdAt: new Date().toISOString()
        }
      }));

      // Act
      component.onSubmit();
      tick();

      // Assert
      expect(routerSpy.navigateByUrl).toHaveBeenCalledWith('/dashboard');
    }));

    it('should display error message on failed login', fakeAsync(() => {
      // Arrange
      component.loginForm.get('email')?.setValue('test@example.com');
      component.loginForm.get('password')?.setValue('WrongPassword!');
      authServiceSpy.login.and.returnValue(
        throwError(() => new Error('Invalid email or password'))
      );

      // Act
      component.onSubmit();
      tick();

      // Assert
      expect(component.errorMessage).toBe('Invalid email or password');
      expect(component.isLoading).toBe(false);
    }));
  });

  // ============================================
  // TEST 5: Password Visibility Toggle
  // ============================================
  describe('Password Visibility', () => {
    it('should toggle password visibility', () => {
      // Arrange
      expect(component.showPassword).toBe(false);

      // Act
      component.togglePasswordVisibility();

      // Assert
      expect(component.showPassword).toBe(true);

      // Act again
      component.togglePasswordVisibility();

      // Assert
      expect(component.showPassword).toBe(false);
    });
  });
});
