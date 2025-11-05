// Google Identity Services Types
// Based on: https://developers.google.com/identity/gsi/web/reference/js-reference

export interface GoogleAuthWindow extends Window {
  google?: {
    accounts: {
      id: {
        initialize: (config: GoogleInitConfig) => void;
        prompt: (momentListener?: (notification: PromptMomentNotification) => void) => void;
        renderButton: (parent: HTMLElement, options: GoogleButtonConfig) => void;
        disableAutoSelect: () => void;
        storeCredential: (credential: { id: string; password: string }, callback: () => void) => void;
        cancel: () => void;
        onGoogleLibraryLoad: () => void;
        revoke: (hint: string, callback: (done: RevocationResponse) => void) => void;
      };
    };
  };
}

export interface GoogleInitConfig {
  client_id: string;
  callback?: (response: GoogleCallbackResponse) => void;
  auto_select?: boolean;
  cancel_on_tap_outside?: boolean;
  context?: 'signin' | 'signup' | 'use';
  ux_mode?: 'popup' | 'redirect';
  login_uri?: string;
  native_callback?: (response: GoogleCallbackResponse) => void;
  intermediate_iframe_close_callback?: () => void;
  itp_support?: boolean;
}

export interface GoogleCallbackResponse {
  credential: string; // JWT ID token
  select_by?: string;
  clientId?: string;
}

export interface GoogleButtonConfig {
  type?: 'standard' | 'icon';
  theme?: 'outline' | 'filled_blue' | 'filled_black';
  size?: 'large' | 'medium' | 'small';
  text?: 'signin_with' | 'signup_with' | 'continue_with' | 'signin';
  shape?: 'rectangular' | 'pill' | 'circle' | 'square';
  logo_alignment?: 'left' | 'center';
  width?: number;
  locale?: string;
}

export interface PromptMomentNotification {
  isDisplayMoment: () => boolean;
  isDisplayed: () => boolean;
  isNotDisplayed: () => boolean;
  getNotDisplayedReason: () => string;
  isSkippedMoment: () => boolean;
  getSkippedReason: () => string;
  isDismissedMoment: () => boolean;
  getDismissedReason: () => string;
  getMomentType: () => string;
}

export interface RevocationResponse {
  successful: boolean;
  error?: string;
}

export interface GoogleCredentialPayload {
  iss: string;
  azp: string;
  aud: string;
  sub: string;
  email: string;
  email_verified: boolean;
  name: string;
  picture?: string;
  given_name?: string;
  family_name?: string;
  iat: number;
  exp: number;
  jti?: string;
}
