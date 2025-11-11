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
      },
      {
        path: 'budgets',
        loadComponent: () => import('./components/budgets/budget-list/budget-list.component').then(m => m.BudgetListComponent)
      },
      {
        path: 'budgets/new',
        loadComponent: () => import('./components/budgets/budget-editor/budget-editor.component').then(m => m.BudgetEditorComponent)
      },
      {
        path: 'budgets/:id/edit',
        loadComponent: () => import('./components/budgets/budget-editor/budget-editor.component').then(m => m.BudgetEditorComponent)
      },
      {
        path: 'goals',
        loadComponent: () => import('./components/goals/goal-list/goal-list.component').then(m => m.GoalListComponent)
      },
      {
        path: 'goals/new',
        loadComponent: () => import('./components/goals/goal-editor/goal-editor.component').then(m => m.GoalEditorComponent)
      },
      {
        path: 'goals/:id/edit',
        loadComponent: () => import('./components/goals/goal-editor/goal-editor.component').then(m => m.GoalEditorComponent)
      },
      {
        path: 'dashboard',
        loadComponent: () => import('./components/finance-dashboard/finance-dashboard.component').then(m => m.FinanceDashboardComponent)
      }
    ]
  }
];
