/**
 * Domain models for the Admin Dashboard feature (T13).
 * All interfaces are readonly — mutated only inside AdminApiService signals.
 */

export type OperationType = 'analysis' | 'comparison' | 'chat';
export type ActivityEventType = 'analysis' | 'comparison' | 'chat' | 'login' | 'upload';
export type OperationFilter = 'all' | OperationType;

// ── Chart data shapes ─────────────────────────────────────────────────────────

export interface BarDataPoint {
  readonly label: string;
  readonly value: number;
}

export interface DonutSegment {
  readonly label: string;
  readonly value: number;
  readonly color: string;
}

// ── Metric card ───────────────────────────────────────────────────────────────

export interface MetricCardConfig {
  readonly label: string;
  readonly value: string | number;
  readonly icon: string;
  readonly description: string;
}

// ── AI usage ──────────────────────────────────────────────────────────────────

export interface DailyUsagePoint {
  readonly date: string; // YYYY-MM-DD
  readonly promptTokens: number;
  readonly completionTokens: number;
  readonly cost: number;
  readonly operationCount: number;
}

export interface UsageByType {
  readonly type: OperationType;
  readonly count: number;
  readonly promptTokens: number;
  readonly completionTokens: number;
  readonly cost: number;
}

export interface AiUsageSummary {
  readonly totalPromptTokens: number;
  readonly totalCompletionTokens: number;
  readonly totalCost: number;
  readonly averageProcessingTimeMs: number;
  readonly dailyUsage: readonly DailyUsagePoint[];
  readonly usageByType: readonly UsageByType[];
}

// ── Activity feed ─────────────────────────────────────────────────────────────

export interface ActivityItem {
  readonly id: string;
  readonly type: ActivityEventType;
  readonly userId: string;
  readonly userEmail: string;
  readonly description: string;
  readonly timestamp: string; // ISO 8601
}

// ── Dashboard aggregate ───────────────────────────────────────────────────────

export interface DashboardMetrics {
  readonly totalUsers: number;
  readonly totalDocuments: number;
  readonly totalAnalyses: number;
  readonly totalComparisons: number;
  readonly totalChatSessions: number;
  readonly aiUsage: AiUsageSummary;
  readonly recentActivity: readonly ActivityItem[];
}

// ── Filters ───────────────────────────────────────────────────────────────────

export interface DashboardFilters {
  readonly dateFrom: string | null; // YYYY-MM-DD or null
  readonly dateTo: string | null;
  readonly operationType: OperationFilter;
  readonly userId: string;
}

export const DEFAULT_FILTERS: DashboardFilters = {
  dateFrom: null,
  dateTo: null,
  operationType: 'all',
  userId: '',
};
