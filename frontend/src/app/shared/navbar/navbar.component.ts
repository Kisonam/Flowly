import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Output, inject, OnInit, HostListener, OnDestroy } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../features/auth/services/auth.service';
import { User } from '../../features/auth/models/user.model';
import { ThemeService, ThemeMode } from '../../core/services/theme.service';
import { LocaleService, SupportedLocale } from '../../core/services/locale.service';
import { filter, Subject, takeUntil } from 'rxjs';

type Language = SupportedLocale;

interface LanguageOption {
  code: Language;
  flag: string;
  name: string;
  nativeName: string;
}

@Component({
  selector: 'app-navbar',
  imports: [CommonModule, TranslateModule],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss']
})
export class NavbarComponent implements OnInit, OnDestroy {
  @Output() moduleChange = new EventEmitter<string>();

  private router = inject(Router);
  private authService = inject(AuthService);
  private themeService = inject(ThemeService);
  private localeService = inject(LocaleService);
  private translate = inject(TranslateService);
  private destroy$ = new Subject<void>();

  activeModule: string = 'overview';
  currentUser: User | null = null;
  showUserMenu = false;
  showLanguageMenu = false;
  showCreateMenu = false;
  currentLanguage: Language = 'en';
  currentTheme: ThemeMode = 'normal';

  readonly languages: LanguageOption[] = [
    { code: 'uk', flag: '🇺🇦', name: 'Ukrainian', nativeName: 'Українська' },
    { code: 'en', flag: '🇬🇧', name: 'English', nativeName: 'English' },
    { code: 'pl', flag: '🇵🇱', name: 'Polish', nativeName: 'Polski' }
  ];

  readonly modules = [
    { id: 'overview', icon: 'target', label: 'Overview', route: '/dashboard' },
    { id: 'notes', icon: 'file-text', label: 'Notes', route: '/notes' },
    { id: 'tasks', icon: 'check-square', label: 'Tasks', route: '/tasks' },
    { id: 'finance', icon: 'dollar-sign', label: 'Finance', route: '/finance' },
    { id: 'archive', icon: 'archive', label: 'Archive', route: '/archive' },
  ];

  ngOnInit(): void {
    // Load current user
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
    });

    // Load current theme
    this.themeService.currentTheme$.subscribe(theme => {
      this.currentTheme = theme;
    });

    // Load current language
    this.currentLanguage = this.localeService.getCurrentLocale();

    // Update active module based on current route
    this.updateActiveModuleFromRoute(this.router.url);

    // Listen to route changes
    this.router.events
      .pipe(
        filter(event => event instanceof NavigationEnd),
        takeUntil(this.destroy$)
      )
      .subscribe((event: NavigationEnd) => {
        this.updateActiveModuleFromRoute(event.urlAfterRedirects);
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private updateActiveModuleFromRoute(url: string): void {
    // Sort modules by route length (longest first) to match most specific routes first
    const sortedModules = [...this.modules].sort((a, b) => b.route.length - a.route.length);

    // Find which module matches the current URL
    for (const module of sortedModules) {
      if (url.startsWith(module.route)) {
        this.activeModule = module.id;
        this.moduleChange.emit(module.id);
        return;
      }
    }
    // Default to overview if no match
    this.activeModule = 'overview';
  }

  toggleTheme(): void {
    this.themeService.toggleTheme();
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
    this.localeService.setLocale(lang);
    this.currentLanguage = lang;
    this.closeLanguageMenu();
    // No page reload needed - instant switch!
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
    if (!confirm(this.translate.instant('COMMON.CONFIRM.LOGOUT'))) {
      return;
    }
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

  /**
   * Handle Escape key to close dropdowns
   */
  @HostListener('document:keydown.escape')
  handleEscapeKey(): void {
    if (this.showUserMenu) {
      this.closeUserMenu();
    }
    if (this.showLanguageMenu) {
      this.closeLanguageMenu();
    }
    if (this.showCreateMenu) {
      this.closeCreateMenu();
    }
  }

  /**
   * Mobile create menu methods
   */
  toggleCreateMenu(): void {
    this.showCreateMenu = !this.showCreateMenu;
    if (this.showCreateMenu) {
      this.showUserMenu = false;
      this.showLanguageMenu = false;
    }
  }

  closeCreateMenu(): void {
    this.showCreateMenu = false;
  }

  createNote(): void {
    this.closeCreateMenu();
    this.router.navigate(['/notes/new']);
  }

  createTask(): void {
    this.closeCreateMenu();
    this.router.navigate(['/tasks/new']);
  }

  createTransaction(): void {
    this.closeCreateMenu();
    this.router.navigate(['/finance/transactions/new']);
  }
}
