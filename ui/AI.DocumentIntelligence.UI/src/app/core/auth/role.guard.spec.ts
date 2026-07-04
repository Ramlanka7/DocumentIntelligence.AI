import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot, provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

import { roleGuard } from './role.guard';
import { AuthStore } from './auth-store';

function buildToken(claims: Record<string, unknown>): string {
  const header = { alg: 'none', typ: 'JWT' };
  const base64Url = (obj: unknown) =>
    btoa(JSON.stringify(obj)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `${base64Url(header)}.${base64Url(claims)}.signature`;
}

function routeSnapshotWithRoles(roles: string[]): ActivatedRouteSnapshot {
  return { data: { roles } } as unknown as ActivatedRouteSnapshot;
}

describe('roleGuard', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  function signInAs(role: string): void {
    const store = TestBed.inject(AuthStore);
    const token = buildToken({
      sub: 'user-1',
      email: 'user@example.com',
      role,
      exp: Math.floor(Date.now() / 1000) + 3600,
    });
    store.applyTokens(token, 'refresh-token', new Date(Date.now() + 3600_000).toISOString());
  }

  it('should allow access when the user role is in the allowed list', () => {
    signInAs('Analyst');

    const result = TestBed.runInInjectionContext(() =>
      roleGuard(routeSnapshotWithRoles(['Admin', 'Analyst']), {} as RouterStateSnapshot),
    );

    expect(result).toBe(true);
  });

  it('should deny access and redirect when the user role is not in the allowed list', () => {
    signInAs('Viewer');

    const result = TestBed.runInInjectionContext(() =>
      roleGuard(routeSnapshotWithRoles(['Admin']), {} as RouterStateSnapshot),
    );

    expect(result).not.toBe(true);
  });

  it('should redirect to login when there is no authenticated user', () => {
    const result = TestBed.runInInjectionContext(() =>
      roleGuard(routeSnapshotWithRoles(['Admin']), {} as RouterStateSnapshot),
    );

    expect(result).not.toBe(true);
  });
});
