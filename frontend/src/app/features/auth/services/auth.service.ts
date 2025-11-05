// frontend/src/app/features/auth/services/auth.service.ts

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap, catchError, throwError, map } from 'rxjs';
import { Router } from '@angular/router';
import {
  LoginRequest,
  RegisterRequest,
  AuthResponse,
  RefreshTokenRequest,
  UpdateProfileRequest,
  ChangePasswordRequest,
  GoogleLoginRequest
} from '../models/auth.models';
import { User } from '../models/user.model';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  private readonly API_URL = `${environment.apiUrl}/auth`;
  private readonly TOKEN_KEY = 'flowly_access_token';
  private readonly REFRESH_TOKEN_KEY = 'flowly_refresh_token';
  private readonly USER_KEY = 'flowly_user';

  private currentUserSubject = new BehaviorSubject<User | null>(this.getUserFromStorage());
  public currentUser$ = this.currentUserSubject.asObservable();

  // BehaviorSubject для статусу автентифікації
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(this.hasValidToken());
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  constructor() {
    this.initializeAuth();
  }

  private initializeAuth(): void {
    const token = this.getAccessToken();
    const refreshToken = this.getRefreshToken();

    if (!token || !refreshToken) {
      this.clearStorage();
      this.isAuthenticatedSubject.next(false);
      return;
    }

    // Check if access token is valid
    if (this.hasValidToken()) {
      // Token is valid, set authenticated state
      this.isAuthenticatedSubject.next(true);

      // Load user info from storage or server
      const cachedUser = this.getUserFromStorage();
      if (cachedUser) {
        this.currentUserSubject.next(cachedUser);
      }
    } else {
      // Access token expired, try to refresh
      this.refreshToken().subscribe({
        next: () => {
          console.log('✅ Token refreshed on app initialization');
        },
        error: () => {
          console.log('❌ Failed to refresh token, logging out');
          this.clearStorage();
          this.isAuthenticatedSubject.next(false);
        }
      });
    }
  }

  // Authentication Methods


  /**
   * Register new user
   */
  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.API_URL}/register`, request)
      .pipe(
        tap(response => this.handleAuthSuccess(response)),
        catchError(this.handleError)
      );
  }

  /**
   * Login with email and password
   */
  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.API_URL}/login`, request)
      .pipe(
        tap(response => this.handleAuthSuccess(response)),
        catchError(this.handleError)
      );
  }

  /**
   * Login with Google
   */
  loginWithGoogle(idToken: string): Observable<AuthResponse> {
    const request: GoogleLoginRequest = { idToken };
    return this.http.post<AuthResponse>(`${this.API_URL}/google`, request)
      .pipe(
        tap(response => this.handleAuthSuccess(response)),
        catchError(this.handleError)
      );
  }

  /**
   * Logout user
   */
  logout(): void {
    const refreshToken = this.getRefreshToken();

    if (refreshToken) {
      // Revoke token on server
      this.http.post(`${this.API_URL}/revoke`, { refreshToken })
        .subscribe({
          next: () => console.log('Token revoked on server'),
          error: (err) => console.error('Failed to revoke token', err)
        });
    }

    this.clearStorage();
    this.currentUserSubject.next(null);
    this.isAuthenticatedSubject.next(false);
    this.router.navigate(['/auth/login']);
  }

  /**
   * Refresh access token
   */
  refreshToken(): Observable<AuthResponse> {
    const accessToken = this.getAccessToken();
    const refreshToken = this.getRefreshToken();

    if (!accessToken || !refreshToken) {
      return throwError(() => new Error('No tokens available'));
    }

    const request: RefreshTokenRequest = { accessToken, refreshToken };

    return this.http.post<AuthResponse>(`${this.API_URL}/refresh`, request)
      .pipe(
        tap(response => this.handleAuthSuccess(response)),
        catchError(err => {
          this.logout();
          return throwError(() => err);
        })
      );
  }

  // Profile Methods

  /**
   * Get current user profile
   */
  getCurrentUser(): Observable<User> {
    return this.http.get<User>(`${this.API_URL}/me`)
      .pipe(
        tap(user => {
          this.setUser(user);
          this.currentUserSubject.next(user);
        }),
        catchError(this.handleError)
      );
  }

  /**
   * Update user profile
   */
  updateProfile(request: UpdateProfileRequest): Observable<User> {
    return this.http.put<User>(`${this.API_URL}/profile`, request)
      .pipe(
        tap(user => {
          this.setUser(user);
          this.currentUserSubject.next(user);
        }),
        catchError(this.handleError)
      );
  }

  /**
   * Change password
   */
  changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.API_URL}/change-password`, request)
      .pipe(catchError(this.handleError));
  }

  /**
   * Upload avatar
   */
  uploadAvatar(file: File): Observable<{ avatarUrl: string }> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<{ avatarUrl: string }>(`${this.API_URL}/avatar`, formData)
      .pipe(
        tap(response => {
          const user = this.currentUserSubject.value;
          if (user) {
            user.avatarUrl = response.avatarUrl;
            this.setUser(user);
            this.currentUserSubject.next(user);
          }
        }),
        catchError(this.handleError)
      );
  }

  /**
   * Delete avatar
   */
  deleteAvatar(): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/avatar`)
      .pipe(
        tap(() => {
          const user = this.currentUserSubject.value;
          if (user) {
            user.avatarUrl = undefined;
            this.setUser(user);
            this.currentUserSubject.next(user);
          }
        }),
        catchError(this.handleError)
      );
  }

  // ============================================
  // Token Management
  // ============================================

  /**
   * Get access token
   */
  getAccessToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  /**
   * Get refresh token
   */
  getRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  /**
   * Check if user is authenticated
   */
  isAuthenticated(): boolean {
    return this.hasValidToken();
  }

  /**
   * Get current user value (synchronous)
   */
  getCurrentUserValue(): User | null {
    return this.currentUserSubject.value;
  }

  // ============================================
  // Private Helper Methods
  // ============================================

  private handleAuthSuccess(response: AuthResponse): void {
    // Save tokens
    localStorage.setItem(this.TOKEN_KEY, response.accessToken);
    localStorage.setItem(this.REFRESH_TOKEN_KEY, response.refreshToken);

    // Save user
    this.setUser(response.user);

    // Update observables
    this.currentUserSubject.next(response.user);
    this.isAuthenticatedSubject.next(true);

    console.log('✅ Authentication successful', response.user);
  }

  private setUser(user: User): void {
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
  }

  private getUserFromStorage(): User | null {
    const userJson = localStorage.getItem(this.USER_KEY);
    return userJson ? JSON.parse(userJson) : null;
  }

  private hasValidToken(): boolean {
    const token = this.getAccessToken();
    if (!token) return false;

    try {
      // Decode JWT to check expiration
      const payload = JSON.parse(atob(token.split('.')[1]));
      const exp = payload.exp * 1000; // Convert to milliseconds
      // Add 5 second buffer to prevent edge cases
      return Date.now() < (exp - 5000);
    } catch {
      return false;
    }
  }

  private clearStorage(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
  }

  private handleError(error: any): Observable<never> {
    console.error('❌ Auth error:', error);

    let errorMessage = 'An error occurred';

    if (error.error?.message) {
      errorMessage = error.error.message;
    } else if (error.message) {
      errorMessage = error.message;
    }

    return throwError(() => new Error(errorMessage));
  }
}
