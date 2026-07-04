import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

import { AuthStore } from './auth-store';
import { TokenStorageService } from './token-storage.service';

// A syntactically valid (unsigned/placeholder) JWT with header/payload/signature segments,
// carrying { sub, email, role, exp } claims — sufficient for client-side decoding tests.
function buildFakeAccessToken(claims: Record<string, unknown>): string {
  const header = { alg: 'none', typ: 'JWT' };
  const base64Url = (obj: unknown) =>
    btoa(JSON.stringify(obj)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `${base64Url(header)}.${base64Url(claims)}.signature`;
}

describe('AuthStore', () => {
  let tokenStorage: TokenStorageService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    tokenStorage = TestBed.inject(TokenStorageService);
    tokenStorage.clear();
  });

  it('should start unauthenticated when storage is empty', () => {
    const store = TestBed.inject(AuthStore);
    expect(store.isAuthenticated()).toBeFalse();
    expect(store.user()).toBeNull();
  });

  it('should restore an authenticated user from a valid stored access token', () => {
    const accessToken = buildFakeAccessToken({
      sub: 'user-1',
      email: 'analyst@example.com',
      role: 'Analyst',
      exp: Math.floor(Date.now() / 1000) + 3600,
    });
    tokenStorage.save({
      accessToken,
      refreshToken: 'refresh-token',
      expiresAt: new Date(Date.now() + 3600_000).toISOString(),
    });

    const store = TestBed.inject(AuthStore);

    expect(store.isAuthenticated()).toBeTrue();
    expect(store.user()).toEqual({ id: 'user-1', email: 'analyst@example.com', role: 'Analyst' });
  });

  it('should clear the session on clearSession()', () => {
    const accessToken = buildFakeAccessToken({
      sub: 'user-1',
      email: 'analyst@example.com',
      role: 'Analyst',
      exp: Math.floor(Date.now() / 1000) + 3600,
    });
    tokenStorage.save({
      accessToken,
      refreshToken: 'refresh-token',
      expiresAt: new Date(Date.now() + 3600_000).toISOString(),
    });

    const store = TestBed.inject(AuthStore);
    expect(store.isAuthenticated()).toBeTrue();

    store.clearSession();

    expect(store.isAuthenticated()).toBeFalse();
    expect(store.user()).toBeNull();
    expect(tokenStorage.read()).toBeNull();
  });
});
