import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
  signal,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';

import { ComparisonResult, Citation } from '../../models/comparison.models';
import { DiffViewerComponent } from '../diff-viewer/diff-viewer';

type ActiveTab = 'overview' | 'differences' | 'risk' | 'recommendations' | 'changelog' | 'citations';

interface Tab {
  id: ActiveTab;
  label: string;
  icon: string;
}

@Component({
  selector: 'app-results-panel',
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatDividerModule,
    DiffViewerComponent,
  ],
  templateUrl: './results-panel.html',
  styleUrl: './results-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResultsPanelComponent {
  readonly result = input.required<ComparisonResult>();
  readonly newComparison = output<void>();

  protected readonly activeTab = signal<ActiveTab>('overview');

  protected readonly tabs: Tab[] = [
    { id: 'overview', label: 'Executive Overview', icon: 'summarize' },
    { id: 'differences', label: 'Key Differences', icon: 'compare_arrows' },
    { id: 'risk', label: 'Risk Analysis', icon: 'warning' },
    { id: 'recommendations', label: 'Recommendations', icon: 'lightbulb' },
    { id: 'changelog', label: 'Change Log', icon: 'history' },
    { id: 'citations', label: 'Source Citations', icon: 'source' },
  ];

  protected setTab(tab: ActiveTab): void {
    this.activeTab.set(tab);
  }

  protected onTabKeydown(event: KeyboardEvent, currentIndex: number): void {
    const total = this.tabs.length;
    if (event.key === 'ArrowRight') {
      event.preventDefault();
      this.setTab(this.tabs[(currentIndex + 1) % total].id);
    } else if (event.key === 'ArrowLeft') {
      event.preventDefault();
      this.setTab(this.tabs[(currentIndex - 1 + total) % total].id);
    }
  }

  protected formatConfidence(score: number): string {
    return `${Math.round(score * 100)}%`;
  }

  protected trackTab(_index: number, tab: Tab): string {
    return tab.id;
  }

  protected trackCitation(_index: number, citation: Citation): string {
    return `${citation.documentName}-${citation.pageNumber}-${citation.paragraphRef}`;
  }

  protected trackRecommendation(index: number, _rec: string): number {
    return index;
  }

  protected get addedCount(): number {
    return this.result().changeLog.filter((e) => e.status === 'added').length;
  }

  protected get removedCount(): number {
    return this.result().changeLog.filter((e) => e.status === 'removed').length;
  }

  protected get modifiedCount(): number {
    return this.result().changeLog.filter((e) => e.status === 'modified').length;
  }
}
