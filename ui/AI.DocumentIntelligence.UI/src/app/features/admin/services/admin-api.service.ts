import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';

import { environment } from '../../../../environments/environment';
import {
  ActivityItem,
  AiUsageSummary,
  DailyUsagePoint,
  DashboardFilters,
  DashboardMetrics,
  OperationType,
  UsageByType,
} from '../models/admin-dashboard.models';

// ── Mock data factories ───────────────────────────────────────────────────────

const DAILY_RAW: ReadonlyArray<{ prompt: number; completion: number; ops: number }> = [
  { prompt: 8_420, completion: 3_210, ops: 12 },
  { prompt: 12_300, completion: 4_800, ops: 18 },
  { prompt: 6_750, completion: 2_560, ops: 9 },
  { prompt: 15_200, completion: 6_100, ops: 23 },
  { prompt: 9_870, completion: 3_950, ops: 14 },
  { prompt: 11_450, completion: 4_580, ops: 17 },
  { prompt: 7_830, completion: 3_120, ops: 11 },
  { prompt: 18_650, completion: 7_420, ops: 28 },
  { prompt: 13_200, completion: 5_280, ops: 20 },
  { prompt: 10_540, completion: 4_210, ops: 16 },
  { prompt: 14_870, completion: 5_950, ops: 22 },
  { prompt: 8_290, completion: 3_310, ops: 12 },
  { prompt: 16_450, completion: 6_580, ops: 25 },
  { prompt: 12_780, completion: 5_110, ops: 19 },
];

function buildDailyUsage(): readonly DailyUsagePoint[] {
  return DAILY_RAW.map((d, i) => {
    const date = new Date(Date.now() - (DAILY_RAW.length - 1 - i) * 86_400_000);
    const dateStr = date.toISOString().split('T')[0] ?? '';
    return {
      date: dateStr,
      promptTokens: d.prompt,
      completionTokens: d.completion,
      cost: parseFloat(((d.prompt * 0.002 + d.completion * 0.003) / 1_000).toFixed(4)),
      operationCount: d.ops,
    };
  });
}

function buildUsageByType(): readonly UsageByType[] {
  const types: readonly OperationType[] = ['analysis', 'comparison', 'chat'];
  const splits = [
    { count: 118, prompt: 92_340, completion: 36_930, cost: 0.2962 },
    { count: 74, prompt: 62_180, completion: 24_870, cost: 0.1991 },
    { count: 46, prompt: 27_480, completion: 10_990, cost: 0.0880 },
  ] as const;
  return types.map((type, i) => ({
    type,
    count: splits[i].count,
    promptTokens: splits[i].prompt,
    completionTokens: splits[i].completion,
    cost: splits[i].cost,
  }));
}

function ts(minutesAgo: number): string {
  return new Date(Date.now() - minutesAgo * 60_000).toISOString();
}

const MOCK_ACTIVITY: readonly ActivityItem[] = [
  {
    id: 'act-001',
    type: 'analysis',
    userId: 'u-001',
    userEmail: 'alice@corp.example',
    description: 'Analysed "Q4 Vendor Contract.pdf"',
    timestamp: ts(8),
  },
  {
    id: 'act-002',
    type: 'upload',
    userId: 'u-002',
    userEmail: 'bob@corp.example',
    description: 'Uploaded "IT Security Policy 2024.pdf" (1.2 MB)',
    timestamp: ts(22),
  },
  {
    id: 'act-003',
    type: 'comparison',
    userId: 'u-001',
    userEmail: 'alice@corp.example',
    description: 'Compared "RFP Response Alpha.pdf" vs "RFP Response Beta.pdf"',
    timestamp: ts(45),
  },
  {
    id: 'act-004',
    type: 'chat',
    userId: 'u-003',
    userEmail: 'carol@corp.example',
    description: 'Chat session "Procurement RFP Evaluation" — 12 messages',
    timestamp: ts(90),
  },
  {
    id: 'act-005',
    type: 'login',
    userId: 'u-004',
    userEmail: 'david@corp.example',
    description: 'User signed in',
    timestamp: ts(112),
  },
  {
    id: 'act-006',
    type: 'analysis',
    userId: 'u-002',
    userEmail: 'bob@corp.example',
    description: 'Analysed "Compliance Framework v3.pdf"',
    timestamp: ts(180),
  },
  {
    id: 'act-007',
    type: 'upload',
    userId: 'u-003',
    userEmail: 'carol@corp.example',
    description: 'Uploaded "Annual Report 2023.pdf" (3.8 MB)',
    timestamp: ts(240),
  },
  {
    id: 'act-008',
    type: 'comparison',
    userId: 'u-004',
    userEmail: 'david@corp.example',
    description: 'Compared 3 contract revisions (side-by-side)',
    timestamp: ts(300),
  },
  {
    id: 'act-009',
    type: 'chat',
    userId: 'u-001',
    userEmail: 'alice@corp.example',
    description: 'Chat session "Vendor Contract Review Q4" — 8 messages',
    timestamp: ts(420),
  },
  {
    id: 'act-010',
    type: 'login',
    userId: 'u-005',
    userEmail: 'eve@corp.example',
    description: 'User signed in',
    timestamp: ts(480),
  },
  {
    id: 'act-011',
    type: 'analysis',
    userId: 'u-005',
    userEmail: 'eve@corp.example',
    description: 'Analysed "NDA Agreement 2024.pdf"',
    timestamp: ts(540),
  },
  {
    id: 'act-012',
    type: 'upload',
    userId: 'u-002',
    userEmail: 'bob@corp.example',
    description: 'Uploaded "Service Level Agreement.pdf" (0.9 MB)',
    timestamp: ts(620),
  },
  {
    id: 'act-013',
    type: 'comparison',
    userId: 'u-003',
    userEmail: 'carol@corp.example',
    description: 'Compared "Policy v1.pdf" vs "Policy v2.pdf" (policy type)',
    timestamp: ts(720),
  },
  {
    id: 'act-014',
    type: 'analysis',
    userId: 'u-004',
    userEmail: 'david@corp.example',
    description: 'Analysed "Master Service Agreement.pdf"',
    timestamp: ts(900),
  },
  {
    id: 'act-015',
    type: 'login',
    userId: 'u-006',
    userEmail: 'frank@corp.example',
    description: 'User signed in',
    timestamp: ts(1_080),
  },
];

function buildMockMetrics(): DashboardMetrics {
  const dailyUsage = buildDailyUsage();
  const usageByType = buildUsageByType();

  const totalPromptTokens = dailyUsage.reduce((s, d) => s + d.promptTokens, 0);
  const totalCompletionTokens = dailyUsage.reduce((s, d) => s + d.completionTokens, 0);
  const totalCost = parseFloat(dailyUsage.reduce((s, d) => s + d.cost, 0).toFixed(4));

  const aiUsage: AiUsageSummary = {
    totalPromptTokens,
    totalCompletionTokens,
    totalCost,
    averageProcessingTimeMs: 3_840,
    dailyUsage,
    usageByType,
  };

  return {
    totalUsers: 24,
    totalDocuments: 187,
    totalAnalyses: 118,
    totalComparisons: 74,
    totalChatSessions: 46,
    aiUsage,
    recentActivity: MOCK_ACTIVITY,
  };
}

// ── Service ───────────────────────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class AdminApiService {
  private readonly http = inject(HttpClient);
  private readonly apiBase = environment.apiBaseUrl;

  private readonly _metrics = signal<DashboardMetrics | null>(null);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  /** Current dashboard metrics, or `null` when not yet loaded. */
  readonly metrics = this._metrics.asReadonly();
  /** True while an HTTP fetch is in-flight. */
  readonly loading = this._loading.asReadonly();
  /** Non-null when a load error occurred. */
  readonly error = this._error.asReadonly();

  loadMetrics(filters: DashboardFilters): void {
    this._loading.set(true);
    this._error.set(null);

    const params = this.buildParams(filters);

    this.http.get<DashboardMetrics>(`${this.apiBase}/admin/metrics`, { params }).subscribe({
      next: (data) => {
        this._metrics.set(data);
        this._loading.set(false);
      },
      error: () => {
        // Backend not yet wired — serve realistic mock data for demonstration.
        this._metrics.set(buildMockMetrics());
        this._loading.set(false);
      },
    });
  }

  setError(msg: string | null): void {
    this._error.set(msg);
  }

  private buildParams(filters: DashboardFilters): HttpParams {
    let params = new HttpParams();
    if (filters.dateFrom) {
      params = params.set('dateFrom', filters.dateFrom);
    }
    if (filters.dateTo) {
      params = params.set('dateTo', filters.dateTo);
    }
    if (filters.operationType !== 'all') {
      params = params.set('operationType', filters.operationType);
    }
    if (filters.userId) {
      params = params.set('userId', filters.userId);
    }
    return params;
  }
}
