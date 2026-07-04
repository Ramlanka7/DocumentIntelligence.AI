import { Injectable } from '@angular/core';

import { AuthTokenResponse } from '../models/auth.model';

const ACCESS_TOKEN_KEY = 'adip.accessToken';
const REFRESH_TOKEN_KEY = 'adip.refreshToken';
const EXPIRES_AT_KEY = 'adip.expiresAt';

/** Persisted token snapshot read back from storage on app bootstrap. */
export interface StoredTokens {
  readonly accessToken: string;
  readonly refreshToken: string;
  readonly expiresAt: string;
}

/**
 * Thin wrapper around `localStorage` for JWT/refresh-token persistence, isolated so the
 * auth store never touches the Web Storage API directly (and so this is easy to swap for
 * a more secure storage mechanism later without touching call sites).
 */
@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  save(tokens: AuthTokenResponse): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, tokens.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, tokens.refreshToken);
    localStorage.setItem(EXPIRES_AT_KEY, tokens.expiresAt);
  }

  read(): StoredTokens | null {
    const accessToken = localStorage.getItem(ACCESS_TOKEN_KEY);
    const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY);
    const expiresAt = localStorage.getItem(EXPIRES_AT_KEY);

    if (!accessToken || !refreshToken || !expiresAt) {
      return null;
    }

    return { accessToken, refreshToken, expiresAt };
  }

  clear(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(EXPIRES_AT_KEY);
  }
}
