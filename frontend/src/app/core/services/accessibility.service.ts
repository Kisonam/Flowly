import { Injectable, ElementRef } from '@angular/core';

/**
 * Service to manage accessibility features across the application
 */
@Injectable({
  providedIn: 'root'
})
export class AccessibilityService {
  private focusableSelectors = [
    'a[href]',
    'button:not([disabled])',
    'textarea:not([disabled])',
    'input:not([disabled])',
    'select:not([disabled])',
    '[tabindex]:not([tabindex="-1"])'
  ].join(', ');

  /**
   * Get all focusable elements within a container
   */
  getFocusableElements(container: HTMLElement): HTMLElement[] {
    return Array.from(container.querySelectorAll(this.focusableSelectors));
  }

  /**
   * Trap focus within a container (for modals/dialogs)
   */
  trapFocus(container: HTMLElement, event: KeyboardEvent): void {
    const focusableElements = this.getFocusableElements(container);
    
    if (focusableElements.length === 0) {
      return;
    }

    const firstElement = focusableElements[0];
    const lastElement = focusableElements[focusableElements.length - 1];

    // Handle Tab key
    if (event.key === 'Tab') {
      if (event.shiftKey) {
        // Shift + Tab
        if (document.activeElement === firstElement) {
          event.preventDefault();
          lastElement.focus();
        }
      } else {
        // Tab
        if (document.activeElement === lastElement) {
          event.preventDefault();
          firstElement.focus();
        }
      }
    }
  }

  /**
   * Focus the first focusable element in a container
   */
  focusFirstElement(container: HTMLElement): void {
    const focusableElements = this.getFocusableElements(container);
    if (focusableElements.length > 0) {
      focusableElements[0].focus();
    }
  }

  /**
   * Restore focus to a previously focused element
   */
  restoreFocus(element: HTMLElement | null): void {
    if (element && typeof element.focus === 'function') {
      // Use setTimeout to ensure the element is ready to receive focus
      setTimeout(() => element.focus(), 0);
    }
  }

  /**
   * Save the currently focused element
   */
  saveFocus(): HTMLElement | null {
    return document.activeElement as HTMLElement;
  }

  /**
   * Announce a message to screen readers
   */
  announce(message: string, priority: 'polite' | 'assertive' = 'polite'): void {
    const announcer = document.createElement('div');
    announcer.setAttribute('aria-live', priority);
    announcer.setAttribute('aria-atomic', 'true');
    announcer.className = 'sr-only';
    announcer.textContent = message;
    
    document.body.appendChild(announcer);
    
    // Remove after announcement
    setTimeout(() => {
      document.body.removeChild(announcer);
    }, 1000);
  }
}
