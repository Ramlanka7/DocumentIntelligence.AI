import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { AuthApiService } from './auth-api.service';
import { environment } from '../../../environments/environment';
import { AuthTokenResponse } from '../models/auth.model';

describe('AuthApiService', () => {
  let service: AuthApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should POST to /auth/login with the given credentials', () => {
    const response: AuthTokenResponse = {
      accessToken: 'access',
      expiresAt: new Date().toISOString(),
    };

    service.login({ email: 'user@example.com', password: 'password123' }).subscribe((result) => {
      expect(result).toEqual(response);
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/auth/login`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'user@example.com', password: 'password123' });
    // The refresh token travels only in the HttpOnly cookie, so credentials must be on.
    expect(req.request.withCredentials).toBeTrue();
    req.flush(response);
  });

  it('should POST to /auth/refresh with an empty body (token travels in the cookie)', () => {
    const response: AuthTokenResponse = {
      accessToken: 'new-access',
      expiresAt: new Date().toISOString(),
    };

    service.refresh().subscribe((result) => {
      expect(result).toEqual(response);
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/auth/refresh`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({});
    expect(req.request.withCredentials).toBeTrue();
    req.flush(response);
  });

  it('should POST to /auth/logout with no body', () => {
    service.logout().subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/auth/logout`);
    expect(req.request.method).toBe('POST');
    req.flush(null, { status: 204, statusText: 'No Content' });
  });
});
