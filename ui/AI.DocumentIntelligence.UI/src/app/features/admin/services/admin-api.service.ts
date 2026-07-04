import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';

import { environment } from '../../../../environments/environment';
import {
  DashboardFilters,
  DashboardMetrics,
} from '../models/admin-dashboard.models';

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

  /**
   * Fetch admin dashboard metrics.
   * GET /api/v1/admin/metrics?dateFrom=&dateTo=&operationType=&userId=
   */
  loadMetrics(filters: DashboardFilters): void {
    this._loading.set(true);
    this._error.set(null);

    const params = this.buildParams(filters);

    this.http.get<DashboardMetrics>(`${this.apiBase}/admin/metrics`, { params }).subscribe({
      next: (data) => {
        this._metrics.set(data);
        this._loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this._loading.set(false);
        if (err.status === 403) {
          this._error.set('You do not have permission to view admin metrics.');
        } else if (err.status === 400) {
          this._error.set('Invalid filter parameters. Please check your inputs.');
        } else {
          this._error.set('Failed to load admin metrics. Please try again.');
        }
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
