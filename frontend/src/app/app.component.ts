
import { Component, inject, OnInit } from '@angular/core';
import { NgIf } from '@angular/common';
import { Router, RouterOutlet } from '@angular/router';
import { NavbarComponent } from './shared/navbar/navbar.component';
import { ThemeService } from './core/services/theme.service';
import { LocaleService } from './core/services/locale.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, NavbarComponent, NgIf],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  title = 'frontend';
  private themeService = inject(ThemeService);
  private localeService = inject(LocaleService);

  constructor(private router: Router) {}

  ngOnInit(): void {
    // Theme service will initialize automatically via constructor
    // This ensures theme is applied on app startup

    // Initialize locale service to ensure translations are loaded
    // This triggers the constructor which sets up the language
    this.localeService.getCurrentLocale();
  }

  get showNavbar(): boolean {
    const url = this.router.url || '';
    return !url.startsWith('/auth');
  }
}
