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

  private initializeTheme(): void {
    
    const savedTheme = localStorage.getItem(this.THEME_STORAGE_KEY) as ThemeMode;
    if (savedTheme && (savedTheme === 'normal' || savedTheme === 'low-stimulus')) {
      this.applyTheme(savedTheme);
    }

    this.authService.currentUser$.subscribe(user => {
      if (user?.preferredTheme) {
        const theme = user.preferredTheme === BackendThemeMode.Normal ? 'normal' : 'low-stimulus';
        this.applyTheme(theme);
      }
    });
  }

  setTheme(mode: ThemeMode): void {
    
    this.applyTheme(mode);

    localStorage.setItem(this.THEME_STORAGE_KEY, mode);

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

  private applyTheme(mode: ThemeMode): void {
    const body = document.body;

    body.classList.remove('theme-normal', 'theme-low-stimulus');

    body.classList.add(`theme-${mode}`);

    const root = document.documentElement;
    root.classList.remove('theme-normal', 'theme-low-stimulus');
    root.classList.add(`theme-${mode}`);

    this.currentThemeSubject.next(mode);
  }

  getCurrentTheme(): ThemeMode {
    return this.currentThemeSubject.value;
  }

  toggleTheme(): void {
    const newTheme = this.currentThemeSubject.value === 'normal' ? 'low-stimulus' : 'normal';
    this.setTheme(newTheme);
  }

  getCssVarValue(varName: string, fallback = ''): string {
    if (typeof window === 'undefined') {
      return fallback;
    }

    const value = getComputedStyle(document.documentElement).getPropertyValue(varName);
    return value?.trim() || fallback;
  }
}
