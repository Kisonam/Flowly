import { Injectable, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { Observable, from } from 'rxjs';

export interface ConfirmDialogOptions {
  title?: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
}

@Injectable({
  providedIn: 'root'
})
export class DialogService {
  private translate = inject(TranslateService);

  /**
   * Shows a confirmation dialog
   * @param options Dialog options
   * @returns Observable<boolean> - true if confirmed, false if cancelled
   */
  confirm(options: ConfirmDialogOptions | string): Observable<boolean> {
    const message = typeof options === 'string'
      ? options
      : options.message;

    // For now, using native confirm but with proper structure
    // This can be replaced with a custom modal component later
    const result = window.confirm(message);
    return from(Promise.resolve(result));
  }

  /**
   * Shows an alert dialog
   * @param message Message to display
   */
  alert(message: string): void {
    window.alert(message);
  }

  /**
   * Shows a confirmation dialog with translation support
   * @param translationKey Translation key for the message
   * @param interpolateParams Parameters for translation interpolation
   * @returns Observable<boolean> - true if confirmed, false if cancelled
   */
  confirmTranslated(translationKey: string, interpolateParams?: any): Observable<boolean> {
    const message = this.translate.instant(translationKey, interpolateParams);
    return this.confirm(message);
  }

  /**
   * Shows an alert dialog with translation support
   * @param translationKey Translation key for the message
   * @param interpolateParams Parameters for translation interpolation
   */
  alertTranslated(translationKey: string, interpolateParams?: any): void {
    const message = this.translate.instant(translationKey, interpolateParams);
    this.alert(message);
  }
}
