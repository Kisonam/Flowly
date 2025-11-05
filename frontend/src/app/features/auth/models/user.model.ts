export enum ThemeMode {
  Normal = 'Normal',
  LowStimulus = 'LowStimulus'
}

export interface User {
  id: string;
  email: string;
  displayName: string;
  avatarUrl?: string;
  preferredTheme: ThemeMode;
  createdAt: string;
}
