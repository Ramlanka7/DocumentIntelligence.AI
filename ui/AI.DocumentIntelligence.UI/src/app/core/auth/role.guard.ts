import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn, Router } from '@angular/router';

import { UserRole } from '../models/auth.model';
import { AuthStore } from './auth-store';

/** Route data key read by `roleGuard` — set via `data: { roles: [...] }` on a route. */
export interface RoleGuardData {
  readonly roles: readonly UserRole[];
}

/**
 * Restricts a route to one or more roles (exact JWT `role` claim values: `Admin`,
 * `Analyst`, `Viewer`). Assumes `authGuard` already ran and the user is authenticated;
 * unauthenticated users are still redirected to `/login` defensively.
 */
export const roleGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const authStore = inject(AuthStore);
  const router = inject(Router);

  const user = authStore.user();
  if (!user) {
    return router.createUrlTree(['/login']);
  }

  const allowedRoles = (route.data as Partial<RoleGuardData>).roles ?? [];
  if (allowedRoles.length === 0 || allowedRoles.includes(user.role)) {
    return true;
  }

  return router.createUrlTree(['/']);
};
