import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

interface Language {
  code: string;
  name: string;
  nativeName: string;
  flag: string;
}

@Component({
  selector: 'app-language-settings',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './language-settings.component.html',
  styleUrls: ['./language-settings.component.scss']
})
export class LanguageSettingsComponent {
  selectedLanguage = 'uk';
  isLoading = false;
  message = '';
  error = '';

  readonly languages: Language[] = [
    { code: 'uk', name: 'Ukrainian', nativeName: 'Українська', flag: '🇺🇦' },
    { code: 'en', name: 'English', nativeName: 'English', flag: '🇬🇧' },
    { code: 'pl', name: 'Polish', nativeName: 'Polski', flag: '🇵🇱' },
    { code: 'de', name: 'German', nativeName: 'Deutsch', flag: '🇩🇪' }
  ];

  selectLanguage(languageCode: string): void {
    if (languageCode === this.selectedLanguage) {
      return;
    }

    this.selectedLanguage = languageCode;
    this.saveLanguage();
  }

  private saveLanguage(): void {
    this.isLoading = true;
    this.error = '';
    this.message = '';

    // TODO: Implement language persistence (localStorage or backend)
    localStorage.setItem('flowly_language', this.selectedLanguage);

    setTimeout(() => {
      this.isLoading = false;
      this.message = 'Мову успішно змінено';
      setTimeout(() => this.message = '', 3000);
    }, 500);
  }
}
