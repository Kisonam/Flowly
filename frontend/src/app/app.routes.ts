import { Routes } from '@angular/router';
import { HomeComponent } from './features/home/home.component';
import { authGuard, guestGuard } from './features/auth/guards/auth.guard';
import { NoteListComponent } from './features/notes/components/note-list/note-list.component';
import { NoteEditorComponent } from './features/notes/components/note-editor/note-editor.component';
import { NoteDetailComponent } from './features/notes/components/note-detail/note-detail.component';
import { NotesBoardComponent } from './features/notes/components/notes-board/notes-board.component';
import { ThemesBoardComponent } from './features/themes/components/themes-board/themes-board.component';
import { TagsManagerComponent } from './features/tags/components/tags-manager/tags-manager.component';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/auth/login',
    pathMatch: 'full'
  },
  {
    path: 'auth',
    canActivate: [guestGuard],
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.AUTH_ROUTES)
  },
  {
    path: 'home',
    component: HomeComponent,
    canActivate: [authGuard]
  },
  {
    path: 'dashboard',
    component: HomeComponent,
    canActivate: [authGuard]
  },
  {
    path: 'notes',
    component: NoteListComponent,
    canActivate: [authGuard]
  },
  {
    path: 'notes/board',
    component: NotesBoardComponent,
    canActivate: [authGuard]
  },
  {
    path: 'notes/new',
    component: NoteEditorComponent,
    canActivate: [authGuard]
  },
  {
    path: 'notes/:id',
    component: NoteDetailComponent,
    canActivate: [authGuard]
  },
  {
    path: 'notes/:id/edit',
    component: NoteEditorComponent,
    canActivate: [authGuard]
  },
  {
    path: 'tasks',
    redirectTo: 'tasks/board',
    pathMatch: 'full'
  },
  {
    path: 'tasks/board',
    canActivate: [authGuard],
    loadComponent: () => import('./features/tasks/components/task-board/task-board.component').then(m => m.TaskBoardComponent)
  },
  {
    path: 'tasks/archived',
    canActivate: [authGuard],
    loadComponent: () => import('./features/tasks/components/task-board/task-board.component').then(m => m.TaskBoardComponent),
    data: { archived: true }
  },
  {
    path: 'tasks/new',
    canActivate: [authGuard],
    loadComponent: () => import('./features/tasks/components/task-editor/task-editor.component').then(m => m.TaskEditorComponent)
  },
  {
    path: 'tasks/:id/edit',
    canActivate: [authGuard],
    loadComponent: () => import('./features/tasks/components/task-editor/task-editor.component').then(m => m.TaskEditorComponent)
  },
  {
    path: 'tasks/:id',
    canActivate: [authGuard],
    loadComponent: () => import('./features/tasks/components/task-detail/task-detail.component').then(m => m.TaskDetailComponent)
  },
  {
    path: 'tasks/themes',
    canActivate: [authGuard],
    loadComponent: () => import('./features/tasks/components/theme-manager/theme-manager.component').then(m => m.ThemeManagerComponent)
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
    path: '**',
    redirectTo: '/auth/login'
  }
];
