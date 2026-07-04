import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, map, of, tap } from 'rxjs';

import { AuthenticatedUser, AuthTokenResponse, LoginRequest } from '../models/auth.model';
import { AuthApiService } from './auth-api.service';
import { decodeAccessToken } from './jwt.util';
import { TokenStorageService } from './token-storage.service';

/**
 * Signal-based auth state store — the single source of truth for the current session.
 * Access tokens are never held anywhere else; the HTTP auth interceptor and route guards
 * both read from this store.
 */
@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly authApi = inject(AuthApiService);
  private readonly tokenStorage = inject(TokenStorageService);

  private readonly accessTokenSignal = signal<string | null>(null);
  private readonly refreshTokenSignal = signal<string | null>(null);
  private readonly userSignal = signal<AuthenticatedUser | null>(null);

  /** Current Bearer access token, or `null` when signed out. */
  readonly accessToken = this.accessTokenSignal.asReadonly();
  /** Current refresh token, or `null` when signed out. */
  readonly refreshToken = this.refreshTokenSignal.asReadonly();
  /** Decoded identity of the current user, or `null` when signed out. */
  readonly user = this.userSignal.asReadonly();
  /** True once a user is signed in with a (not-yet-necessarily-fresh) access token. */
  readonly isAuthenticated = computed(() => this.userSignal() !== null);

  constructor() {
    this.restoreFromStorage();
  }

  /** Re-hydrates in-memory signals from persisted storage (called on app bootstrap). */
  restoreFromStorage(): void {
    const stored = this.tokenStorage.read();
    if (!stored) {
      return;
    }

    const decoded = decodeAccessToken(stored.accessToken);
    if (!decoded) {
      this.tokenStorage.clear();
      return;
    }

    this.accessTokenSignal.set(stored.accessToken);
    this.refreshTokenSignal.set(stored.refreshToken);
    this.userSignal.set({ id: decoded.sub, email: decoded.email, role: decoded.role });
  }

  login(request: LoginRequest): Observable<AuthenticatedUser | null> {
    return this.authApi.login(request).pipe(
      tap((tokens: AuthTokenResponse) => {
        this.applyTokens(tokens.accessToken, tokens.refreshToken, tokens.expiresAt);
        this.tokenStorage.save(tokens);
      }),
      map(() => this.userSignal()),
      catchError((error: unknown) => {
        this.clearSession();
        throw error;
      }),
    );
  }

  /**
   * Applies a fresh access/refresh token pair to the in-memory signals only (no HTTP call,
   * no storage write) — used internally by `login`/`refresh` after the network round trip.
   */
  applyTokens(accessToken: string, refreshToken: string, _expiresAt: string): void {
    const decoded = decodeAccessToken(accessToken);
    this.accessTokenSignal.set(accessToken);
    this.refreshTokenSignal.set(refreshToken);
    this.userSignal.set(decoded ? { id: decoded.sub, email: decoded.email, role: decoded.role } : null);
  }

  /** Clears in-memory signals and persisted storage — used on logout or unrecoverable 401. */
  clearSession(): void {
    this.accessTokenSignal.set(null);
    this.refreshTokenSignal.set(null);
    this.userSignal.set(null);
    this.tokenStorage.clear();
  }

  logout(): Observable<void> {
    return this.authApi.logout().pipe(
      tap(() => this.clearSession()),
      catchError(() => {
        // Even if server-side revocation fails (e.g. token already expired), the client
        // session must still end.
        this.clearSession();
        return of(undefined);
      }),
    );
  }
}
