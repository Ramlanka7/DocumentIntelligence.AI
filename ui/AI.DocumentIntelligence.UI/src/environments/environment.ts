/**
 * Production environment configuration.
 * The API is served behind the same origin in production (see nginx.conf), so the
 * base URL is relative — no CORS/proxy needed once deployed.
 */
export interface Environment {
  readonly production: boolean;
  readonly apiBaseUrl: string;
}

export const environment: Environment = {
  production: true,
  apiBaseUrl: '/api/v1',
};
