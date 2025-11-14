import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Output, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../features/auth/services/auth.service';
import { User, ThemeMode as BackendThemeMode } from '../../features/auth/models/user.model';

type Language = 'uk' | 'en' | 'pl';
type ThemeMode = 'normal' | 'low-stimulus';

interface LanguageOption {
  code: Language;
  flag: string;
  name: string;
  nativeName: string;
}

@Component({
  selector: 'app-navbar',
  imports: [CommonModule],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss']
})
export class NavbarComponent implements OnInit {
  @Output() moduleChange = new EventEmitter<string>();

  private router = inject(Router);
  private authService = inject(AuthService);

  activeModule: string = 'overview';
  currentUser: User | null = null;
  showUserMenu = false;
  showLanguageMenu = false;
  currentLanguage: Language = 'uk';
  currentTheme: ThemeMode = 'normal';

  readonly languages: LanguageOption[] = [
    { code: 'uk', flag: '🇺🇦', name: 'Ukrainian', nativeName: 'Українська' },
    { code: 'en', flag: '🇬🇧', name: 'English', nativeName: 'English' },
    { code: 'pl', flag: '🇵🇱', name: 'Polish', nativeName: 'Polski' }
  ];

  readonly modules = [
    { id: 'overview', icon: 'target', label: 'Огляд', route: '/home' },
    { id: 'notes', icon: 'file-text', label: 'Нотатки', route: '/notes' },
    { id: 'tasks', icon: 'check-square', label: 'Завдання', route: '/tasks' },
    { id: 'finance', icon: 'dollar-sign', label: 'Фінанси', route: '/finance' },
    { id: 'archive', icon: 'archive', label: 'Архів', route: '/archive' },
  ];

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;

      // Load theme from user profile
      if (user?.preferredTheme) {
        this.currentTheme = user.preferredTheme === 'Normal' ? 'normal' : 'low-stimulus';
      }
    });

    // Load saved language from localStorage
    const savedLang = localStorage.getItem('app-language') as Language;
    if (savedLang && this.languages.find(l => l.code === savedLang)) {
      this.currentLanguage = savedLang;
    }
  }

  toggleTheme(): void {
    if (!this.currentUser) return;

    const newTheme: ThemeMode = this.currentTheme === 'normal' ? 'low-stimulus' : 'normal';
    this.currentTheme = newTheme;

    // Update user profile with new theme
    const backendTheme = newTheme === 'normal' ? BackendThemeMode.Normal : BackendThemeMode.LowStimulus;

    this.authService.updateProfile({
      displayName: this.currentUser.displayName,
      preferredTheme: backendTheme
    }).subscribe({
      next: () => {
        console.log('Theme changed to:', newTheme);
        // TODO: Apply theme changes to the UI
      },
      error: (err) => {
        console.error('Failed to update theme:', err);
        // Revert on error
        this.currentTheme = newTheme === 'normal' ? 'low-stimulus' : 'normal';
      }
    });
  }

  setActiveModule(moduleId: string): void {
    this.activeModule = moduleId;
    this.moduleChange.emit(moduleId);

    const module = this.modules.find(m => m.id === moduleId);
    if (module?.route) {
      this.router.navigate([module.route]);
    }
  }

  toggleLanguageMenu(): void {
    this.showLanguageMenu = !this.showLanguageMenu;
    if (this.showLanguageMenu) {
      this.showUserMenu = false;
    }
  }

  closeLanguageMenu(): void {
    this.showLanguageMenu = false;
  }

  selectLanguage(lang: Language): void {
    this.currentLanguage = lang;
    localStorage.setItem('app-language', lang);
    this.closeLanguageMenu();

    // TODO: Implement actual i18n language change
    console.log('Language changed to:', lang);
  }

  toggleUserMenu(): void {
    this.showUserMenu = !this.showUserMenu;
    if (this.showUserMenu) {
      this.showLanguageMenu = false;
    }
  }

  closeUserMenu(): void {
    this.showUserMenu = false;
  }

  navigateToSettings(): void {
    this.closeUserMenu();
    this.router.navigate(['/settings']);
  }

  logout(): void {
    this.closeUserMenu();
    this.authService.logout();
  }

  get userInitials(): string {
    if (!this.currentUser?.displayName) return '?';
    const names = this.currentUser.displayName.split(' ');
    if (names.length >= 2) {
      return (names[0][0] + names[1][0]).toUpperCase();
    }
    return this.currentUser.displayName[0].toUpperCase();
  }

  get selectedLanguage(): LanguageOption {
    return this.languages.find(l => l.code === this.currentLanguage) || this.languages[0];
  }
}
