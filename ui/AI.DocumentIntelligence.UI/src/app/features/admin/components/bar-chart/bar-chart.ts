import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { BarDataPoint } from '../../models/admin-dashboard.models';

// ── Internal chart types ──────────────────────────────────────────────────────

interface ComputedBar {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
  readonly label: string;
  readonly formattedValue: string;
  readonly showLabel: boolean;
}

interface YGridLine {
  readonly y: number;
  readonly label: string;
}

export interface ChartLayout {
  readonly bars: readonly ComputedBar[];
  readonly yLines: readonly YGridLine[];
  readonly hasData: boolean;
}

// ── SVG constants ─────────────────────────────────────────────────────────────

const SVG_W = 560;
const SVG_H = 220;
const PAD_LEFT = 55;
const PAD_RIGHT = 16;
const PAD_TOP = 16;
const PAD_BOTTOM = 54;
const CHART_W = SVG_W - PAD_LEFT - PAD_RIGHT;
const CHART_H = SVG_H - PAD_TOP - PAD_BOTTOM;
const Y_LINE_COUNT = 5;

// ── Formatting helpers ────────────────────────────────────────────────────────

function formatAxisValue(n: number, isCost: boolean): string {
  if (isCost) {
    return `$${n.toFixed(n < 1 ? 3 : 2)}`;
  }
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
  if (n >= 1_000) return `${(n / 1_000).toFixed(1)}k`;
  return n.toFixed(0);
}

// ── Component ─────────────────────────────────────────────────────────────────

/**
 * Presentational SVG bar-chart. Accepts a data array and renders a
 * dark-theme bar chart with Y-axis gridlines and X-axis date labels.
 * Every other label is shown to avoid crowding for 14-day windows.
 */
@Component({
  selector: 'app-bar-chart',
  standalone: true,
  imports: [],
  templateUrl: './bar-chart.html',
  styleUrl: './bar-chart.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BarChartComponent {
  readonly data = input<readonly BarDataPoint[]>([]);
  readonly title = input('');
  readonly isCost = input(false); // affects Y-axis formatting

  protected readonly svgW = SVG_W;
  protected readonly svgH = SVG_H;
  protected readonly padLeft = PAD_LEFT;
  protected readonly padTop = PAD_TOP;
  protected readonly padBottom = PAD_BOTTOM;
  protected readonly chartH = CHART_H;

  /** Computes bar rects and Y gridlines from `data` input. */
  protected readonly layout = computed<ChartLayout>(() => {
    const items = this.data();
    if (items.length === 0) {
      return { bars: [], yLines: [], hasData: false };
    }

    const maxVal = Math.max(...items.map((d) => d.value));
    const roundedMax = this.niceMax(maxVal);
    const n = items.length;
    const slotW = CHART_W / n;
    const barW = slotW * 0.6;
    const gapW = slotW * 0.4;
    const cost = this.isCost();

    const bars: ComputedBar[] = items.map((d, i) => {
      const barH = roundedMax > 0 ? (d.value / roundedMax) * CHART_H : 0;
      return {
        x: PAD_LEFT + i * slotW + gapW / 2,
        y: PAD_TOP + CHART_H - barH,
        width: barW,
        height: Math.max(barH, 1),
        label: d.label,
        formattedValue: formatAxisValue(d.value, cost),
        showLabel: i % 2 === 0,
      };
    });

    const yLines: YGridLine[] = Array.from({ length: Y_LINE_COUNT }, (_, k) => {
      const fraction = k / (Y_LINE_COUNT - 1);
      return {
        y: PAD_TOP + CHART_H * (1 - fraction),
        label: formatAxisValue(roundedMax * fraction, cost),
      };
    });

    return { bars, yLines, hasData: true };
  });

  /** Rounds up to a nice axis maximum. */
  private niceMax(max: number): number {
    if (max === 0) return 1;
    const magnitude = Math.pow(10, Math.floor(Math.log10(max)));
    const normalized = max / magnitude;
    const nice = normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 5 ? 5 : 10;
    return nice * magnitude;
  }

  protected xAxisY(): number {
    return PAD_TOP + CHART_H;
  }
}
