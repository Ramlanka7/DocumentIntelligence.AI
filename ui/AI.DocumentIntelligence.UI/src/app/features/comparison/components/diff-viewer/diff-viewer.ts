import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

import { Citation, DocumentDifference, DifferenceType } from '../../models/comparison.models';

interface DiffStatusConfig {
  label: string;
  marker: string;
  ariaLabel: string;
}

const DIFF_TYPE_CONFIG: Record<DifferenceType, DiffStatusConfig> = {
  Added: { label: 'Added', marker: '+', ariaLabel: 'Content added' },
  Removed: { label: 'Removed', marker: '-', ariaLabel: 'Content removed' },
  Modified: { label: 'Modified', marker: '~', ariaLabel: 'Content modified' },
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
  readonly entries = input<DocumentDifference[]>([]);
  readonly showHeader = input(true);

  protected getStatusConfig(type: DifferenceType): DiffStatusConfig {
    return DIFF_TYPE_CONFIG[type];
  }

  protected formatConfidence(score: number): string {
    return `${Math.round(score * 100)}%`;
  }

  protected trackEntry(index: number, entry: DocumentDifference): string {
    return `${entry.type}-${entry.section}-${index}`;
  }

  protected trackCitation(_index: number, citation: Citation): string {
    return `${citation.documentName}-${citation.pageNumber}-${citation.paragraphReference}`;
  }
}
