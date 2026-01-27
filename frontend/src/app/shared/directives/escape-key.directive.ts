import { Directive, Output, EventEmitter, HostListener } from '@angular/core';

@Directive({
  selector: '[escapeKey]',
  standalone: true
})
export class EscapeKeyDirective {
  @Output() escapeKey = new EventEmitter<void>();

  @HostListener('document:keydown.escape')
  handleEscape(): void {
    this.escapeKey.emit();
  }
}
