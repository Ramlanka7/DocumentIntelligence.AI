import { provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';

import { Landing } from './landing';

describe('Landing', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Landing],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(Landing);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render the exact hero title and subtitle from the spec', () => {
    const fixture = TestBed.createComponent(Landing);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('.landing__title')?.textContent?.trim()).toBe(
      'AI Document Intelligence Platform',
    );
    expect(compiled.querySelector('.landing__subtitle')?.textContent?.trim()).toBe(
      'Analyze, compare, and understand complex documents using advanced AI reasoning and enterprise-grade document intelligence.',
    );
  });

  it('should render the Analysis Mode and Comparison Mode feature cards linking to /analysis and /compare', () => {
    const fixture = TestBed.createComponent(Landing);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const cardTitles = Array.from(compiled.querySelectorAll('mat-card-title')).map((el) =>
      el.textContent?.trim(),
    );

    expect(cardTitles).toContain('Analysis Mode');
    expect(cardTitles).toContain('Comparison Mode');

    const hrefs = Array.from(compiled.querySelectorAll('a')).map((el) => el.getAttribute('href'));
    expect(hrefs).toContain('/analysis');
    expect(hrefs).toContain('/compare');
  });
});
