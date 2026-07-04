import { DecodedAccessToken, UserRole } from '../models/auth.model';

const ROLE_CLAIM_KEYS = [
  'role',
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role',
] as const;

const NAME_ID_CLAIM_KEYS = [
  'sub',
  'nameid',
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier',
] as const;

const EMAIL_CLAIM_KEYS = [
  'email',
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress',
] as const;

/** Raw claim bag decoded from a JWT payload — values are typically strings or numbers. */
type JwtClaims = Readonly<Record<string, string | number | undefined>>;

function base64UrlDecode(segment: string): string {
  const normalized = segment.replace(/-/g, '+').replace(/_/g, '/');
  const padded = normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), '=');
  return atob(padded);
}

function firstClaim(claims: JwtClaims, keys: readonly string[]): string | undefined {
  for (const key of keys) {
    const value = claims[key];
    if (typeof value === 'string' && value.length > 0) {
      return value;
    }
  }
  return undefined;
}

/**
 * Parses a JWT access token client-side (no signature verification — the server is the
 * source of truth for validity) to extract the claims the auth store needs to render UI
 * and enforce role-based route guards.
 */
export function decodeAccessToken(token: string): DecodedAccessToken | null {
  const segments = token.split('.');
  if (segments.length !== 3) {
    return null;
  }

  try {
    const payload = JSON.parse(base64UrlDecode(segments[1])) as JwtClaims;
    const sub = firstClaim(payload, NAME_ID_CLAIM_KEYS);
    const email = firstClaim(payload, EMAIL_CLAIM_KEYS);
    const role = firstClaim(payload, ROLE_CLAIM_KEYS);
    const exp = payload['exp'];

    if (!sub || !email || !role || typeof exp !== 'number') {
      return null;
    }

    return { sub, email, role: role as UserRole, exp };
  } catch {
    return null;
  }
}
