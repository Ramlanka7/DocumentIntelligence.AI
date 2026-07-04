import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { authInterceptor } from './auth.interceptor';
import { AuthStore } from '../auth/auth-store';
import { environment } from '../../../environments/environment';

function buildToken(claims: Record<string, unknown>): string {
  const header = { alg: 'none', typ: 'JWT' };
  const base64Url = (obj: unknown) =>
    btoa(JSON.stringify(obj)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `${base64Url(header)}.${base64Url(claims)}.signature`;
}

describe('authInterceptor', () => {
  let httpClient: HttpClient;
  let httpMock: HttpTestingController;
  let authStore: AuthStore;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([authInterceptor])), provideHttpClientTesting()],
    });

    httpClient = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    authStore = TestBed.inject(AuthStore);

    const token = buildToken({
      sub: 'user-1',
      email: 'analyst@example.com',
      role: 'Analyst',
      exp: Math.floor(Date.now() / 1000) + 3600,
    });
    authStore.applyTokens(token, 'refresh-token', new Date(Date.now() + 3600_000).toISOString());
  });

  afterEach(() => httpMock.verify());

  it('should attach the Bearer access token to outgoing requests', () => {
    httpClient.get('/api/v1/documents').subscribe();

    const req = httpMock.expectOne('/api/v1/documents');
    expect(req.request.headers.get('Authorization')).toBe(`Bearer ${authStore.accessToken()}`);
    req.flush({});
  });

  it('should refresh the token and retry the original request on a 401', () => {
    const newAccessToken = buildToken({
      sub: 'user-1',
      email: 'analyst@example.com',
      role: 'Analyst',
      exp: Math.floor(Date.now() / 1000) + 7200,
    });

    let result: unknown;
    httpClient.get('/api/v1/documents').subscribe((response) => (result = response));

    const firstReq = httpMock.expectOne('/api/v1/documents');
    firstReq.flush({ title: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    const refreshReq = httpMock.expectOne(`${environment.apiBaseUrl}/auth/refresh`);
    expect(refreshReq.request.body).toEqual({ refreshToken: 'refresh-token' });
    refreshReq.flush({
      accessToken: newAccessToken,
      refreshToken: 'rotated-refresh-token',
      expiresAt: new Date(Date.now() + 3600_000).toISOString(),
    });

    const retryReq = httpMock.expectOne('/api/v1/documents');
    expect(retryReq.request.headers.get('Authorization')).toBe(`Bearer ${newAccessToken}`);
    retryReq.flush({ ok: true });

    expect(result).toEqual({ ok: true });
    expect(authStore.accessToken()).toBe(newAccessToken);
  });

  it('should clear the session when refresh also fails after a 401', () => {
    let sawError = false;
    httpClient.get('/api/v1/documents').subscribe({
      next: () => fail('expected an error'),
      error: () => (sawError = true),
    });

    const firstReq = httpMock.expectOne('/api/v1/documents');
    firstReq.flush({ title: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    const refreshReq = httpMock.expectOne(`${environment.apiBaseUrl}/auth/refresh`);
    refreshReq.flush({ title: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    expect(sawError).toBeTrue();
    expect(authStore.isAuthenticated()).toBeFalse();
  });
});
