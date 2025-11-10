// frontend/src/app/features/notes/notes.routes.ts

import { Routes } from '@angular/router';
import { authGuard } from '../auth/guards/auth.guard';

export const NOTES_ROUTES: Routes = [
  {
    path: '',
    canActivate: [authGuard],
    children: [
      {
        path: '',
        loadComponent: () => import('./components/note-list/note-list.component').then(m => m.NoteListComponent)
      },
      {
        path: 'board',
        loadComponent: () => import('./components/notes-board/notes-board.component').then(m => m.NotesBoardComponent)
      },
      {
        path: 'new',
        loadComponent: () => import('./components/note-editor/note-editor.component').then(m => m.NoteEditorComponent)
      },
      {
        path: ':id',
        loadComponent: () => import('./components/note-detail/note-detail.component').then(m => m.NoteDetailComponent)
      },
      {
        path: ':id/edit',
        loadComponent: () => import('./components/note-editor/note-editor.component').then(m => m.NoteEditorComponent)
      }
    ]
  }
];
