/** The exact role claim strings issued by the backend JWT (see AI.DocumentIntelligence.Domain.Enums.UserRole). */
export type UserRole = 'Admin' | 'Analyst' | 'Viewer';

/** Request body for `POST /api/v1/auth/login`. */
export interface LoginRequest {
  readonly email: string;
  readonly password: string;
}

/** Request body for `POST /api/v1/auth/refresh`. */
export interface RefreshTokenRequest {
  readonly refreshToken: string;
}

/** Request body for `POST /api/v1/auth/register` (Admin-only). */
export interface RegisterRequest {
  readonly email: string;
  readonly password: string;
  readonly fullName: string;
  /** 0 = Admin, 1 = Analyst, 2 = Viewer — matches AI.DocumentIntelligence.Domain.Enums.UserRole. */
  readonly role: 0 | 1 | 2;
}

/** Response body shared by `login` and `refresh`. */
export interface AuthTokenResponse {
  readonly accessToken: string;
  readonly refreshToken: string;
  /** ISO 8601 DateTimeOffset string. */
  readonly expiresAt: string;
}

/** Claims of interest decoded from the JWT access token payload. */
export interface DecodedAccessToken {
  readonly sub: string;
  readonly email: string;
  readonly role: UserRole;
  readonly exp: number;
}

/** Signal-based auth state store's public snapshot shape. */
export interface AuthenticatedUser {
  readonly id: string;
  readonly email: string;
  readonly role: UserRole;
}
