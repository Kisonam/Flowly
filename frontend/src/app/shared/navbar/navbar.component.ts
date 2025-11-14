import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Output, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../features/auth/services/auth.service';
import { User } from '../../features/auth/models/user.model';

@Component({
  selector: 'app-navbar',
  imports: [CommonModule],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss']
})
export class NavbarComponent implements OnInit {
  @Output() moduleChange = new EventEmitter<string>();

  private router = inject(Router);
  private authService = inject(AuthService);

  activeModule: string = 'overview';
  currentUser: User | null = null;
  showUserMenu = false;

  readonly modules = [
    { id: 'overview', icon: 'target', label: 'Огляд', route: '/home' },
    { id: 'notes', icon: 'file-text', label: 'Нотатки', route: '/notes' },
    { id: 'tasks', icon: 'check-square', label: 'Завдання', route: '/tasks' },
    { id: 'finance', icon: 'dollar-sign', label: 'Фінанси', route: '/finance' },
    { id: 'archive', icon: 'archive', label: 'Архів', route: '/archive' },
  ];

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
    });
  }

  setActiveModule(moduleId: string): void {
    this.activeModule = moduleId;
    this.moduleChange.emit(moduleId);

    const module = this.modules.find(m => m.id === moduleId);
    if (module?.route) {
      this.router.navigate([module.route]);
    }
  }

  toggleUserMenu(): void {
    this.showUserMenu = !this.showUserMenu;
  }

  closeUserMenu(): void {
    this.showUserMenu = false;
  }

  navigateToSettings(): void {
    this.closeUserMenu();
    this.router.navigate(['/settings']);
  }

  logout(): void {
    this.closeUserMenu();
    this.authService.logout();
  }

  get userInitials(): string {
    if (!this.currentUser?.displayName) return '?';
    const names = this.currentUser.displayName.split(' ');
    if (names.length >= 2) {
      return (names[0][0] + names[1][0]).toUpperCase();
    }
    return this.currentUser.displayName[0].toUpperCase();
  }
}
