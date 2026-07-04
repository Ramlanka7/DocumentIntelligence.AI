import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthTokenResponse, LoginRequest, RefreshTokenRequest, RegisterRequest } from '../models/auth.model';
import { SKIP_AUTH_REFRESH } from '../interceptors/auth-context.token';

/**
 * Typed HTTP client for the `/api/v1/auth` endpoints (T08 `AuthController`).
 * Kept deliberately thin — no business logic, only request/response typing.
 */
@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/auth`;

  login(request: LoginRequest): Observable<AuthTokenResponse> {
    return this.http.post<AuthTokenResponse>(`${this.baseUrl}/login`, request, {
      context: new HttpContext().set(SKIP_AUTH_REFRESH, true),
    });
  }

  refresh(request: RefreshTokenRequest): Observable<AuthTokenResponse> {
    return this.http.post<AuthTokenResponse>(`${this.baseUrl}/refresh`, request, {
      context: new HttpContext().set(SKIP_AUTH_REFRESH, true),
    });
  }

  logout(): Observable<void> {
    // Session revocation must not itself trigger a refresh-and-retry round trip on a 401 —
    // the session is being torn down either way, so skip straight to clearing it client-side.
    return this.http.post<void>(`${this.baseUrl}/logout`, {}, {
      context: new HttpContext().set(SKIP_AUTH_REFRESH, true),
    });
  }

  /** Admin-only. Scaffolded for future admin-dashboard (T13) user management. */
  register(request: RegisterRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/register`, request);
  }
}
