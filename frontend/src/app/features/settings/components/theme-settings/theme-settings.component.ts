import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../auth/services/auth.service';
import { ThemeMode } from '../../../auth/models/user.model';

@Component({
  selector: 'app-theme-settings',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './theme-settings.component.html',
  styleUrls: ['./theme-settings.component.scss']
})
export class ThemeSettingsComponent implements OnInit {
  private authService = inject(AuthService);

  selectedTheme: ThemeMode = ThemeMode.Normal;
  isLoading = false;
  message = '';
  error = '';

  readonly themes = [
    {
      id: ThemeMode.Normal,
      name: 'Звичайна',
      description: 'Класична світла тема',
      preview: 'linear-gradient(135deg, #f8fafc 0%, #e2e8f0 100%)'
    },
    {
      id: ThemeMode.LowStimulus,
      name: 'Низька стимуляція',
      description: 'Тема з низькою стимуляцією для комфортної роботи',
      preview: 'linear-gradient(135deg, #1e293b 0%, #0f172a 100%)'
    }
  ];

  ngOnInit(): void {
    this.loadCurrentTheme();
  }

  private loadCurrentTheme(): void {
    this.authService.currentUser$.subscribe(user => {
      if (user?.preferredTheme) {
        this.selectedTheme = user.preferredTheme;
      }
    });
  }

  selectTheme(themeId: ThemeMode): void {
    if (themeId === this.selectedTheme) {
      return;
    }

    this.selectedTheme = themeId;
    this.saveTheme();
  }

  private saveTheme(): void {
    this.isLoading = true;
    this.error = '';
    this.message = '';

    const currentUser = this.authService.getCurrentUserValue();
    if (!currentUser) {
      this.error = 'Користувач не знайдений';
      return;
    }

    this.authService.updateProfile({
      displayName: currentUser.displayName,
      preferredTheme: this.selectedTheme
    }).subscribe({
      next: () => {
        this.isLoading = false;
        this.message = 'Тему успішно змінено';
        setTimeout(() => this.message = '', 3000);
      },
      error: (error) => {
        this.isLoading = false;
        this.error = error.message || 'Не вдалося змінити тему';
      }
    });
  }
}
