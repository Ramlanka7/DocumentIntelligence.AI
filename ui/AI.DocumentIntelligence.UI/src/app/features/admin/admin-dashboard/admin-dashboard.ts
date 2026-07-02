import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';

import { AdminApiService } from '../services/admin-api.service';
import {
  BarDataPoint,
  DashboardFilters,
  DEFAULT_FILTERS,
  DonutSegment,
  MetricCardConfig,
} from '../models/admin-dashboard.models';
import { MetricCardComponent } from '../components/metric-card/metric-card';
import { BarChartComponent } from '../components/bar-chart/bar-chart';
import { DonutChartComponent } from '../components/donut-chart/donut-chart';
import { ActivityFeedComponent } from '../components/activity-feed/activity-feed';
import { DashboardFiltersComponent } from '../components/dashboard-filters/dashboard-filters';

/**
 * Smart (page-level) component for the Admin Dashboard.
 * Guarded by `roleGuard` with `roles: ['Admin']` — non-admins are redirected
 * to `/` by the guard before reaching this component.
 *
 * Reactive: loads metrics via `effect()` whenever the filter signal changes.
 */
@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [
    MatIconModule,
    MatButtonModule,
    MatProgressBarModule,
    MetricCardComponent,
    BarChartComponent,
    DonutChartComponent,
    ActivityFeedComponent,
    DashboardFiltersComponent,
  ],
  templateUrl: './admin-dashboard.html',
  styleUrl: './admin-dashboard.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminDashboardComponent {
  protected readonly service = inject(AdminApiService);

  private readonly _filters = signal<DashboardFilters>(DEFAULT_FILTERS);
  protected readonly filters = this._filters.asReadonly();

  constructor() {
    // Reactively reload whenever filters change
    effect(() => {
      this.service.loadMetrics(this._filters());
    });
  }

  // ── Metric cards ────────────────────────────────────────────────────────────

  protected readonly metricCards = computed<readonly MetricCardConfig[]>(() => {
    const m = this.service.metrics();
    if (!m) return this.loadingCards();
    return [
      {
        label: 'Total Users',
        value: m.totalUsers,
        icon: 'people',
        description: 'Registered platform users',
      },
      {
        label: 'Total Documents',
        value: m.totalDocuments,
        icon: 'description',
        description: 'Uploaded & indexed documents',
      },
      {
        label: 'Total Analyses',
        value: m.totalAnalyses,
        icon: 'analytics',
        description: 'AI analysis operations run',
      },
      {
        label: 'Total Comparisons',
        value: m.totalComparisons,
        icon: 'compare_arrows',
        description: 'Document comparisons performed',
      },
      {
        label: 'Chat Sessions',
        value: m.totalChatSessions,
        icon: 'forum',
        description: 'AI chat sessions created',
      },
      {
        label: 'Prompt Tokens',
        value: this.formatTokens(m.aiUsage.totalPromptTokens),
        icon: 'token',
        description: 'Total tokens sent to AI',
      },
      {
        label: 'AI Cost (USD)',
        value: `$${m.aiUsage.totalCost.toFixed(2)}`,
        icon: 'attach_money',
        description: 'Estimated total spend (14 days)',
      },
      {
        label: 'Avg Processing Time',
        value: `${(m.aiUsage.averageProcessingTimeMs / 1_000).toFixed(1)}s`,
        icon: 'timer',
        description: 'Average AI operation duration',
      },
    ];
  });

  // ── Chart data ──────────────────────────────────────────────────────────────

  protected readonly tokenChartData = computed<readonly BarDataPoint[]>(() => {
    const m = this.service.metrics();
    if (!m) return [];
    return m.aiUsage.dailyUsage.map((d) => ({
      label: d.date.slice(5), // MM-DD
      value: d.promptTokens + d.completionTokens,
    }));
  });

  protected readonly costChartData = computed<readonly BarDataPoint[]>(() => {
    const m = this.service.metrics();
    if (!m) return [];
    return m.aiUsage.dailyUsage.map((d) => ({
      label: d.date.slice(5),
      value: d.cost,
    }));
  });

  protected readonly donutSegments = computed<readonly DonutSegment[]>(() => {
    const m = this.service.metrics();
    if (!m) return [];
    const colors: Record<string, string> = {
      analysis: '#60a5fa',
      comparison: '#34d399',
      chat: '#a78bfa',
    };
    return m.aiUsage.usageByType.map((t) => ({
      label: t.type,
      value: t.count,
      color: colors[t.type] ?? '#94a3b8',
    }));
  });

  // ── Handlers ────────────────────────────────────────────────────────────────

  protected onFiltersChange(f: DashboardFilters): void {
    this._filters.set(f);
  }

  protected dismissError(): void {
    this.service.setError(null);
  }

  protected refresh(): void {
    this.service.loadMetrics(this._filters());
  }

  // ── Private helpers ─────────────────────────────────────────────────────────

  private formatTokens(n: number): string {
    if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
    if (n >= 1_000) return `${(n / 1_000).toFixed(1)}k`;
    return n.toString();
  }

  private loadingCards(): readonly MetricCardConfig[] {
    return [
      { label: 'Total Users', value: '—', icon: 'people', description: '' },
      { label: 'Total Documents', value: '—', icon: 'description', description: '' },
      { label: 'Total Analyses', value: '—', icon: 'analytics', description: '' },
      { label: 'Total Comparisons', value: '—', icon: 'compare_arrows', description: '' },
      { label: 'Chat Sessions', value: '—', icon: 'forum', description: '' },
      { label: 'Prompt Tokens', value: '—', icon: 'token', description: '' },
      { label: 'AI Cost (USD)', value: '—', icon: 'attach_money', description: '' },
      { label: 'Avg Processing Time', value: '—', icon: 'timer', description: '' },
    ];
  }
}
