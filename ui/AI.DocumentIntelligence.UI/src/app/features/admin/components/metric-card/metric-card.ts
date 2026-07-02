import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { MetricCardConfig } from '../../models/admin-dashboard.models';

/**
 * Presentational component that renders a single KPI metric card.
 * Accepts the card config and a `loading` flag; shows a skeleton pulse
 * while data is loading.
 */
@Component({
  selector: 'app-metric-card',
  standalone: true,
  imports: [MatIconModule, MatProgressSpinnerModule],
  templateUrl: './metric-card.html',
  styleUrl: './metric-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MetricCardComponent {
  readonly card = input.required<MetricCardConfig>();
  readonly loading = input(false);
}
