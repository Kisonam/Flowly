import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';

export type SupportedLocale = 'en' | 'uk' | 'pl';

@Injectable({
  providedIn: 'root'
})
export class LocaleService {
  private readonly LOCALE_KEY = 'app-locale';
  private readonly supportedLocales: SupportedLocale[] = ['en', 'uk', 'pl'];
  private translate = inject(TranslateService);
  private http = inject(HttpClient);

  constructor() {
    // Set default and current language
    this.translate.setDefaultLang('en');
    const locale = this.getCurrentLocale();
    this.translate.use(locale);
  }

  /**
   * Get current locale from localStorage or use English as default
   */
  getCurrentLocale(): SupportedLocale {
    const stored = localStorage.getItem(this.LOCALE_KEY);
    if (stored && this.isSupportedLocale(stored)) {
      return stored as SupportedLocale;
    }
    // Always use English on first launch
    return 'en';
  }

  /**
   * Set locale and switch language instantly
   */
  setLocale(locale: SupportedLocale): void {
    if (!this.supportedLocales.includes(locale)) {
      console.warn(`Unsupported locale: ${locale}`);
      return;
    }

    localStorage.setItem(this.LOCALE_KEY, locale);
    this.translate.use(locale); // Instant switch!
  }

  /**
   * Check if locale is supported
   */
  private isSupportedLocale(locale: string): boolean {
    return this.supportedLocales.includes(locale as SupportedLocale);
  }

  /**
   * Get all supported locales
   */
  getSupportedLocales(): SupportedLocale[] {
    return [...this.supportedLocales];
  }
}
