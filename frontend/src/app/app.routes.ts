import { Routes } from '@angular/router';
import { HomeComponent } from './features/home/home.component';
import { authGuard } from './features/auth/guards/auth.guard';
import { ThemesBoardComponent } from './features/themes/components/themes-board/themes-board.component';
import { TagsManagerComponent } from './features/tags/components/tags-manager/tags-manager.component';
import { ArchiveListComponent } from './features/archive/archive-list/archive-list.component';
import { SettingsComponent } from './features/settings/settings.component';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/home',
    pathMatch: 'full'
  },
  {
    path: 'home',
    component: HomeComponent
    
  },
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.AUTH_ROUTES)
  },
  {
    path: 'dashboard',
    loadChildren: () => import('./features/dashboard/dashboard.routes').then(m => m.dashboardRoutes),
    canActivate: [authGuard]
  },
  {
    path: 'notes',
    loadChildren: () => import('./features/notes/notes.routes').then(m => m.NOTES_ROUTES)
  },
  {
    path: 'tasks',
    loadChildren: () => import('./features/tasks/tasks.routes').then(m => m.TASKS_ROUTES)
  },
  {
    path: 'finance',
    loadChildren: () => import('./features/finance/finance.routes').then(m => m.FINANCE_ROUTES)
  },
  {
    path: 'tags',
    component: TagsManagerComponent,
    canActivate: [authGuard]
  },
  {
    path: 'themes',
    component: ThemesBoardComponent,
    canActivate: [authGuard]
  },
  {
    path: 'archive',
    component: ArchiveListComponent,
    canActivate: [authGuard]
  },
  {
    path: 'settings',
    component: SettingsComponent,
    canActivate: [authGuard]
  },
  {
    path: '**',
    redirectTo: '/home'
  }
];

