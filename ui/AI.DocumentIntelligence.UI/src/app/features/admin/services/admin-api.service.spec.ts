import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { AdminApiService } from './admin-api.service';
import { DEFAULT_FILTERS } from '../models/admin-dashboard.models';
import { environment } from '../../../../environments/environment';

describe('AdminApiService', () => {
  let service: AdminApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AdminApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('initial state: metrics null, loading false, error null', () => {
    expect(service.metrics()).toBeNull();
    expect(service.loading()).toBeFalse();
    expect(service.error()).toBeNull();
  });

  it('loadMetrics sets loading=true, then populates metrics on success', () => {
    service.loadMetrics(DEFAULT_FILTERS);

    expect(service.loading()).toBeTrue();

    const req = httpMock.expectOne((r) => r.url.includes('/admin/metrics'));
    expect(req.request.method).toBe('GET');

    const mockData = {
      totalUsers: 10,
      totalDocuments: 50,
      totalAnalyses: 20,
      totalComparisons: 10,
      totalChatSessions: 5,
      aiUsage: {
        totalPromptTokens: 1000,
        totalCompletionTokens: 500,
        totalCost: 0.005,
        averageProcessingTimeMs: 2000,
        dailyUsage: [],
        usageByType: [],
      },
      recentActivity: [],
    };

    req.flush(mockData);

    expect(service.loading()).toBeFalse();
    expect(service.metrics()).toEqual(mockData);
    expect(service.error()).toBeNull();
  });

  it('loadMetrics falls back to mock data on HTTP error', () => {
    service.loadMetrics(DEFAULT_FILTERS);

    const req = httpMock.expectOne((r) => r.url.includes('/admin/metrics'));
    req.flush('Server Error', { status: 500, statusText: 'Internal Server Error' });

    expect(service.loading()).toBeFalse();
    expect(service.metrics()).not.toBeNull();
    // Mock data should have realistic values
    const metrics = service.metrics();
    expect(metrics?.totalUsers).toBeGreaterThan(0);
    expect(metrics?.aiUsage.dailyUsage.length).toBe(14);
  });

  it('setError updates the error signal', () => {
    service.setError('Something went wrong');
    expect(service.error()).toBe('Something went wrong');
    service.setError(null);
    expect(service.error()).toBeNull();
  });

  it('loadMetrics appends operationType query param when not "all"', () => {
    service.loadMetrics({ ...DEFAULT_FILTERS, operationType: 'analysis' });

    const req = httpMock.expectOne((r) =>
      r.url.includes('/admin/metrics') && r.params.get('operationType') === 'analysis',
    );
    expect(req.request.method).toBe('GET');
    req.flush({
      totalUsers: 1,
      totalDocuments: 1,
      totalAnalyses: 1,
      totalComparisons: 0,
      totalChatSessions: 0,
      aiUsage: {
        totalPromptTokens: 0,
        totalCompletionTokens: 0,
        totalCost: 0,
        averageProcessingTimeMs: 0,
        dailyUsage: [],
        usageByType: [],
      },
      recentActivity: [],
    });

    expect(service.loading()).toBeFalse();
  });

  it('buildParams omits "all" operationType from query string', () => {
    service.loadMetrics({ ...DEFAULT_FILTERS, operationType: 'all' });

    const req = httpMock.expectOne(
      (r) => r.url.includes('/admin/metrics') && !r.params.has('operationType'),
    );
    expect(req.request.method).toBe('GET');
    req.flush({
      totalUsers: 1,
      totalDocuments: 1,
      totalAnalyses: 1,
      totalComparisons: 0,
      totalChatSessions: 0,
      aiUsage: {
        totalPromptTokens: 0,
        totalCompletionTokens: 0,
        totalCost: 0,
        averageProcessingTimeMs: 0,
        dailyUsage: [],
        usageByType: [],
      },
      recentActivity: [],
    });
  });

  it('loadMetrics appends dateFrom and dateTo when provided', () => {
    service.loadMetrics({ ...DEFAULT_FILTERS, dateFrom: '2026-06-01', dateTo: '2026-06-30' });

    const req = httpMock.expectOne(
      (r) =>
        r.url.includes('/admin/metrics') &&
        r.params.get('dateFrom') === '2026-06-01' &&
        r.params.get('dateTo') === '2026-06-30',
    );
    expect(req.request.method).toBe('GET');
    req.flush({
      totalUsers: 1,
      totalDocuments: 1,
      totalAnalyses: 1,
      totalComparisons: 0,
      totalChatSessions: 0,
      aiUsage: {
        totalPromptTokens: 0,
        totalCompletionTokens: 0,
        totalCost: 0,
        averageProcessingTimeMs: 0,
        dailyUsage: [],
        usageByType: [],
      },
      recentActivity: [],
    });
  });
});
