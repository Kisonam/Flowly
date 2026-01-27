import { Directive, ElementRef, OnInit, OnDestroy, HostListener } from '@angular/core';
import { AccessibilityService } from '../../core/services/accessibility.service';

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
    
    this.previouslyFocusedElement = this.accessibilityService.saveFocus();

    setTimeout(() => {
      this.accessibilityService.focusFirstElement(this.elementRef.nativeElement);
    }, 0);
  }

  ngOnDestroy(): void {
    
    this.accessibilityService.restoreFocus(this.previouslyFocusedElement);
  }

  @HostListener('keydown', ['$event'])
  handleKeydown(event: KeyboardEvent): void {
    if (event.key === 'Tab') {
      this.accessibilityService.trapFocus(this.elementRef.nativeElement, event);
    }
  }
}
