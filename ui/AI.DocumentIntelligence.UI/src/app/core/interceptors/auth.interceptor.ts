import { HttpErrorResponse, HttpEvent, HttpHandlerFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, Subject, catchError, switchMap, take, throwError } from 'rxjs';

import { AuthStore } from '../auth/auth-store';
import { AuthApiService } from '../auth/auth-api.service';
import { TokenStorageService } from '../auth/token-storage.service';
import { SKIP_AUTH_REFRESH } from './auth-context.token';

// Module-level (singleton) refresh coordination state so concurrent 401s from multiple
// in-flight requests trigger only one refresh call and all wait on its result.
let isRefreshing = false;
const refreshCompleted$ = new Subject<string | null>();

function withBearerToken(request: HttpRequest<unknown>, accessToken: string | null): HttpRequest<unknown> {
  if (!accessToken) {
    return request;
  }
  return request.clone({ setHeaders: { Authorization: `Bearer ${accessToken}` } });
}

/**
 * Functional HTTP interceptor (Angular 20 convention) that:
 * 1. Attaches the current Bearer access token to outgoing requests.
 * 2. On a 401 response, attempts a single token refresh and retries the original request.
 * 3. If refresh also fails, clears the session so the auth guard redirects to `/login`.
 *
 * Requests tagged with `SKIP_AUTH_REFRESH` (login/refresh themselves) are passed through
 * untouched to avoid recursive refresh loops.
 */
export function authInterceptor(
  request: HttpRequest<unknown>,
  next: HttpHandlerFn,
): Observable<HttpEvent<unknown>> {
  const authStore = inject(AuthStore);
  const authApi = inject(AuthApiService);
  const tokenStorage = inject(TokenStorageService);

  if (request.context.get(SKIP_AUTH_REFRESH)) {
    return next(request);
  }

  const authorizedRequest = withBearerToken(request, authStore.accessToken());

  return next(authorizedRequest).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }

      const refreshToken = authStore.refreshToken();
      if (!refreshToken) {
        authStore.clearSession();
        return throwError(() => error);
      }

      if (isRefreshing) {
        // A refresh is already in flight — wait for it to complete, then retry.
        return refreshCompleted$.pipe(
          take(1),
          switchMap((newAccessToken) => {
            if (!newAccessToken) {
              return throwError(() => error);
            }
            return next(withBearerToken(request, newAccessToken));
          }),
        );
      }

      isRefreshing = true;

      return authApi.refresh({ refreshToken }).pipe(
        switchMap((tokens) => {
          authStore.applyTokens(tokens.accessToken, tokens.refreshToken, tokens.expiresAt);
          tokenStorage.save(tokens);
          isRefreshing = false;
          refreshCompleted$.next(tokens.accessToken);
          return next(withBearerToken(request, tokens.accessToken));
        }),
        catchError((refreshError: unknown) => {
          isRefreshing = false;
          refreshCompleted$.next(null);
          authStore.clearSession();
          return throwError(() => refreshError);
        }),
      );
    }),
  );
}
