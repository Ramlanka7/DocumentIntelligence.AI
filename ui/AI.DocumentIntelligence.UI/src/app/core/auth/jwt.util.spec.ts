import { decodeAccessToken } from './jwt.util';

function buildToken(claims: Record<string, unknown>): string {
  const header = { alg: 'none', typ: 'JWT' };
  const base64Url = (obj: unknown) =>
    btoa(JSON.stringify(obj)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `${base64Url(header)}.${base64Url(claims)}.signature`;
}

describe('jwt.util', () => {
  it('should decode sub/email/role/exp claims from a well-formed token', () => {
    const token = buildToken({ sub: 'abc-123', email: 'admin@example.com', role: 'Admin', exp: 9999999999 });
    const decoded = decodeAccessToken(token);

    expect(decoded).toEqual({ sub: 'abc-123', email: 'admin@example.com', role: 'Admin', exp: 9999999999 });
  });

  it('should return null for a malformed token', () => {
    expect(decodeAccessToken('not-a-jwt')).toBeNull();
  });

  it('should return null when required claims are missing', () => {
    const token = buildToken({ sub: 'abc-123' });
    expect(decodeAccessToken(token)).toBeNull();
  });
});
