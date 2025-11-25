// frontend/src/app/features/auth/services/auth.service.spec.ts

import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { LoginRequest, AuthResponse } from '../models/auth.models';
import { User } from '../models/user.model';
import { environment } from '../../../../environments/environment';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let routerSpy: jasmine.SpyObj<Router>;

  const mockUser: User = {
    id: '123e4567-e89b-12d3-a456-426614174000',
    email: 'test@example.com',
    displayName: 'Test User',
    preferredTheme: 'Normal' as any, // ThemeMode enum
    createdAt: new Date().toISOString()
  };

  const mockAuthResponse: AuthResponse = {
    accessToken: 'mock-access-token',
    refreshToken: 'mock-refresh-token',
    tokenType: 'Bearer',
    expiresIn: 3600,
    user: mockUser
  };

  beforeEach(() => {
    // Create spy for Router
    const routerSpyObj = jasmine.createSpyObj('Router', ['navigate', 'navigateByUrl']);

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        AuthService,
        { provide: Router, useValue: routerSpyObj }
      ]
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
    routerSpy = TestBed.inject(Router) as jasmine.SpyObj<Router>;

    // Clear localStorage before each test
    localStorage.clear();
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  // ============================================
  // TEST 1: Login stores token
  // ============================================
  describe('login', () => {
    it('should store access token and refresh token in localStorage after successful login', (done) => {
      // Arrange
      const loginRequest: LoginRequest = {
        email: 'test@example.com',
        password: 'Password123!'
      };

      // Act
      service.login(loginRequest).subscribe({
        next: (response) => {
          // Assert
          expect(response).toEqual(mockAuthResponse);

          // Verify tokens are stored in localStorage
          const accessToken = localStorage.getItem('flowly_access_token');
          const refreshToken = localStorage.getItem('flowly_refresh_token');
          const userJson = localStorage.getItem('flowly_user');

          expect(accessToken).toBe('mock-access-token');
          expect(refreshToken).toBe('mock-refresh-token');
          expect(userJson).toBeTruthy();

          const storedUser = JSON.parse(userJson!);
          expect(storedUser.email).toBe(mockUser.email);
          expect(storedUser.displayName).toBe(mockUser.displayName);

          done();
        },
        error: (error) => {
          fail('Login should not fail: ' + error);
          done();
        }
      });

      // Simulate HTTP response
      const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(loginRequest);
      req.flush(mockAuthResponse);
    });

    it('should update currentUser$ observable after successful login', (done) => {
      // Arrange
      const loginRequest: LoginRequest = {
        email: 'test@example.com',
        password: 'Password123!'
      };

      // Subscribe to currentUser$ before login
      service.currentUser$.subscribe(user => {
        if (user) {
          // Assert
          expect(user.email).toBe(mockUser.email);
          expect(user.displayName).toBe(mockUser.displayName);
          done();
        }
      });

      // Act
      service.login(loginRequest).subscribe();

      // Simulate HTTP response
      const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
      req.flush(mockAuthResponse);
    });
  });

  // ============================================
  // TEST 2: Logout clears token
  // ============================================
  describe('logout', () => {
    it('should clear all tokens and user data from localStorage', () => {
      // Arrange - Set up initial state with tokens
      localStorage.setItem('flowly_access_token', 'mock-access-token');
      localStorage.setItem('flowly_refresh_token', 'mock-refresh-token');
      localStorage.setItem('flowly_user', JSON.stringify(mockUser));

      // Verify initial state
      expect(localStorage.getItem('flowly_access_token')).toBeTruthy();
      expect(localStorage.getItem('flowly_refresh_token')).toBeTruthy();
      expect(localStorage.getItem('flowly_user')).toBeTruthy();

      // Act
      service.logout();

      // Handle the revoke token request (may be sent)
      const requests = httpMock.match(`${environment.apiUrl}/auth/revoke`);
      requests.forEach(req => req.flush({}));

      // Assert - All tokens should be cleared
      expect(localStorage.getItem('flowly_access_token')).toBeNull();
      expect(localStorage.getItem('flowly_refresh_token')).toBeNull();
      expect(localStorage.getItem('flowly_user')).toBeNull();
    });

    it('should set currentUser$ to null after logout', (done) => {
      // Arrange - Set up initial state
      localStorage.setItem('flowly_access_token', 'mock-access-token');
      localStorage.setItem('flowly_refresh_token', 'mock-refresh-token');
      localStorage.setItem('flowly_user', JSON.stringify(mockUser));

      // Act
      service.logout();

      // Assert - currentUser$ should emit null
      service.currentUser$.subscribe(user => {
        expect(user).toBeNull();
        done();
      });

      // Handle the revoke token request (optional, may fail silently)
      const requests = httpMock.match(`${environment.apiUrl}/auth/revoke`);
      requests.forEach(req => req.flush({}));
    });

    it('should navigate to login page after logout', () => {
      // Arrange
      localStorage.setItem('flowly_access_token', 'mock-access-token');
      localStorage.setItem('flowly_refresh_token', 'mock-refresh-token');

      // Act
      service.logout();

      // Assert
      expect(routerSpy.navigate).toHaveBeenCalledWith(['/auth/login']);

      // Handle the revoke token request
      const requests = httpMock.match(`${environment.apiUrl}/auth/revoke`);
      requests.forEach(req => req.flush({}));
    });
  });

  // ============================================
  // TEST 3: getCurrentUser returns user
  // ============================================
  describe('getCurrentUser', () => {
    it('should fetch user from API and return user data', (done) => {
      // Act
      service.getCurrentUser().subscribe({
        next: (user) => {
          // Assert
          expect(user).toEqual(mockUser);
          expect(user.email).toBe('test@example.com');
          expect(user.displayName).toBe('Test User');
          done();
        },
        error: (error) => {
          fail('getCurrentUser should not fail: ' + error);
          done();
        }
      });

      // Simulate HTTP response
      const req = httpMock.expectOne(`${environment.apiUrl}/auth/me`);
      expect(req.request.method).toBe('GET');
      req.flush(mockUser);
    });

    it('should update currentUser$ observable after fetching user', (done) => {
      // Subscribe to currentUser$ before fetching
      let emissionCount = 0;
      service.currentUser$.subscribe(user => {
        emissionCount++;
        if (emissionCount === 2 && user) {
          // First emission is null (initial), second is the fetched user
          expect(user.email).toBe(mockUser.email);
          expect(user.displayName).toBe(mockUser.displayName);
          done();
        }
      });

      // Act
      service.getCurrentUser().subscribe();

      // Simulate HTTP response
      const req = httpMock.expectOne(`${environment.apiUrl}/auth/me`);
      req.flush(mockUser);
    });

    it('should store user in localStorage after fetching', (done) => {
      // Act
      service.getCurrentUser().subscribe({
        next: () => {
          // Assert
          const userJson = localStorage.getItem('flowly_user');
          expect(userJson).toBeTruthy();

          const storedUser = JSON.parse(userJson!);
          expect(storedUser.email).toBe(mockUser.email);
          expect(storedUser.displayName).toBe(mockUser.displayName);
          done();
        }
      });

      // Simulate HTTP response
      const req = httpMock.expectOne(`${environment.apiUrl}/auth/me`);
      req.flush(mockUser);
    });
  });

  // ============================================
  // Additional Helper Method Tests
  // ============================================
  describe('getAccessToken', () => {
    it('should return access token from localStorage', () => {
      // Arrange
      localStorage.setItem('flowly_access_token', 'test-token');

      // Act
      const token = service.getAccessToken();

      // Assert
      expect(token).toBe('test-token');
    });

    it('should return null if no token exists', () => {
      // Act
      const token = service.getAccessToken();

      // Assert
      expect(token).toBeNull();
    });
  });

  describe('isAuthenticated', () => {
    it('should return false when no token exists', () => {
      // Act
      const isAuth = service.isAuthenticated();

      // Assert
      expect(isAuth).toBe(false);
    });

    it('should return false when token is expired', () => {
      // Arrange - Create expired JWT token
      const expiredToken = createMockJwt({ exp: Math.floor(Date.now() / 1000) - 3600 }); // Expired 1 hour ago
      localStorage.setItem('flowly_access_token', expiredToken);

      // Act
      const isAuth = service.isAuthenticated();

      // Assert
      expect(isAuth).toBe(false);
    });

    it('should return true when token is valid', () => {
      // Arrange - Create valid JWT token
      const validToken = createMockJwt({ exp: Math.floor(Date.now() / 1000) + 3600 }); // Expires in 1 hour
      localStorage.setItem('flowly_access_token', validToken);

      // Act
      const isAuth = service.isAuthenticated();

      // Assert
      expect(isAuth).toBe(true);
    });
  });
});

// ============================================
// Helper Functions
// ============================================

/**
 * Create a mock JWT token for testing
 */
function createMockJwt(payload: any): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify(payload));
  const signature = 'mock-signature';
  return `${header}.${body}.${signature}`;
}
