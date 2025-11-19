import { Directive, ElementRef, OnInit, OnDestroy, HostListener } from '@angular/core';
import { AccessibilityService } from '../../core/services/accessibility.service';

/**
 * Directive to trap focus within a container (typically a modal or dialog)
 * Usage: <div appFocusTrap>...</div>
 */
@Directive({
  selector: '[appFocusTrap]',
  standalone: true
})
export class FocusTrapDirective implements OnInit, OnDestroy {
  private previouslyFocusedElement: HTMLElement | null = null;

  constructor(
    private elementRef: ElementRef<HTMLElement>,
    private accessibilityService: AccessibilityService
  ) {}

  ngOnInit(): void {
    // Save the currently focused element
    this.previouslyFocusedElement = this.accessibilityService.saveFocus();
    
    // Focus the first element in the container
    setTimeout(() => {
      this.accessibilityService.focusFirstElement(this.elementRef.nativeElement);
    }, 0);
  }

  ngOnDestroy(): void {
    // Restore focus to the previously focused element
    this.accessibilityService.restoreFocus(this.previouslyFocusedElement);
  }

  @HostListener('keydown', ['$event'])
  handleKeydown(event: KeyboardEvent): void {
    if (event.key === 'Tab') {
      this.accessibilityService.trapFocus(this.elementRef.nativeElement, event);
    }
  }
}
