

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

    googleAuthServiceSpy.initialize.and.returnValue(Promise.resolve());
    googleAuthServiceSpy.getCredentialResponse.and.returnValue(of('mock-credential'));
    googleAuthServiceSpy.isAvailable.and.returnValue(false);

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Form Validation - Email', () => {
    it('should mark email as invalid when empty', () => {
      
      const emailControl = component.loginForm.get('email');

      emailControl?.setValue('');
      emailControl?.markAsTouched();

      expect(emailControl?.invalid).toBe(true);
      expect(emailControl?.hasError('required')).toBe(true);
    });

    it('should mark email as invalid when format is incorrect', () => {
      
      const emailControl = component.loginForm.get('email');

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

        expect(emailControl?.invalid).toBe(true, `Email "${invalidEmail}" should be invalid`);
        expect(emailControl?.hasError('email')).toBe(true, `Email "${invalidEmail}" should have email error`);
      });
    });

    it('should mark email as valid when format is correct', () => {
      
      const emailControl = component.loginForm.get('email');

      const validEmails = [
        'test@example.com',
        'user.name@domain.co.uk',
        'user+tag@example.com',
        'test123@test-domain.com'
      ];

      validEmails.forEach(validEmail => {
        emailControl?.setValue(validEmail);
        emailControl?.markAsTouched();

        expect(emailControl?.valid).toBe(true, `Email "${validEmail}" should be valid`);
        expect(emailControl?.hasError('email')).toBe(false, `Email "${validEmail}" should not have email error`);
      });
    });
  });

  describe('Form Validation - Password', () => {
    it('should mark password as invalid when empty', () => {
      
      const passwordControl = component.loginForm.get('password');

      passwordControl?.setValue('');
      passwordControl?.markAsTouched();

      expect(passwordControl?.invalid).toBe(true);
      expect(passwordControl?.hasError('required')).toBe(true);
    });

    it('should mark password as invalid when length is less than 8 characters', () => {
      
      const passwordControl = component.loginForm.get('password');

      const shortPasswords = ['1234567', 'Pass12!', 'abc'];

      shortPasswords.forEach(shortPassword => {
        passwordControl?.setValue(shortPassword);
        passwordControl?.markAsTouched();

        expect(passwordControl?.invalid).toBe(true, `Password "${shortPassword}" should be invalid`);
        expect(passwordControl?.hasError('minlength')).toBe(true, `Password "${shortPassword}" should have minlength error`);
      });
    });

    it('should mark password as valid when length is 8 or more characters', () => {
      
      const passwordControl = component.loginForm.get('password');

      const validPasswords = [
        'Password123!',
        '12345678',
        'LongPassword'
      ];

      validPasswords.forEach(validPassword => {
        passwordControl?.setValue(validPassword);
        passwordControl?.markAsTouched();

        expect(passwordControl?.valid).toBe(true, `Password "${validPassword}" should be valid`);
      });
    });
  });

  describe('Form Validation - Overall Form', () => {
    it('should mark form as invalid when both email and password are empty', () => {
      
      component.loginForm.get('email')?.setValue('');
      component.loginForm.get('password')?.setValue('');

      component.loginForm.markAllAsTouched();

      expect(component.loginForm.invalid).toBe(true);
    });

    it('should mark form as invalid when email is valid but password is empty', () => {
      
      component.loginForm.get('email')?.setValue('test@example.com');
      component.loginForm.get('password')?.setValue('');

      component.loginForm.markAllAsTouched();

      expect(component.loginForm.invalid).toBe(true);
    });

    it('should mark form as invalid when password is valid but email is empty', () => {
      
      component.loginForm.get('email')?.setValue('');
      component.loginForm.get('password')?.setValue('Password123!');

      component.loginForm.markAllAsTouched();

      expect(component.loginForm.invalid).toBe(true);
    });

    it('should mark form as valid when both email and password are valid', () => {
      
      component.loginForm.get('email')?.setValue('test@example.com');
      component.loginForm.get('password')?.setValue('Password123!');

      component.loginForm.markAllAsTouched();

      expect(component.loginForm.valid).toBe(true);
    });

    it('should not submit form when form is invalid', () => {
      
      component.loginForm.get('email')?.setValue('invalid-email');
      component.loginForm.get('password')?.setValue('short');

      component.onSubmit();

      expect(authServiceSpy.login).not.toHaveBeenCalled();
      
      expect(component.loginForm.get('email')?.touched).toBe(true);
      expect(component.loginForm.get('password')?.touched).toBe(true);
    });

    it('should submit form when form is valid', () => {
      
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

      component.onSubmit();

      expect(authServiceSpy.login).toHaveBeenCalledWith({
        email: 'test@example.com',
        password: 'Password123!',
        rememberMe: false
      });
    });
  });

  describe('Login Behavior', () => {
    it('should set isLoading to true when submitting form', () => {
      
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

      component.onSubmit();

      expect(component.isLoading).toBe(true);
    });

    it('should navigate to dashboard on successful login', fakeAsync(() => {
      
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

      component.onSubmit();
      tick();

      expect(routerSpy.navigateByUrl).toHaveBeenCalledWith('/dashboard');
    }));

    it('should display error message on failed login', fakeAsync(() => {
      
      component.loginForm.get('email')?.setValue('test@example.com');
      component.loginForm.get('password')?.setValue('WrongPassword!');
      authServiceSpy.login.and.returnValue(
        throwError(() => new Error('Invalid email or password'))
      );

      component.onSubmit();
      tick();

      expect(component.errorMessage).toBe('Invalid email or password');
      expect(component.isLoading).toBe(false);
    }));
  });

  describe('Password Visibility', () => {
    it('should toggle password visibility', () => {
      
      expect(component.showPassword).toBe(false);

      component.togglePasswordVisibility();

      expect(component.showPassword).toBe(true);

      component.togglePasswordVisibility();

      expect(component.showPassword).toBe(false);
    });
  });
});
