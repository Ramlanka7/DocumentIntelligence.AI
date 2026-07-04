import { HttpErrorResponse, HttpEvent, HttpHandlerFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';

import { ProblemDetails } from '../models/api-error.model';
import { NotificationService } from '../services/notification.service';

function extractMessage(error: HttpErrorResponse): string {
  const problem = error.error as ProblemDetails | null;

  if (problem?.detail) {
    return problem.detail;
  }
  if (problem?.title) {
    return problem.title;
  }
  if (error.status === 0) {
    return 'Unable to reach the server. Please check your connection and try again.';
  }
  if (error.status === 401) {
    return 'Your session has expired. Please sign in again.';
  }
  if (error.status === 403) {
    return 'You do not have permission to perform this action.';
  }
  if (error.status >= 500) {
    return 'An unexpected server error occurred. Please try again later.';
  }
  return error.message || 'An unexpected error occurred.';
}

/**
 * Functional HTTP interceptor that surfaces failed requests as toast notifications.
 * Runs after the auth interceptor, so a 401 that was successfully recovered via token
 * refresh never reaches here — only genuinely unrecoverable errors do.
 */
export function errorInterceptor(
  request: HttpRequest<unknown>,
  next: HttpHandlerFn,
): Observable<HttpEvent<unknown>> {
  const notifications = inject(NotificationService);

  return next(request).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        notifications.error(extractMessage(error));
      }
      return throwError(() => error);
    }),
  );
}
