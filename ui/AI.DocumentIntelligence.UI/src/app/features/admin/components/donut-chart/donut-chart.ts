import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { DonutSegment } from '../../models/admin-dashboard.models';

// ── SVG arc helpers ───────────────────────────────────────────────────────────

interface Point {
  readonly x: number;
  readonly y: number;
}

function polarToCartesian(cx: number, cy: number, r: number, angleDeg: number): Point {
  const rad = ((angleDeg - 90) * Math.PI) / 180;
  return {
    x: cx + r * Math.cos(rad),
    y: cy + r * Math.sin(rad),
  };
}

function describeArc(
  cx: number,
  cy: number,
  outerR: number,
  innerR: number,
  startDeg: number,
  endDeg: number,
): string {
  // Cap to avoid degenerate full-circle paths
  const clamped = Math.min(endDeg, startDeg + 359.99);
  const os = polarToCartesian(cx, cy, outerR, clamped);
  const oe = polarToCartesian(cx, cy, outerR, startDeg);
  const is_ = polarToCartesian(cx, cy, innerR, clamped);
  const ie = polarToCartesian(cx, cy, innerR, startDeg);
  const large = clamped - startDeg > 180 ? 1 : 0;

  return [
    `M ${os.x.toFixed(2)} ${os.y.toFixed(2)}`,
    `A ${outerR} ${outerR} 0 ${large} 0 ${oe.x.toFixed(2)} ${oe.y.toFixed(2)}`,
    `L ${ie.x.toFixed(2)} ${ie.y.toFixed(2)}`,
    `A ${innerR} ${innerR} 0 ${large} 1 ${is_.x.toFixed(2)} ${is_.y.toFixed(2)}`,
    'Z',
  ].join(' ');
}

// ── Internal types ────────────────────────────────────────────────────────────

export interface ComputedDonutSegment {
  readonly path: string;
  readonly color: string;
  readonly label: string;
  readonly percentage: number;
  readonly value: number;
}

// ── Component ─────────────────────────────────────────────────────────────────

const CX = 90;
const CY = 90;
const OUTER_R = 80;
const INNER_R = 50;

/**
 * Presentational SVG donut chart showing proportional breakdown by segment.
 * Renders a legend to the right of the ring.
 */
@Component({
  selector: 'app-donut-chart',
  standalone: true,
  imports: [],
  templateUrl: './donut-chart.html',
  styleUrl: './donut-chart.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DonutChartComponent {
  readonly segments = input<readonly DonutSegment[]>([]);
  readonly title = input('');

  protected readonly computed = computed<readonly ComputedDonutSegment[]>(() => {
    const segs = this.segments();
    const total = segs.reduce((s, seg) => s + seg.value, 0);
    if (total === 0 || segs.length === 0) return [];

    let angle = -90;
    return segs.map((seg) => {
      const sweep = (seg.value / total) * 360;
      const path = describeArc(CX, CY, OUTER_R, INNER_R, angle, angle + sweep);
      angle += sweep;
      return {
        path,
        color: seg.color,
        label: seg.label,
        percentage: Math.round((seg.value / total) * 100),
        value: seg.value,
      };
    });
  });

  protected readonly hasData = computed(() => this.computed().length > 0);
}
