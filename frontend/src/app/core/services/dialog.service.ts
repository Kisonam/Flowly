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

  confirm(options: ConfirmDialogOptions | string): Observable<boolean> {
    const message = typeof options === 'string'
      ? options
      : options.message;

    const result = window.confirm(message);
    return from(Promise.resolve(result));
  }

  alert(message: string): void {
    window.alert(message);
  }

  confirmTranslated(translationKey: string, interpolateParams?: any): Observable<boolean> {
    const message = this.translate.instant(translationKey, interpolateParams);
    return this.confirm(message);
  }

  alertTranslated(translationKey: string, interpolateParams?: any): void {
    const message = this.translate.instant(translationKey, interpolateParams);
    this.alert(message);
  }
}
