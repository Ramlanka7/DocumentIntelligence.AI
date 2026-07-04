/**
 * Shape of the RFC 7807 ProblemDetails payload returned by the global exception
 * handling middleware and by `Result.ToActionResult` failures on the API.
 */
export interface ProblemDetails {
  readonly type?: string;
  readonly title?: string;
  readonly status?: number;
  readonly detail?: string;
  readonly instance?: string;
  readonly errors?: Readonly<Record<string, readonly string[]>>;
}

/** Normalized error used by the UI (toast notifications, inline form errors, etc.). */
export interface AppError {
  readonly status: number;
  readonly message: string;
  readonly details?: ReadonlyArray<string>;
}
