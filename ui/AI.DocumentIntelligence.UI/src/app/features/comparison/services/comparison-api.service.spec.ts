import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';

import { environment } from '../../../../environments/environment';
import { ComparisonApiService } from './comparison-api.service';

const API = environment.apiBaseUrl;

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
    expect(service.comparisonType()).toBe('side-by-side');
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
    service.setComparisonType('contract');
    expect(service.comparisonType()).toBe('contract');
  });

  it('should set and clear error via setError', () => {
    service.setError('Something went wrong');
    expect(service.error()).toBe('Something went wrong');
    service.setError(null);
    expect(service.error()).toBeNull();
  });

  it('should reset all state', () => {
    service.addDocument({ id: '1', name: 'test.pdf', size: 1024 });
    service.setComparisonType('policy');
    service.setError('oops');
    service.reset();
    expect(service.uploadedDocs()).toEqual([]);
    expect(service.comparisonType()).toBe('side-by-side');
    expect(service.loading()).toBe(false);
    expect(service.error()).toBeNull();
    expect(service.result()).toBeNull();
  });

  it('should POST to /documents/upload', () => {
    const file = new File(['content'], 'test.pdf', { type: 'application/pdf' });
    service.uploadDocument(file).subscribe();
    const req = httpMock.expectOne(`${API}/documents/upload`);
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'doc-1', name: 'test.pdf', size: 7, contentType: 'application/pdf', uploadedAt: '' });
  });

  it('should POST to /comparisons', () => {
    const request = { documentIds: ['doc-1', 'doc-2'], comparisonType: 'side-by-side' as const };
    service.createComparison(request).subscribe();
    const req = httpMock.expectOne(`${API}/comparisons`);
    expect(req.request.method).toBe('POST');
    req.flush({ id: 'job-1', status: 'pending' });
  });

  it('should GET /comparisons/{id}', () => {
    service.getComparison('job-1').subscribe();
    const req = httpMock.expectOne(`${API}/comparisons/job-1`);
    expect(req.request.method).toBe('GET');
    req.flush({ id: 'job-1', status: 'completed' });
  });
});
