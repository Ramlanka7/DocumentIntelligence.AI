import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';

import { AnalysisComponent } from './analysis';
import { AnalysisApiService } from './services/analysis-api.service';
import { AnalysisResult, AnalysisFile } from './models/analysis.models';

const mockResult: AnalysisResult = {
  executiveSummary: 'Summary text.',
  keyFindings: [],
  risks: [],
  recommendations: [],
  actionItems: [],
  sources: [
    {
      documentId: 'doc-1',
      documentName: 'Report.pdf',
      pageNumber: 3,
      paragraphReference: '§1.2',
      snippet: 'Supporting text.',
      confidenceScore: 0.93,
    },
  ],
};

describe('AnalysisComponent', () => {
  let fixture: ComponentFixture<AnalysisComponent>;
  let component: AnalysisComponent;

  const loadingSig = signal(false);
  const errorSig = signal<string | null>(null);
  const resultSig = signal<AnalysisResult | null>(null);
  const forbiddenSig = signal(false);
  const filesSig = signal<AnalysisFile[]>([]);

  const mockService: Partial<AnalysisApiService> = {
    loading: loadingSig.asReadonly(),
    error: errorSig.asReadonly(),
    result: resultSig.asReadonly(),
    forbidden: forbiddenSig.asReadonly(),
    analysisFiles: filesSig.asReadonly(),
    setError: jasmine.createSpy('setError'),
    reset: jasmine.createSpy('reset'),
    runAnalysis: jasmine.createSpy('runAnalysis'),
  };

  beforeEach(async () => {
    // Reset signal state before each test
    loadingSig.set(false);
    errorSig.set(null);
    resultSig.set(null);
    forbiddenSig.set(false);
    filesSig.set([]);

    await TestBed.configureTestingModule({
      imports: [AnalysisComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AnalysisApiService, useValue: mockService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AnalysisComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render the page header with correct title', () => {
    const h1 = fixture.nativeElement.querySelector('h1') as HTMLElement | null;
    expect(h1?.textContent).toContain('Document Analysis');
  });

  it('should show the upload section by default', () => {
    const uploadSection = fixture.nativeElement.querySelector('app-analysis-upload');
    expect(uploadSection).toBeTruthy();
  });

  it('should show the capability selector by default', () => {
    const selector = fixture.nativeElement.querySelector('app-capability-selector');
    expect(selector).toBeTruthy();
  });

  it('should disable analyse button when no files are selected', () => {
    const btn = fixture.nativeElement.querySelector('.analyse-btn') as HTMLButtonElement | null;
    expect(btn?.disabled).toBeTrue();
  });

  it('should call service.reset on reset()', () => {
    (component as unknown as { reset: () => void }).reset();
    expect(mockService.reset).toHaveBeenCalled();
  });

  it('should show loading overlay when service.loading() is true', () => {
    loadingSig.set(true);
    fixture.detectChanges();
    const overlay = fixture.nativeElement.querySelector('.loading-overlay');
    expect(overlay).toBeTruthy();
  });

  it('should show error banner when service.error() is set', () => {
    errorSig.set('Something went wrong');
    fixture.detectChanges();
    const banner = fixture.nativeElement.querySelector('.error-banner');
    expect(banner).toBeTruthy();
    expect(banner.textContent).toContain('Something went wrong');
  });

  it('should show analysis results when service.result() returns a value', () => {
    resultSig.set(mockResult);
    fixture.detectChanges();
    const results = fixture.nativeElement.querySelector('app-analysis-results');
    expect(results).toBeTruthy();
  });

  it('should not show setup form when results are available', () => {
    resultSig.set(mockResult);
    fixture.detectChanges();
    const setup = fixture.nativeElement.querySelector('.analysis-setup');
    expect(setup).toBeFalsy();
  });

  it('should show forbidden-styled banner when service.forbidden() is true', () => {
    errorSig.set('Permission denied');
    forbiddenSig.set(true);
    fixture.detectChanges();
    const banner = fixture.nativeElement.querySelector('.error-banner--forbidden');
    expect(banner).toBeTruthy();
  });
});
