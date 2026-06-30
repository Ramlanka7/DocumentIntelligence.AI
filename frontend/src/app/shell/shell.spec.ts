import { TestBed } from '@angular/core/testing';
import { Shell } from './shell';

describe('Shell', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Shell],
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
});
