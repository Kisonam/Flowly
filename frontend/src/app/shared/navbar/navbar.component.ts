import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Output } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-navbar',
  imports: [CommonModule],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss']
})
export class NavbarComponent {
  @Output() moduleChange = new EventEmitter<string>();
  activeModule: string = 'overview';
  readonly modules = [
    { id: 'overview', icon: 'target', label: 'Огляд', route: '/home' },
    { id: 'notes', icon: 'file-text', label: 'Нотатки', route: '/notes' },
    { id: 'tasks', icon: 'check-square', label: 'Завдання', route: '/' },
    { id: 'finance', icon: 'dollar-sign', label: 'Фінанси', route: '/' },
  ];

  constructor(private router: Router) {}

  setActiveModule(moduleId: string): void {
    this.activeModule = moduleId;
    this.moduleChange.emit(moduleId);

    const module = this.modules.find(m => m.id === moduleId);
    if (module?.route) {
      this.router.navigate([module.route]);
    }
  }
}
