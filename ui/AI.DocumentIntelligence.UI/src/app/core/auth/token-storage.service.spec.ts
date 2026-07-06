import { TestBed } from '@angular/core/testing';

import { TokenStorageService } from './token-storage.service';
import { AuthTokenResponse } from '../models/auth.model';

describe('TokenStorageService', () => {
  let service: TokenStorageService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(TokenStorageService);
    service.clear();
  });

  it('should return null when nothing is stored', () => {
    expect(service.read()).toBeNull();
  });

  it('should persist and read back a token response', () => {
    const tokens: AuthTokenResponse = {
      accessToken: 'access',
      expiresAt: new Date().toISOString(),
    };

    service.save(tokens);

    expect(service.read()).toEqual({
      accessToken: tokens.accessToken,
      expiresAt: tokens.expiresAt,
    });
  });

  it('should clear persisted tokens', () => {
    service.save({ accessToken: 'a', expiresAt: new Date().toISOString() });
    service.clear();
    expect(service.read()).toBeNull();
  });
});
