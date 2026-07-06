import { Injectable } from '@angular/core';

import { AuthTokenResponse } from '../models/auth.model';

const ACCESS_TOKEN_KEY = 'adip.accessToken';
const EXPIRES_AT_KEY = 'adip.expiresAt';
const LEGACY_REFRESH_TOKEN_KEY = 'adip.refreshToken';

/** Persisted token snapshot read back from storage on app bootstrap. */
export interface StoredTokens {
  readonly accessToken: string;
  readonly expiresAt: string;
}

/**
 * Thin wrapper around `localStorage` for the short-lived access token only.
 * The long-lived refresh token is NEVER stored here — it lives in an HttpOnly cookie
 * set by the server, out of reach of any script (XSS cannot exfiltrate it).
 */
@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  save(tokens: AuthTokenResponse): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, tokens.accessToken);
    localStorage.setItem(EXPIRES_AT_KEY, tokens.expiresAt);
  }

  read(): StoredTokens | null {
    // One-time cleanup for sessions created before the HttpOnly-cookie migration.
    localStorage.removeItem(LEGACY_REFRESH_TOKEN_KEY);

    const accessToken = localStorage.getItem(ACCESS_TOKEN_KEY);
    const expiresAt = localStorage.getItem(EXPIRES_AT_KEY);

    if (!accessToken || !expiresAt) {
      return null;
    }

    return { accessToken, expiresAt };
  }

  clear(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(LEGACY_REFRESH_TOKEN_KEY);
    localStorage.removeItem(EXPIRES_AT_KEY);
  }
}
