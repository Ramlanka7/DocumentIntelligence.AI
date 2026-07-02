import { TestBed, ComponentFixture } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { signal, WritableSignal } from '@angular/core';

import { AdminDashboardComponent } from './admin-dashboard';
import { AdminApiService } from '../services/admin-api.service';
import { DashboardMetrics } from '../models/admin-dashboard.models';

const MOCK_METRICS: DashboardMetrics = {
  totalUsers: 24,
  totalDocuments: 187,
  totalAnalyses: 118,
  totalComparisons: 74,
  totalChatSessions: 46,
  aiUsage: {
    totalPromptTokens: 182_000,
    totalCompletionTokens: 72_900,
    totalCost: 0.5833,
    averageProcessingTimeMs: 3_840,
    dailyUsage: Array.from({ length: 14 }, (_, i) => ({
      date: `2026-06-${(i + 1).toString().padStart(2, '0')}`,
      promptTokens: 10_000,
      completionTokens: 4_000,
      cost: 0.032,
      operationCount: 10,
    })),
    usageByType: [
      {
        type: 'analysis',
        count: 118,
        promptTokens: 92_340,
        completionTokens: 36_930,
        cost: 0.2962,
      },
      {
        type: 'comparison',
        count: 74,
        promptTokens: 62_180,
        completionTokens: 24_870,
        cost: 0.1991,
      },
      { type: 'chat', count: 46, promptTokens: 27_480, completionTokens: 10_990, cost: 0.0880 },
    ],
  },
  recentActivity: [
    {
      id: 'act-001',
      type: 'analysis',
      userId: 'u-001',
      userEmail: 'alice@corp.example',
      description: 'Analysed "Q4 Vendor Contract.pdf"',
      timestamp: new Date(Date.now() - 900_000).toISOString(),
    },
  ],
};

describe('AdminDashboardComponent', () => {
  let fixture: ComponentFixture<AdminDashboardComponent>;
  let component: AdminDashboardComponent;

  // Fresh signal instances per test — avoids signal state bleeding between tests
  let metricsSignal: WritableSignal<DashboardMetrics | null>;
  let loadingSignal: WritableSignal<boolean>;
  let errorSignal: WritableSignal<string | null>;
  let loadMetricsSpy: jasmine.Spy;
  let setErrorSpy: jasmine.Spy;

  beforeEach(async () => {
    metricsSignal = signal<DashboardMetrics | null>(null);
    loadingSignal = signal(false);
    errorSignal = signal<string | null>(null);
    loadMetricsSpy = jasmine.createSpy('loadMetrics');
    setErrorSpy = jasmine.createSpy('setError');

    const mockApiService = {
      metrics: metricsSignal,
      loading: loadingSignal,
      error: errorSignal,
      loadMetrics: loadMetricsSpy,
      setError: setErrorSpy,
    };

    await TestBed.configureTestingModule({
      imports: [AdminDashboardComponent],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AdminApiService, useValue: mockApiService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminDashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should call loadMetrics on init via effect', () => {
    expect(loadMetricsSpy).toHaveBeenCalled();
  });

  it('should show progress bar when loading', () => {
    loadingSignal.set(true);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('mat-progress-bar')).not.toBeNull();
  });

  it('should hide progress bar when not loading', () => {
    loadingSignal.set(false);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('mat-progress-bar')).toBeNull();
  });

  it('should display error banner when error is set', () => {
    errorSignal.set('Failed to load dashboard data');
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const errorEl = compiled.querySelector('.admin-error');
    expect(errorEl).not.toBeNull();
    expect(errorEl?.textContent).toContain('Failed to load dashboard data');
  });

  it('should not display error banner when error is null', () => {
    errorSignal.set(null);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.admin-error')).toBeNull();
  });

  it('should render 8 metric cards when metrics are loaded', () => {
    metricsSignal.set(MOCK_METRICS);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const cards = compiled.querySelectorAll('app-metric-card');
    expect(cards.length).toBe(8);
  });

  it('should render chart components when not loading', () => {
    // Ensure loading is false — charts are shown in @else branch
    loadingSignal.set(false);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelectorAll('app-bar-chart').length).toBeGreaterThanOrEqual(2);
    expect(compiled.querySelectorAll('app-donut-chart').length).toBeGreaterThanOrEqual(1);
  });

  it('should render activity feed component', () => {
    metricsSignal.set(MOCK_METRICS);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('app-activity-feed')).not.toBeNull();
  });

  it('should render filter component', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('app-dashboard-filters')).not.toBeNull();
  });
});
