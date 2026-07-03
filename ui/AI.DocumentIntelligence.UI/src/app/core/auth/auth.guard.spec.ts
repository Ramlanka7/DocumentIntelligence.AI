import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot, provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

import { authGuard } from './auth.guard';
import { AuthStore } from './auth-store';

function buildToken(claims: Record<string, unknown>): string {
  const header = { alg: 'none', typ: 'JWT' };
  const base64Url = (obj: unknown) =>
    btoa(JSON.stringify(obj)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `${base64Url(header)}.${base64Url(claims)}.signature`;
}

describe('authGuard', () => {
  beforeEach(() => {
    localStorage.clear(); // guard tests must not inherit tokens written by other specs
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  it('should allow navigation when authenticated', () => {
    const store = TestBed.inject(AuthStore);
    const token = buildToken({
      sub: 'user-1',
      email: 'viewer@example.com',
      role: 'Viewer',
      exp: Math.floor(Date.now() / 1000) + 3600,
    });
    store.applyTokens(token, 'refresh-token', new Date(Date.now() + 3600_000).toISOString());

    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, { url: '/analysis' } as RouterStateSnapshot),
    );

    expect(result).toBe(true);
  });

  it('should redirect unauthenticated users to /login with a returnUrl', () => {
    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, { url: '/analysis' } as RouterStateSnapshot),
    );

    expect(result).not.toBe(true);
  });
});
