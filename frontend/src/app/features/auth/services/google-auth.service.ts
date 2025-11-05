// Google Authentication Service
// Handles Google Identity Services integration

import { Injectable, NgZone } from '@angular/core';
import { Observable, Subject } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  GoogleAuthWindow,
  GoogleCallbackResponse,
  GoogleInitConfig,
  GoogleButtonConfig
} from '../types/google-auth.types';

declare const window: GoogleAuthWindow;

@Injectable({
  providedIn: 'root'
})
export class GoogleAuthService {
  private readonly clientId = environment.googleClientId;
  private initialized = false;
  private credentialResponseSubject = new Subject<string>();

  constructor(private ngZone: NgZone) {}

  /**
   * Initialize Google Identity Services
   * Should be called once during app initialization
   */
  initialize(): Promise<void> {
    return new Promise((resolve, reject) => {
      if (this.initialized) {
        resolve();
        return;
      }

      // Check if Google Identity Services script is loaded
      if (!window.google?.accounts?.id) {
        // Wait for script to load
        const checkInterval = setInterval(() => {
          if (window.google?.accounts?.id) {
            clearInterval(checkInterval);
            this.initializeGoogleAuth();
            this.initialized = true;
            resolve();
          }
        }, 100);

        // Timeout after 10 seconds
        setTimeout(() => {
          clearInterval(checkInterval);
          reject(new Error('Google Identity Services failed to load'));
        }, 10000);
      } else {
        this.initializeGoogleAuth();
        this.initialized = true;
        resolve();
      }
    });
  }

  /**
   * Initialize Google Authentication
   */
  private initializeGoogleAuth(): void {
    const config: GoogleInitConfig = {
      client_id: this.clientId,
      callback: (response: GoogleCallbackResponse) => {
        // Run callback inside Angular zone
        this.ngZone.run(() => {
          this.credentialResponseSubject.next(response.credential);
        });
      },
      auto_select: false,
      cancel_on_tap_outside: true,
      ux_mode: 'popup', // Use popup instead of One Tap to avoid CORS issues
      context: 'signin'
    };

    window.google!.accounts.id.initialize(config);
  }

  /**
   * Render Google Sign-In button
   * @param element HTML element where button will be rendered
   * @param options Button configuration
   */
  renderButton(element: HTMLElement, options?: Partial<GoogleButtonConfig>): void {
    if (!window.google?.accounts?.id) {
      console.error('Google Identity Services not loaded');
      return;
    }

    const defaultOptions: GoogleButtonConfig = {
      type: 'standard',
      theme: 'outline',
      size: 'large',
      text: 'signin_with',
      shape: 'rectangular',
      logo_alignment: 'left',
      width: element.offsetWidth || 300
    };

    const buttonConfig = { ...defaultOptions, ...options };
    window.google.accounts.id.renderButton(element, buttonConfig);
  }

  /**
   * Show One Tap prompt
   */
  showOneTap(): void {
    if (!window.google?.accounts?.id) {
      console.error('Google Identity Services not loaded');
      return;
    }

    window.google.accounts.id.prompt();
  }

  /**
   * Disable auto-select
   */
  disableAutoSelect(): void {
    if (!window.google?.accounts?.id) {
      return;
    }

    window.google.accounts.id.disableAutoSelect();
  }

  /**
   * Cancel One Tap prompt
   */
  cancel(): void {
    if (!window.google?.accounts?.id) {
      return;
    }

    window.google.accounts.id.cancel();
  }

  /**
   * Get credential response observable
   */
  getCredentialResponse(): Observable<string> {
    return this.credentialResponseSubject.asObservable();
  }

  /**
   * Sign in with Google (programmatic)
   * Returns the ID token
   */
  signIn(): Promise<string> {
    return new Promise((resolve, reject) => {
      const subscription = this.credentialResponseSubject.subscribe({
        next: (credential) => {
          subscription.unsubscribe();
          resolve(credential);
        },
        error: (error) => {
          subscription.unsubscribe();
          reject(error);
        }
      });

      // Show One Tap prompt
      this.showOneTap();

      // Timeout after 60 seconds
      setTimeout(() => {
        subscription.unsubscribe();
        reject(new Error('Google Sign-In timeout'));
      }, 60000);
    });
  }

  /**
   * Revoke Google access
   */
  revoke(email: string): Promise<boolean> {
    return new Promise((resolve, reject) => {
      if (!window.google?.accounts?.id) {
        reject(new Error('Google Identity Services not loaded'));
        return;
      }

      window.google.accounts.id.revoke(email, (response) => {
        if (response.successful) {
          resolve(true);
        } else {
          reject(new Error(response.error || 'Failed to revoke access'));
        }
      });
    });
  }

  /**
   * Check if Google Identity Services is available
   */
  isAvailable(): boolean {
    return !!window.google?.accounts?.id;
  }

  /**
   * Decode JWT token (for debugging only - don't rely on this for security)
   */
  decodeCredential(credential: string): any {
    try {
      const base64Url = credential.split('.')[1];
      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
      const jsonPayload = decodeURIComponent(
        atob(base64)
          .split('')
          .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
          .join('')
      );
      return JSON.parse(jsonPayload);
    } catch (error) {
      console.error('Failed to decode credential:', error);
      return null;
    }
  }
}
