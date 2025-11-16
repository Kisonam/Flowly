
import { Component, inject, OnInit } from '@angular/core';
import { NgIf } from '@angular/common';
import { Router, RouterOutlet } from '@angular/router';
import { NavbarComponent } from './shared/navbar/navbar.component';
import { ThemeService } from './core/services/theme.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, NavbarComponent, NgIf],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  title = 'frontend';
  private themeService = inject(ThemeService);

  constructor(private router: Router) {}

  ngOnInit(): void {
    // Theme service will initialize automatically via constructor
    // This ensures theme is applied on app startup
  }

  get showNavbar(): boolean {
    const url = this.router.url || '';
    return !url.startsWith('/auth');
  }
}
