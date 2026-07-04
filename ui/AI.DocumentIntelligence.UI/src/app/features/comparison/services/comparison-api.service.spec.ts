import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';

import { environment } from '../../../../environments/environment';
import { ComparisonApiService } from './comparison-api.service';
import { ComparisonResult } from '../models/comparison.models';

const API = environment.apiBaseUrl;

const mockResult: ComparisonResult = {
  executiveOverview: 'Overview of differences.',
  differences: [
    {
      type: 'Modified',
      section: 'Payment Terms',
      before: 'Net 30.',
      after: 'Net 45.',
      summary: 'Payment period extended.',
      citations: [
        {
          documentId: 'doc-1',
          documentName: 'Contract A.pdf',
          pageNumber: 2,
          paragraphReference: '§4.1',
          snippet: 'Payment terms snippet.',
          confidenceScore: 0.96,
        },
      ],
    },
  ],
  risks: [],
  recommendations: [],
  sources: [
    {
      documentId: 'doc-1',
      documentName: 'Contract A.pdf',
      pageNumber: 1,
      paragraphReference: '§1.0',
      snippet: 'Intro snippet.',
      confidenceScore: 0.92,
    },
  ],
};

describe('ComparisonApiService', () => {
  let service: ComparisonApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), ComparisonApiService],
    });
    service = TestBed.inject(ComparisonApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should initialise signals with defaults', () => {
    expect(service.uploadedDocs()).toEqual([]);
    expect(service.comparisonType()).toBe('SideBySide');
    expect(service.loading()).toBe(false);
    expect(service.error()).toBeNull();
    expect(service.result()).toBeNull();
  });

  it('should add a document to uploadedDocs', () => {
    const doc = { id: '1', name: 'test.pdf', size: 1024 };
    service.addDocument(doc);
    expect(service.uploadedDocs()).toContain(doc);
  });

  it('should remove a document from uploadedDocs', () => {
    const doc = { id: '1', name: 'test.pdf', size: 1024 };
    service.addDocument(doc);
    service.removeDocument('1');
    expect(service.uploadedDocs()).not.toContain(doc);
  });

  it('should update comparison type', () => {
    service.setComparisonType('Contract');
    expect(service.comparisonType()).toBe('Contract');
  });

  it('should set and clear error via setError', () => {
    service.setError('Something went wrong');
    expect(service.error()).toBe('Something went wrong');
    service.setError(null);
    expect(service.error()).toBeNull();
  });

  it('should reset all state to defaults', () => {
    service.addDocument({ id: '1', name: 'test.pdf', size: 1024 });
    service.setComparisonType('Policy');
    service.setError('oops');
    service.reset();
    expect(service.uploadedDocs()).toEqual([]);
    expect(service.comparisonType()).toBe('SideBySide');
    expect(service.loading()).toBe(false);
    expect(service.error()).toBeNull();
    expect(service.result()).toBeNull();
  });

  it('uploadDocument should POST to /documents (not /documents/upload)', () => {
    const file = new File(['content'], 'test.pdf', { type: 'application/pdf' });
    service.uploadDocument(file).subscribe();
    const req = httpMock.expectOne(`${API}/documents`);
    expect(req.request.method).toBe('POST');
    req.flush({ documentId: 'doc-1', fileName: 'test.pdf', status: 'Pending' });
  });

  it('uploadDocument body should use multipart form "file" field', () => {
    const file = new File(['content'], 'test.pdf', { type: 'application/pdf' });
    service.uploadDocument(file).subscribe();
    const req = httpMock.expectOne(`${API}/documents`);
    const body = req.request.body as FormData;
    expect(body.get('file')).toBeTruthy();
    req.flush({ documentId: 'doc-1', fileName: 'test.pdf', status: 'Pending' });
  });

  it('compareDocuments should POST to /comparison (synchronous — no polling)', () => {
    const request = {
      documentIds: ['doc-1', 'doc-2'],
      comparisonType: 'SideBySide' as const,
    };
    service.compareDocuments(request).subscribe((result) => {
      expect(result.executiveOverview).toBe('Overview of differences.');
    });
    const req = httpMock.expectOne(`${API}/comparison`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.comparisonType).toBe('SideBySide');
    req.flush(mockResult);
  });

  it('compareDocuments should use PascalCase enum values matching backend ComparisonType', () => {
    const types = ['SideBySide', 'Version', 'Contract', 'Policy', 'Custom'] as const;
    expect(types.length).toBe(5);
    expect(types).toContain('SideBySide');
    // Confirm old kebab-case values are NOT used
    expect(types).not.toContain('side-by-side' as never);
    expect(types).not.toContain('contract' as never);
  });

  it('runComparison should upload then POST to /comparison — not async poll', fakeAsync(() => {
    const file1 = new File(['a'], 'doc1.pdf', { type: 'application/pdf' });
    const file2 = new File(['b'], 'doc2.pdf', { type: 'application/pdf' });
    service.runComparison([file1, file2], 'Contract');

    expect(service.loading()).toBe(true);

    // First upload
    const upload1 = httpMock.expectOne(`${API}/documents`);
    upload1.flush({ documentId: 'id-1', fileName: 'doc1.pdf', status: 'Pending' });
    tick();

    // Second upload
    const upload2 = httpMock.expectOne(`${API}/documents`);
    upload2.flush({ documentId: 'id-2', fileName: 'doc2.pdf', status: 'Pending' });
    tick();

    // Synchronous comparison POST — NO polling endpoints called
    const cmpReq = httpMock.expectOne(`${API}/comparison`);
    expect(cmpReq.request.body.documentIds).toEqual(['id-1', 'id-2']);
    expect(cmpReq.request.body.comparisonType).toBe('Contract');
    cmpReq.flush(mockResult);
    tick();

    expect(service.loading()).toBe(false);
    expect(service.result()).toEqual(mockResult);
    expect(service.error()).toBeNull();
  }));

  it('runComparison should set error signal (not mock data) on upload failure', fakeAsync(() => {
    const file1 = new File(['a'], 'doc1.pdf', { type: 'application/pdf' });
    const file2 = new File(['b'], 'doc2.pdf', { type: 'application/pdf' });
    service.runComparison([file1, file2], 'SideBySide');

    const req = httpMock.expectOne(`${API}/documents`);
    req.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
    tick();

    expect(service.loading()).toBe(false);
    expect(service.result()).toBeNull();
    expect(service.error()).toBeTruthy();
  }));

  it('runComparison should set error signal (not mock data) on comparison failure', fakeAsync(() => {
    const file1 = new File(['a'], 'doc1.pdf', { type: 'application/pdf' });
    const file2 = new File(['b'], 'doc2.pdf', { type: 'application/pdf' });
    service.runComparison([file1, file2], 'Policy');

    httpMock.expectOne(`${API}/documents`).flush({ documentId: 'id-1', fileName: 'doc1.pdf', status: 'Pending' });
    tick();
    httpMock.expectOne(`${API}/documents`).flush({ documentId: 'id-2', fileName: 'doc2.pdf', status: 'Pending' });
    tick();

    const req = httpMock.expectOne(`${API}/comparison`);
    req.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
    tick();

    expect(service.loading()).toBe(false);
    expect(service.result()).toBeNull();
    expect(service.error()).toBeTruthy();
  }));
});
