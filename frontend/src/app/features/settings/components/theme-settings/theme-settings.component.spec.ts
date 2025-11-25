// frontend/src/app/features/settings/components/theme-settings/theme-settings.component.spec.ts

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, BehaviorSubject } from 'rxjs';
import { ThemeSettingsComponent } from './theme-settings.component';
import { AuthService } from '../../../auth/services/auth.service';
import { ThemeService, ThemeMode } from '../../../../core/services/theme.service';
import { TranslateModule } from '@ngx-translate/core';

describe('ThemeSettingsComponent', () => {
  let component: ThemeSettingsComponent;
  let fixture: ComponentFixture<ThemeSettingsComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let themeServiceSpy: jasmine.SpyObj<ThemeService>;
  let currentThemeSubject: BehaviorSubject<ThemeMode>;

  beforeEach(async () => {
    // Create BehaviorSubject for theme
    currentThemeSubject = new BehaviorSubject<ThemeMode>('normal');

    // Create spies for dependencies
    const authSpy = jasmine.createSpyObj('AuthService', ['updateProfile']);
    const themeSpy = jasmine.createSpyObj('ThemeService', ['setTheme'], {
      currentTheme$: currentThemeSubject.asObservable()
    });

    await TestBed.configureTestingModule({
      imports: [
        ThemeSettingsComponent,
        TranslateModule.forRoot()
      ],
      providers: [
        { provide: AuthService, useValue: authSpy },
        { provide: ThemeService, useValue: themeSpy }
      ]
    }).compileComponents();

    authServiceSpy = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
    themeServiceSpy = TestBed.inject(ThemeService) as jasmine.SpyObj<ThemeService>;

    fixture = TestBed.createComponent(ThemeSettingsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ============================================
  // TEST 1: Theme Initialization
  // ============================================
  describe('Theme Initialization', () => {
    it('should load current theme from ThemeService on init', (done) => {
      // Arrange
      currentThemeSubject.next('low-stimulus');

      // Act
      component.ngOnInit();

      // Assert
      setTimeout(() => {
        expect(component.selectedTheme).toBe('low-stimulus');
        done();
      }, 100);
    });

    it('should have default theme as "normal"', () => {
      // Assert
      expect(component.selectedTheme).toBe('normal');
    });

    it('should have two theme options available', () => {
      // Assert
      expect(component.themes.length).toBe(2);
      expect(component.themes[0].id).toBe('normal');
      expect(component.themes[1].id).toBe('low-stimulus');
    });
  });

  // ============================================
  // TEST 2: Theme Selection
  // ============================================
  describe('Theme Selection', () => {
    it('should call ThemeService.setTheme when selecting a theme', () => {
      // Arrange
      const newTheme: ThemeMode = 'low-stimulus';

      // Act
      component.selectTheme(newTheme);

      // Assert
      expect(themeServiceSpy.setTheme).toHaveBeenCalledWith(newTheme);
    });

    it('should not call ThemeService.setTheme when selecting the same theme', () => {
      // Arrange
      component.selectedTheme = 'normal';

      // Act
      component.selectTheme('normal');

      // Assert
      expect(themeServiceSpy.setTheme).not.toHaveBeenCalled();
    });

    it('should update selectedTheme when theme is changed', () => {
      // Arrange
      expect(component.selectedTheme).toBe('normal');

      // Act
      component.selectTheme('low-stimulus');

      // Assert - selectedTheme will be updated via the observable subscription
      currentThemeSubject.next('low-stimulus');
      expect(component.selectedTheme).toBe('low-stimulus');
    });
  });

  // ============================================
  // TEST 3: Theme Change Behavior
  // ============================================
  describe('Theme Change Behavior', () => {
    it('should change from normal to low-stimulus theme', () => {
      // Arrange
      component.selectedTheme = 'normal';

      // Act
      component.selectTheme('low-stimulus');
      currentThemeSubject.next('low-stimulus');

      // Assert
      expect(component.selectedTheme).toBe('low-stimulus');
      expect(themeServiceSpy.setTheme).toHaveBeenCalledWith('low-stimulus');
    });

    it('should change from low-stimulus to normal theme', () => {
      // Arrange
      component.selectedTheme = 'low-stimulus';

      // Act
      component.selectTheme('normal');
      currentThemeSubject.next('normal');

      // Assert
      expect(component.selectedTheme).toBe('normal');
      expect(themeServiceSpy.setTheme).toHaveBeenCalledWith('normal');
    });

    it('should reflect theme changes from ThemeService observable', (done) => {
      // Arrange
      expect(component.selectedTheme).toBe('normal');

      // Act - Simulate external theme change
      currentThemeSubject.next('low-stimulus');

      // Assert
      setTimeout(() => {
        expect(component.selectedTheme).toBe('low-stimulus');
        done();
      }, 100);
    });
  });

  // ============================================
  // TEST 4: Theme Preview
  // ============================================
  describe('Theme Preview', () => {
    it('should return correct preview gradient for normal theme', () => {
      // Arrange
      component.selectedTheme = 'normal';

      // Act
      const preview = component.getSelectedThemePreview();

      // Assert
      expect(preview).toContain('linear-gradient');
      expect(preview).toContain('#f8fafc');
    });

    it('should return correct preview gradient for low-stimulus theme', () => {
      // Arrange
      component.selectedTheme = 'low-stimulus';

      // Act
      const preview = component.getSelectedThemePreview();

      // Assert
      expect(preview).toContain('linear-gradient');
      expect(preview).toContain('#e9ecef');
    });

    it('should return empty string for unknown theme', () => {
      // Arrange
      component.selectedTheme = 'unknown-theme' as ThemeMode;

      // Act
      const preview = component.getSelectedThemePreview();

      // Assert
      expect(preview).toBe('');
    });
  });

  // ============================================
  // TEST 5: Theme Name
  // ============================================
  describe('Theme Name', () => {
    it('should return correct translation key for normal theme', () => {
      // Arrange
      component.selectedTheme = 'normal';

      // Act
      const name = component.getSelectedThemeName();

      // Assert
      expect(name).toBe('SETTINGS.THEME.NORMAL.NAME');
    });

    it('should return correct translation key for low-stimulus theme', () => {
      // Arrange
      component.selectedTheme = 'low-stimulus';

      // Act
      const name = component.getSelectedThemeName();

      // Assert
      expect(name).toBe('SETTINGS.THEME.LOW_STIMULUS.NAME');
    });

    it('should return empty string for unknown theme', () => {
      // Arrange
      component.selectedTheme = 'unknown-theme' as ThemeMode;

      // Act
      const name = component.getSelectedThemeName();

      // Assert
      expect(name).toBe('');
    });
  });

  // ============================================
  // TEST 6: Multiple Theme Switches
  // ============================================
  describe('Multiple Theme Switches', () => {
    it('should handle multiple theme switches correctly', () => {
      // Arrange
      const themeSequence: ThemeMode[] = ['low-stimulus', 'normal', 'low-stimulus', 'normal'];

      // Act & Assert
      themeSequence.forEach((theme, index) => {
        component.selectTheme(theme);
        currentThemeSubject.next(theme);

        expect(component.selectedTheme).toBe(theme, `Theme should be ${theme} at step ${index + 1}`);
        expect(themeServiceSpy.setTheme).toHaveBeenCalledWith(theme);
      });

      // Verify setTheme was called correct number of times
      expect(themeServiceSpy.setTheme).toHaveBeenCalledTimes(themeSequence.length);
    });
  });

  // ============================================
  // TEST 7: Theme Persistence
  // ============================================
  describe('Theme Persistence', () => {
    it('should maintain theme selection across component lifecycle', () => {
      // Arrange
      component.selectTheme('low-stimulus');
      currentThemeSubject.next('low-stimulus');

      // Act - Simulate component re-initialization
      component.ngOnInit();

      // Assert
      expect(component.selectedTheme).toBe('low-stimulus');
    });
  });
});
