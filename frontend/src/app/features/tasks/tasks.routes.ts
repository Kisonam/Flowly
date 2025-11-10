// frontend/src/app/features/tasks/tasks.routes.ts

import { Routes } from '@angular/router';
import { authGuard } from '../auth/guards/auth.guard';

export const TASKS_ROUTES: Routes = [
  {
    path: '',
    canActivate: [authGuard],
    children: [
      {
        path: '',
        redirectTo: 'board',
        pathMatch: 'full'
      },
      {
        path: 'board',
        loadComponent: () => import('./components/task-board/task-board.component').then(m => m.TaskBoardComponent)
      },
      {
        path: 'archived',
        loadComponent: () => import('./components/task-board/task-board.component').then(m => m.TaskBoardComponent),
        data: { archived: true }
      },
      {
        path: 'new',
        loadComponent: () => import('./components/task-editor/task-editor.component').then(m => m.TaskEditorComponent)
      },
      {
        path: ':id',
        loadComponent: () => import('./components/task-detail/task-detail.component').then(m => m.TaskDetailComponent)
      },
      {
        path: ':id/edit',
        loadComponent: () => import('./components/task-editor/task-editor.component').then(m => m.TaskEditorComponent)
      },
      {
        path: 'themes',
        loadComponent: () => import('./components/theme-manager/theme-manager.component').then(m => m.ThemeManagerComponent)
      }
    ]
  }
];
