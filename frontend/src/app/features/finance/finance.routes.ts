// frontend/src/app/features/finance/finance.routes.ts

import { Routes } from '@angular/router';
import { authGuard } from '../auth/guards/auth.guard';

export const FINANCE_ROUTES: Routes = [
  {
    path: '',
    canActivate: [authGuard],
    children: [
      {
        path: '',
        redirectTo: 'transactions',
        pathMatch: 'full'
      },
      {
        path: 'transactions',
        loadComponent: () => import('./components/transactions/transaction-list/transaction-list.component').then(m => m.TransactionListComponent)
      },
      {
        path: 'transactions/new',
        loadComponent: () => import('./components/transactions/transaction-editor/transaction-editor.component').then(m => m.TransactionEditorComponent)
      },
      {
        path: 'transactions/:id/edit',
        loadComponent: () => import('./components/transactions/transaction-editor/transaction-editor.component').then(m => m.TransactionEditorComponent)
      },
      {
        path: 'categories',
        loadComponent: () => import('./components/categories/category-manager/category-manager.component').then(m => m.CategoryManagerComponent)
      }
    ]
  }
];
