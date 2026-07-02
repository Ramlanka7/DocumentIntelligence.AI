import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, provideRouter, RouterStateSnapshot } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

import { guestGuard } from './guest.guard';
import { AuthStore } from './auth-store';

const mockRoute = {} as ActivatedRouteSnapshot;
const mockState = {} as RouterStateSnapshot;

function buildToken(claims: Record<string, unknown>): string {
  const header = { alg: 'none', typ: 'JWT' };
  const base64Url = (obj: unknown) =>
    btoa(JSON.stringify(obj)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `${base64Url(header)}.${base64Url(claims)}.signature`;
}

describe('guestGuard', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  it('should allow access to guest routes when unauthenticated', () => {
    const result = TestBed.runInInjectionContext(() => guestGuard(mockRoute, mockState));
    expect(result).toBe(true);
  });

  it('should redirect authenticated users away from guest routes', () => {
    const store = TestBed.inject(AuthStore);
    const token = buildToken({
      sub: 'user-1',
      email: 'user@example.com',
      role: 'Viewer',
      exp: Math.floor(Date.now() / 1000) + 3600,
    });
    store.applyTokens(token, 'refresh-token', new Date(Date.now() + 3600_000).toISOString());

    const result = TestBed.runInInjectionContext(() => guestGuard(mockRoute, mockState));
    expect(result).not.toBe(true);
  });
});
