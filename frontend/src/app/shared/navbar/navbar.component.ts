import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Output } from '@angular/core';

@Component({
  selector: 'app-navbar',
  imports: [CommonModule],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss']
})
export class NavbarComponent {
  @Output() themeToggle = new EventEmitter<boolean>();
  @Output() languageChange = new EventEmitter<string>();
  @Output() moduleChange = new EventEmitter<string>();

  isLowStim: boolean = false;
  language: 'en' | 'ua' = 'en';
  activeModule: string = 'overview';

  readonly modules = [
    { id: 'overview', icon: 'target' },
    { id: 'notes', icon: 'file-text' },
    { id: 'tasks', icon: 'check-square' },
    { id: 'finance', icon: 'dollar-sign' }
  ];

  translations = {
    en: {
      appName: 'Flowly',
      overview: 'Overview',
      notes: 'Notes',
      tasks: 'Tasks',
      finance: 'Finance'
    },
    ua: {
      appName: 'Flowly',
      overview: 'Огляд',
      notes: 'Нотатки',
      tasks: 'Завдання',
      finance: 'Фінанси'
    }
  };

  get colors() {
    const standard = {
      primary: '#667eea',
      primaryDark: '#5568d3',
      secondary: '#764ba2',
      accent: '#f093fb',
      accentLight: '#f5b5ff',
      gray100: '#f3f4f6',
      gray600: '#4b5563',
      white: '#ffffff',
    };

    const lowStim = {
      primary: '#6b7280',
      primaryDark: '#4b5563',
      secondary: '#6b7280',
      accent: '#9ca3af',
      accentLight: '#d1d5db',
      gray100: '#f3f4f6',
      gray600: '#4b5563',
      white: '#ffffff',
    };

    return this.isLowStim ? lowStim : standard;
  }

  get t() {
    return this.translations[this.language];
  }

  toggleTheme(): void {
    this.isLowStim = !this.isLowStim;
    this.themeToggle.emit(this.isLowStim);
  }

  toggleLanguage(): void {
    this.language = this.language === 'en' ? 'ua' : 'en';
    this.languageChange.emit(this.language);
  }

  setActiveModule(moduleId: string): void {
    this.activeModule = moduleId;
    this.moduleChange.emit(moduleId);
  }

  getModuleLabel(moduleId: string): string {
    const labels: { [key: string]: string } = {
      overview: this.t.overview,
      notes: this.t.notes,
      tasks: this.t.tasks,
      finance: this.t.finance
    };
    return labels[moduleId] || moduleId;
  }

  getLogoGradient(): string {
    return this.isLowStim
      ? this.colors.primary
      : `linear-gradient(135deg, ${this.colors.primary}, ${this.colors.secondary})`;
  }

  getAvatarGradient(): string {
    return this.isLowStim
      ? this.colors.accentLight
      : `linear-gradient(135deg, ${this.colors.accent}, ${this.colors.accentLight})`;
  }

  getActiveTabStyle(moduleId: string): string {
    return this.activeModule === moduleId ? 'active' : '';
  }

  getTabBackground(moduleId: string): string {
    const isActive = this.activeModule === moduleId;
    if (!isActive) return 'transparent';

    return this.isLowStim
      ? this.colors.primary
      : `linear-gradient(135deg, ${this.colors.primary}, ${this.colors.primaryDark})`;
  }
}
