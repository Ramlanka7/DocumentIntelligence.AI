import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

import { Login } from './login';

describe('Login', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Login],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(Login);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should mark the form invalid when fields are empty', () => {
    const fixture = TestBed.createComponent(Login);
    const login = fixture.componentInstance;
    expect(login['form'].valid).toBeFalse();
  });

  it('should mark the form valid with a well-formed email and long-enough password', () => {
    const fixture = TestBed.createComponent(Login);
    const login = fixture.componentInstance;
    login['form'].setValue({ email: 'user@example.com', password: 'password123' });
    expect(login['form'].valid).toBeTrue();
  });

  it('should not submit while the form is invalid', () => {
    const fixture = TestBed.createComponent(Login);
    const login = fixture.componentInstance;
    login['submit']();
    expect(login['isSubmitting']()).toBeFalse();
  });
});
