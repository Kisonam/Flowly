import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../../auth/services/auth.service';
import { ThemeMode as BackendThemeMode } from '../../../auth/models/user.model';
import { ThemeService, ThemeMode } from '../../../../core/services/theme.service';

@Component({
  selector: 'app-theme-settings',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  templateUrl: './theme-settings.component.html',
  styleUrls: ['./theme-settings.component.scss']
})
export class ThemeSettingsComponent implements OnInit {
  private authService = inject(AuthService);
  private themeService = inject(ThemeService);

  selectedTheme: ThemeMode = 'normal';
  isLoading = false;
  message = '';
  error = '';

  readonly themes = [
    {
      id: 'normal' as ThemeMode,
      name: 'SETTINGS.THEME.NORMAL.NAME',
      description: 'SETTINGS.THEME.NORMAL.DESCRIPTION',
      preview: 'linear-gradient(135deg, #f8fafc 0%, #e2e8f0 100%)'
    },
    {
      id: 'low-stimulus' as ThemeMode,
      name: 'SETTINGS.THEME.LOW_STIMULUS.NAME',
      description: 'SETTINGS.THEME.LOW_STIMULUS.DESCRIPTION',
      preview: 'linear-gradient(135deg, #e9ecef 0%, #dee2e6 100%)'
    }
  ];

  ngOnInit(): void {
    this.loadCurrentTheme();
  }

  private loadCurrentTheme(): void {
    
    this.themeService.currentTheme$.subscribe(theme => {
      this.selectedTheme = theme;
    });
  }

  selectTheme(themeId: ThemeMode): void {
    if (themeId === this.selectedTheme) {
      return;
    }

    this.themeService.setTheme(themeId);
  }

  getSelectedThemePreview(): string {
    const theme = this.themes.find(t => t.id === this.selectedTheme);
    return theme?.preview || '';
  }

  getSelectedThemeName(): string {
    const theme = this.themes.find(t => t.id === this.selectedTheme);
    return theme?.name || '';
  }
}
