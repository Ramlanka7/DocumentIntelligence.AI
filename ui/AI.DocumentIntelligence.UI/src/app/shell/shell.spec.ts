import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

import { Shell } from './shell';

describe('Shell', () => {
  beforeEach(async () => {
    localStorage.clear(); // prevent auth-store.spec.ts from leaking tokens into this suite
    await TestBed.configureTestingModule({
      imports: [Shell],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('should create the shell', () => {
    const fixture = TestBed.createComponent(Shell);
    const shell = fixture.componentInstance;
    expect(shell).toBeTruthy();
  });

  it('should render the platform title', () => {
    const fixture = TestBed.createComponent(Shell);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('AI Document Intelligence Platform');
  });

  it('should show a sign-in link when unauthenticated', () => {
    const fixture = TestBed.createComponent(Shell);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Sign in');
  });
});
