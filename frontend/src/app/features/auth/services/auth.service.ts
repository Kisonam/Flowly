

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

    if (this.hasValidToken()) {
      
      this.isAuthenticatedSubject.next(true);

      const cachedUser = this.getUserFromStorage();
      if (cachedUser) {
        this.currentUserSubject.next(cachedUser);
      }
    } else {
      
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

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.API_URL}/register`, request)
      .pipe(
        tap(response => this.handleAuthSuccess(response)),
        catchError(this.handleError)
      );
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.API_URL}/login`, request)
      .pipe(
        tap(response => this.handleAuthSuccess(response)),
        catchError(this.handleError)
      );
  }

  loginWithGoogle(idToken: string): Observable<AuthResponse> {
    const request: GoogleLoginRequest = { idToken };
    return this.http.post<AuthResponse>(`${this.API_URL}/google`, request)
      .pipe(
        tap(response => this.handleAuthSuccess(response)),
        catchError(this.handleError)
      );
  }

  logout(): void {
    const refreshToken = this.getRefreshToken();

    if (refreshToken) {
      
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

  changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.API_URL}/change-password`, request)
      .pipe(catchError(this.handleError));
  }

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

  getAccessToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  isAuthenticated(): boolean {
    return this.hasValidToken();
  }

  getCurrentUserValue(): User | null {
    return this.currentUserSubject.value;
  }

  private handleAuthSuccess(response: AuthResponse): void {
    
    localStorage.setItem(this.TOKEN_KEY, response.accessToken);
    localStorage.setItem(this.REFRESH_TOKEN_KEY, response.refreshToken);

    this.setUser(response.user);

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
      
      const payload = JSON.parse(atob(token.split('.')[1]));
      const exp = payload.exp * 1000; 
      
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
