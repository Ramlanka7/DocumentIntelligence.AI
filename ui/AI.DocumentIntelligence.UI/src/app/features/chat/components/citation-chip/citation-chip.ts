import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';

import { ChatCitation } from '../../models/chat.models';

@Component({
  selector: 'app-citation-chip',
  standalone: true,
  imports: [MatIconModule, MatTooltipModule],
  templateUrl: './citation-chip.html',
  styleUrl: './citation-chip.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CitationChipComponent {
  readonly citation = input.required<ChatCitation>();

  protected readonly confidenceLabel = computed(() =>
    `${Math.round(this.citation().confidenceScore * 100)}%`,
  );

  protected readonly confidenceClass = computed(() => {
    const score = this.citation().confidenceScore;
    if (score >= 0.9) return 'confidence--high';
    if (score >= 0.7) return 'confidence--medium';
    return 'confidence--low';
  });

  protected readonly tooltipText = computed(() => {
    const c = this.citation();
    return `${c.documentName} · Page ${c.pageNumber} · ${c.paragraphReference} · Confidence: ${Math.round(c.confidenceScore * 100)}%`;
  });
}
