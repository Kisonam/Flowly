import { Directive, Output, EventEmitter, HostListener } from '@angular/core';

/**
 * Directive to handle Escape key press
 * Usage: <div (escapeKey)="onClose()">...</div>
 */
@Directive({
  selector: '[escapeKey]',
  standalone: true
})
export class EscapeKeyDirective {
  @Output() escapeKey = new EventEmitter<void>();

  @HostListener('document:keydown.escape', ['$event'])
  handleEscape(event: KeyboardEvent): void {
    event.preventDefault();
    this.escapeKey.emit();
  }
}
