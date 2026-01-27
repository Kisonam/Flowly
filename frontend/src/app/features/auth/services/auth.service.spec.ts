

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
    preferredTheme: 'Normal' as any, 
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

    localStorage.clear();
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  describe('login', () => {
    it('should store access token and refresh token in localStorage after successful login', (done) => {
      
      const loginRequest: LoginRequest = {
        email: 'test@example.com',
        password: 'Password123!'
      };

      service.login(loginRequest).subscribe({
        next: (response) => {
          
          expect(response).toEqual(mockAuthResponse);

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

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(loginRequest);
      req.flush(mockAuthResponse);
    });

    it('should update currentUser$ observable after successful login', (done) => {
      
      const loginRequest: LoginRequest = {
        email: 'test@example.com',
        password: 'Password123!'
      };

      service.currentUser$.subscribe(user => {
        if (user) {
          
          expect(user.email).toBe(mockUser.email);
          expect(user.displayName).toBe(mockUser.displayName);
          done();
        }
      });

      service.login(loginRequest).subscribe();

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
      req.flush(mockAuthResponse);
    });
  });

  describe('logout', () => {
    it('should clear all tokens and user data from localStorage', () => {
      
      localStorage.setItem('flowly_access_token', 'mock-access-token');
      localStorage.setItem('flowly_refresh_token', 'mock-refresh-token');
      localStorage.setItem('flowly_user', JSON.stringify(mockUser));

      expect(localStorage.getItem('flowly_access_token')).toBeTruthy();
      expect(localStorage.getItem('flowly_refresh_token')).toBeTruthy();
      expect(localStorage.getItem('flowly_user')).toBeTruthy();

      service.logout();

      const requests = httpMock.match(`${environment.apiUrl}/auth/revoke`);
      requests.forEach(req => req.flush({}));

      expect(localStorage.getItem('flowly_access_token')).toBeNull();
      expect(localStorage.getItem('flowly_refresh_token')).toBeNull();
      expect(localStorage.getItem('flowly_user')).toBeNull();
    });

    it('should set currentUser$ to null after logout', (done) => {
      
      localStorage.setItem('flowly_access_token', 'mock-access-token');
      localStorage.setItem('flowly_refresh_token', 'mock-refresh-token');
      localStorage.setItem('flowly_user', JSON.stringify(mockUser));

      service.logout();

      service.currentUser$.subscribe(user => {
        expect(user).toBeNull();
        done();
      });

      const requests = httpMock.match(`${environment.apiUrl}/auth/revoke`);
      requests.forEach(req => req.flush({}));
    });

    it('should navigate to login page after logout', () => {
      
      localStorage.setItem('flowly_access_token', 'mock-access-token');
      localStorage.setItem('flowly_refresh_token', 'mock-refresh-token');

      service.logout();

      expect(routerSpy.navigate).toHaveBeenCalledWith(['/auth/login']);

      const requests = httpMock.match(`${environment.apiUrl}/auth/revoke`);
      requests.forEach(req => req.flush({}));
    });
  });

  describe('getCurrentUser', () => {
    it('should fetch user from API and return user data', (done) => {
      
      service.getCurrentUser().subscribe({
        next: (user) => {
          
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

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/me`);
      expect(req.request.method).toBe('GET');
      req.flush(mockUser);
    });

    it('should update currentUser$ observable after fetching user', (done) => {
      
      let emissionCount = 0;
      service.currentUser$.subscribe(user => {
        emissionCount++;
        if (emissionCount === 2 && user) {
          
          expect(user.email).toBe(mockUser.email);
          expect(user.displayName).toBe(mockUser.displayName);
          done();
        }
      });

      service.getCurrentUser().subscribe();

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/me`);
      req.flush(mockUser);
    });

    it('should store user in localStorage after fetching', (done) => {
      
      service.getCurrentUser().subscribe({
        next: () => {
          
          const userJson = localStorage.getItem('flowly_user');
          expect(userJson).toBeTruthy();

          const storedUser = JSON.parse(userJson!);
          expect(storedUser.email).toBe(mockUser.email);
          expect(storedUser.displayName).toBe(mockUser.displayName);
          done();
        }
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/me`);
      req.flush(mockUser);
    });
  });

  describe('getAccessToken', () => {
    it('should return access token from localStorage', () => {
      
      localStorage.setItem('flowly_access_token', 'test-token');

      const token = service.getAccessToken();

      expect(token).toBe('test-token');
    });

    it('should return null if no token exists', () => {
      
      const token = service.getAccessToken();

      expect(token).toBeNull();
    });
  });

  describe('isAuthenticated', () => {
    it('should return false when no token exists', () => {
      
      const isAuth = service.isAuthenticated();

      expect(isAuth).toBe(false);
    });

    it('should return false when token is expired', () => {
      
      const expiredToken = createMockJwt({ exp: Math.floor(Date.now() / 1000) - 3600 }); 
      localStorage.setItem('flowly_access_token', expiredToken);

      const isAuth = service.isAuthenticated();

      expect(isAuth).toBe(false);
    });

    it('should return true when token is valid', () => {
      
      const validToken = createMockJwt({ exp: Math.floor(Date.now() / 1000) + 3600 }); 
      localStorage.setItem('flowly_access_token', validToken);

      const isAuth = service.isAuthenticated();

      expect(isAuth).toBe(true);
    });
  });
});

function createMockJwt(payload: any): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify(payload));
  const signature = 'mock-signature';
  return `${header}.${body}.${signature}`;
}
