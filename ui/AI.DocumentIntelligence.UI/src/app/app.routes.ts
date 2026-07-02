import { Routes } from '@angular/router';

import { authGuard } from './core/auth/auth.guard';
import { guestGuard } from './core/auth/guest.guard';
import { RoleGuardData, roleGuard } from './core/auth/role.guard';

const ALL_ROLES: RoleGuardData = { roles: ['Admin', 'Analyst', 'Viewer'] };
const ADMIN_ONLY: RoleGuardData = { roles: ['Admin'] };

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/landing/landing').then((m) => m.Landing),
    title: 'AI Document Intelligence Platform',
  },
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
    title: 'Sign In',
  },
  {
    path: 'analysis',
    canActivate: [authGuard, roleGuard],
    data: ALL_ROLES,
    loadComponent: () =>
      import('./features/placeholder/analysis-placeholder').then((m) => m.AnalysisPlaceholder),
    title: 'Analysis',
  },
  {
    path: 'compare',
    canActivate: [authGuard, roleGuard],
    data: ALL_ROLES,
    loadComponent: () =>
      import('./features/comparison/comparison').then((m) => m.ComparisonComponent),
    title: 'Comparison',
  },
  {
    path: 'chat',
    canActivate: [authGuard, roleGuard],
    data: ALL_ROLES,
    loadComponent: () => import('./features/chat/chat').then((m) => m.ChatComponent),
    title: 'Chat',
  },
  {
    path: 'admin',
    canActivate: [authGuard, roleGuard],
    data: ADMIN_ONLY,
    loadComponent: () => import('./features/placeholder/admin-placeholder').then((m) => m.AdminPlaceholder),
    title: 'Admin Dashboard',
  },
  {
    path: '**',
    redirectTo: '',
  },
];
