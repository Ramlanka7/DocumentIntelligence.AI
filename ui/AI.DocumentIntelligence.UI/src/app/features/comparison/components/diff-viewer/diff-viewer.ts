import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

import { DiffEntry, DiffStatus, Citation } from '../../models/comparison.models';

interface DiffStatusConfig {
  label: string;
  marker: string;
  ariaLabel: string;
}

const DIFF_STATUS_CONFIG: Record<DiffStatus, DiffStatusConfig> = {
  added: { label: 'Added', marker: '+', ariaLabel: 'Content added' },
  removed: { label: 'Removed', marker: '-', ariaLabel: 'Content removed' },
  modified: { label: 'Modified', marker: '~', ariaLabel: 'Content modified' },
};

@Component({
  selector: 'app-diff-viewer',
  standalone: true,
  imports: [MatIconModule],
  templateUrl: './diff-viewer.html',
  styleUrl: './diff-viewer.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DiffViewerComponent {
  readonly entries = input<DiffEntry[]>([]);
  readonly showHeader = input(true);

  protected getStatusConfig(status: DiffStatus): DiffStatusConfig {
    return DIFF_STATUS_CONFIG[status];
  }

  protected formatConfidence(score: number): string {
    return `${Math.round(score * 100)}%`;
  }

  protected getChangeTypeLabel(changeType: DiffEntry['changeType']): string {
    const labels: Record<DiffEntry['changeType'], string> = {
      clause: 'Clause',
      pricing: 'Pricing',
      risk: 'Risk',
      compliance: 'Compliance',
      general: 'General',
    };
    return labels[changeType];
  }

  protected trackEntry(_index: number, entry: DiffEntry): string {
    return entry.id;
  }

  protected trackCitation(_index: number, citation: Citation): string {
    return `${citation.documentName}-${citation.pageNumber}-${citation.paragraphRef}`;
  }
}
