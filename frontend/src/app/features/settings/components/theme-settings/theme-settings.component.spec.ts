

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
    
    currentThemeSubject = new BehaviorSubject<ThemeMode>('normal');

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

  describe('Theme Initialization', () => {
    it('should load current theme from ThemeService on init', (done) => {
      
      currentThemeSubject.next('low-stimulus');

      component.ngOnInit();

      setTimeout(() => {
        expect(component.selectedTheme).toBe('low-stimulus');
        done();
      }, 100);
    });

    it('should have default theme as "normal"', () => {
      
      expect(component.selectedTheme).toBe('normal');
    });

    it('should have two theme options available', () => {
      
      expect(component.themes.length).toBe(2);
      expect(component.themes[0].id).toBe('normal');
      expect(component.themes[1].id).toBe('low-stimulus');
    });
  });

  describe('Theme Selection', () => {
    it('should call ThemeService.setTheme when selecting a theme', () => {
      
      const newTheme: ThemeMode = 'low-stimulus';

      component.selectTheme(newTheme);

      expect(themeServiceSpy.setTheme).toHaveBeenCalledWith(newTheme);
    });

    it('should not call ThemeService.setTheme when selecting the same theme', () => {
      
      component.selectedTheme = 'normal';

      component.selectTheme('normal');

      expect(themeServiceSpy.setTheme).not.toHaveBeenCalled();
    });

    it('should update selectedTheme when theme is changed', () => {
      
      expect(component.selectedTheme).toBe('normal');

      component.selectTheme('low-stimulus');

      currentThemeSubject.next('low-stimulus');
      expect(component.selectedTheme).toBe('low-stimulus');
    });
  });

  describe('Theme Change Behavior', () => {
    it('should change from normal to low-stimulus theme', () => {
      
      component.selectedTheme = 'normal';

      component.selectTheme('low-stimulus');
      currentThemeSubject.next('low-stimulus');

      expect(component.selectedTheme).toBe('low-stimulus');
      expect(themeServiceSpy.setTheme).toHaveBeenCalledWith('low-stimulus');
    });

    it('should change from low-stimulus to normal theme', () => {
      
      component.selectedTheme = 'low-stimulus';

      component.selectTheme('normal');
      currentThemeSubject.next('normal');

      expect(component.selectedTheme).toBe('normal');
      expect(themeServiceSpy.setTheme).toHaveBeenCalledWith('normal');
    });

    it('should reflect theme changes from ThemeService observable', (done) => {
      
      expect(component.selectedTheme).toBe('normal');

      currentThemeSubject.next('low-stimulus');

      setTimeout(() => {
        expect(component.selectedTheme).toBe('low-stimulus');
        done();
      }, 100);
    });
  });

  describe('Theme Preview', () => {
    it('should return correct preview gradient for normal theme', () => {
      
      component.selectedTheme = 'normal';

      const preview = component.getSelectedThemePreview();

      expect(preview).toContain('linear-gradient');
      expect(preview).toContain('#f8fafc');
    });

    it('should return correct preview gradient for low-stimulus theme', () => {
      
      component.selectedTheme = 'low-stimulus';

      const preview = component.getSelectedThemePreview();

      expect(preview).toContain('linear-gradient');
      expect(preview).toContain('#e9ecef');
    });

    it('should return empty string for unknown theme', () => {
      
      component.selectedTheme = 'unknown-theme' as ThemeMode;

      const preview = component.getSelectedThemePreview();

      expect(preview).toBe('');
    });
  });

  describe('Theme Name', () => {
    it('should return correct translation key for normal theme', () => {
      
      component.selectedTheme = 'normal';

      const name = component.getSelectedThemeName();

      expect(name).toBe('SETTINGS.THEME.NORMAL.NAME');
    });

    it('should return correct translation key for low-stimulus theme', () => {
      
      component.selectedTheme = 'low-stimulus';

      const name = component.getSelectedThemeName();

      expect(name).toBe('SETTINGS.THEME.LOW_STIMULUS.NAME');
    });

    it('should return empty string for unknown theme', () => {
      
      component.selectedTheme = 'unknown-theme' as ThemeMode;

      const name = component.getSelectedThemeName();

      expect(name).toBe('');
    });
  });

  describe('Multiple Theme Switches', () => {
    it('should handle multiple theme switches correctly', () => {
      
      const themeSequence: ThemeMode[] = ['low-stimulus', 'normal', 'low-stimulus', 'normal'];

      themeSequence.forEach((theme, index) => {
        component.selectTheme(theme);
        currentThemeSubject.next(theme);

        expect(component.selectedTheme).toBe(theme, `Theme should be ${theme} at step ${index + 1}`);
        expect(themeServiceSpy.setTheme).toHaveBeenCalledWith(theme);
      });

      expect(themeServiceSpy.setTheme).toHaveBeenCalledTimes(themeSequence.length);
    });
  });

  describe('Theme Persistence', () => {
    it('should maintain theme selection across component lifecycle', () => {
      
      component.selectTheme('low-stimulus');
      currentThemeSubject.next('low-stimulus');

      component.ngOnInit();

      expect(component.selectedTheme).toBe('low-stimulus');
    });
  });
});
