import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { AuthService } from '../../features/auth/services/auth.service';
import { ThemeMode as BackendThemeMode } from '../../features/auth/models/user.model';

export type ThemeMode = 'normal' | 'low-stimulus';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private authService = inject(AuthService);
  private currentThemeSubject = new BehaviorSubject<ThemeMode>('normal');
  public currentTheme$ = this.currentThemeSubject.asObservable();

  private readonly THEME_STORAGE_KEY = 'app-theme';

  constructor() {
    this.initializeTheme();
  }

  /**
   * Initialize theme from localStorage or user profile
   */
  private initializeTheme(): void {
    // First, try to load from localStorage
    const savedTheme = localStorage.getItem(this.THEME_STORAGE_KEY) as ThemeMode;
    if (savedTheme && (savedTheme === 'normal' || savedTheme === 'low-stimulus')) {
      this.applyTheme(savedTheme);
    }

    // Then sync with user profile when available
    this.authService.currentUser$.subscribe(user => {
      if (user?.preferredTheme) {
        const theme = user.preferredTheme === BackendThemeMode.Normal ? 'normal' : 'low-stimulus';
        this.applyTheme(theme);
      }
    });
  }

  /**
   * Set theme mode and sync with backend
   */
  setTheme(mode: ThemeMode): void {
    // Apply theme immediately
    this.applyTheme(mode);

    // Save to localStorage
    localStorage.setItem(this.THEME_STORAGE_KEY, mode);

    // Sync with backend if user is logged in
    const currentUser = this.authService.getCurrentUserValue();
    if (currentUser) {
      const backendTheme = mode === 'normal' ? BackendThemeMode.Normal : BackendThemeMode.LowStimulus;

      this.authService.updateProfile({
        displayName: currentUser.displayName,
        preferredTheme: backendTheme
      }).subscribe({
        next: () => {
          console.log('Theme synced with backend:', mode);
        },
        error: (err) => {
          console.error('Failed to sync theme with backend:', err);
        }
      });
    }
  }

  /**
   * Apply theme to the DOM
   */
  private applyTheme(mode: ThemeMode): void {
    const body = document.body;

    // Remove existing theme classes
    body.classList.remove('theme-normal', 'theme-low-stimulus');

    // Add new theme class
    body.classList.add(`theme-${mode}`);

    // Update root element as well for CSS variables
    const root = document.documentElement;
    root.classList.remove('theme-normal', 'theme-low-stimulus');
    root.classList.add(`theme-${mode}`);

    // Update subject
    this.currentThemeSubject.next(mode);
  }

  /**
   * Get current theme
   */
  getCurrentTheme(): ThemeMode {
    return this.currentThemeSubject.value;
  }

  /**
   * Toggle between themes
   */
  toggleTheme(): void {
    const newTheme = this.currentThemeSubject.value === 'normal' ? 'low-stimulus' : 'normal';
    this.setTheme(newTheme);
  }

  /**
   * Read the computed value of a CSS variable on :root
   */
  getCssVarValue(varName: string, fallback = ''): string {
    if (typeof window === 'undefined') {
      return fallback;
    }

    const value = getComputedStyle(document.documentElement).getPropertyValue(varName);
    return value?.trim() || fallback;
  }
}
