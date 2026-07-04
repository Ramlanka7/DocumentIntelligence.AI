import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';

import { environment } from '../../../../environments/environment';
import { AnalysisApiService } from './analysis-api.service';
import { AnalysisResult } from '../models/analysis.models';

const API = environment.apiBaseUrl;

const mockResult: AnalysisResult = {
  executiveSummary: 'Test executive summary.',
  keyFindings: [
    {
      title: 'Finding 1',
      detail: 'Detail about finding 1.',
      citations: [
        {
          documentId: 'doc-1',
          documentName: 'Test.pdf',
          pageNumber: 1,
          paragraphReference: '§1.1',
          snippet: 'Snippet text.',
          confidenceScore: 0.95,
        },
      ],
    },
  ],
  risks: [
    {
      title: 'Risk 1',
      description: 'Risk description.',
      severity: 'High',
      citations: [],
    },
  ],
  recommendations: [
    {
      title: 'Recommendation 1',
      detail: 'Do something.',
      citations: [],
    },
  ],
  actionItems: [
    {
      description: 'Action 1',
      owner: 'Team Lead',
      citations: [],
    },
  ],
  sources: [
    {
      documentId: 'doc-1',
      documentName: 'Test.pdf',
      pageNumber: 2,
      paragraphReference: '§2.3',
      snippet: 'Source snippet.',
      confidenceScore: 0.88,
    },
  ],
};

describe('AnalysisApiService', () => {
  let service: AnalysisApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), AnalysisApiService],
    });
    service = TestBed.inject(AnalysisApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should initialise signals with defaults', () => {
    expect(service.analysisFiles()).toEqual([]);
    expect(service.loading()).toBe(false);
    expect(service.error()).toBeNull();
    expect(service.result()).toBeNull();
    expect(service.forbidden()).toBe(false);
  });

  it('should set and clear error via setError', () => {
    service.setError('Something went wrong');
    expect(service.error()).toBe('Something went wrong');
    service.setError(null);
    expect(service.error()).toBeNull();
  });

  it('should reset all state', () => {
    service.setError('oops');
    service.reset();
    expect(service.analysisFiles()).toEqual([]);
    expect(service.loading()).toBe(false);
    expect(service.error()).toBeNull();
    expect(service.result()).toBeNull();
    expect(service.forbidden()).toBe(false);
  });

  it('uploadDocument should POST to /documents (not /documents/upload)', () => {
    const file = new File(['content'], 'test.pdf', { type: 'application/pdf' });
    service.uploadDocument(file).subscribe();
    const req = httpMock.expectOne(`${API}/documents`);
    expect(req.request.method).toBe('POST');
    // Response uses documentId field per UploadDocumentResponse.cs
    req.flush({ documentId: 'doc-1', fileName: 'test.pdf', status: 'Pending' });
  });

  it('uploadDocument request should have multipart form data with "file" field', () => {
    const file = new File(['content'], 'test.pdf', { type: 'application/pdf' });
    service.uploadDocument(file).subscribe();
    const req = httpMock.expectOne(`${API}/documents`);
    const body = req.request.body as FormData;
    expect(body.get('file')).toBeTruthy();
    req.flush({ documentId: 'doc-1', fileName: 'test.pdf', status: 'Pending' });
  });

  it('analyzeDocuments should POST to /analysis', () => {
    const request = {
      documentIds: ['doc-1', 'doc-2'],
      capability: 'ExecutiveSummary' as const,
    };
    service.analyzeDocuments(request).subscribe();
    const req = httpMock.expectOne(`${API}/analysis`);
    expect(req.request.method).toBe('POST');
    req.flush(mockResult);
  });

  it('analyzeDocuments should include customQuestion when provided', () => {
    const request = {
      documentIds: ['doc-1'],
      capability: 'CustomQuestion' as const,
      customQuestion: 'What are the key risks?',
    };
    service.analyzeDocuments(request).subscribe();
    const req = httpMock.expectOne(`${API}/analysis`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.customQuestion).toBe('What are the key risks?');
    req.flush(mockResult);
  });

  it('should support all 8 backend AnalysisCapability enum values', () => {
    const capabilities = [
      'ExecutiveSummary',
      'KeyInsights',
      'ActionItems',
      'RiskAssessment',
      'ComplianceReview',
      'FinancialAnalysis',
      'SentimentAnalysis',
      'CustomQuestion',
    ] as const;
    expect(capabilities.length).toBe(8);
    // CustomQuestion is the correct name — not CustomQuestionAnswering
    expect(capabilities).toContain('CustomQuestion');
    expect(capabilities).not.toContain('CustomQuestionAnswering' as never);
  });

  it('analyzeDocuments result signal should carry full citation data', () => {
    service.analyzeDocuments({ documentIds: ['doc-1'], capability: 'KeyInsights' }).subscribe((res) => {
      expect(res.executiveSummary).toBe('Test executive summary.');
      expect(res.sources.length).toBe(1);
      expect(res.sources[0].paragraphReference).toBe('§2.3');
      expect(res.sources[0].confidenceScore).toBe(0.88);
      expect(res.sources[0].documentId).toBe('doc-1');
    });
    const req = httpMock.expectOne(`${API}/analysis`);
    req.flush(mockResult);
  });

  it('runAnalysis should set error signal (not mock data) on upload failure', fakeAsync(() => {
    const file = new File(['content'], 'bad.pdf', { type: 'application/pdf' });
    service.runAnalysis([file], 'ExecutiveSummary');

    expect(service.loading()).toBe(true);

    const req = httpMock.expectOne(`${API}/documents`);
    req.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
    tick();

    expect(service.loading()).toBe(false);
    expect(service.result()).toBeNull();
    expect(service.error()).toContain('bad.pdf');
    // No fabricated data
    expect(service.analysisFiles()[0].status).toBe('error');
  }));

  it('runAnalysis should set forbidden signal and error on 403 upload', fakeAsync(() => {
    const file = new File(['content'], 'test.pdf', { type: 'application/pdf' });
    service.runAnalysis([file], 'ExecutiveSummary');

    const req = httpMock.expectOne(`${API}/documents`);
    req.flush('Forbidden', { status: 403, statusText: 'Forbidden' });
    tick();

    expect(service.forbidden()).toBe(true);
    expect(service.error()).toContain('permission');
    expect(service.result()).toBeNull();
  }));

  it('runAnalysis should set error signal (not mock data) on analysis failure', fakeAsync(() => {
    const file = new File(['content'], 'test.pdf', { type: 'application/pdf' });
    service.runAnalysis([file], 'ExecutiveSummary');

    const uploadReq = httpMock.expectOne(`${API}/documents`);
    uploadReq.flush({ documentId: 'doc-1', fileName: 'test.pdf', status: 'Pending' });
    tick();

    const analysisReq = httpMock.expectOne(`${API}/analysis`);
    analysisReq.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
    tick();

    expect(service.loading()).toBe(false);
    expect(service.result()).toBeNull();
    expect(service.error()).toBeTruthy();
  }));
});
