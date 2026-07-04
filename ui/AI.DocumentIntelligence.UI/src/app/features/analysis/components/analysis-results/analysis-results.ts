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

import {
  AnalysisResult,
  AnalysisCitation,
  KeyFinding,
  RiskItem,
  AnalysisRecommendation,
  ActionItem,
} from '../../models/analysis.models';

type ActiveTab =
  | 'summary'
  | 'findings'
  | 'risks'
  | 'recommendations'
  | 'actions'
  | 'sources';

interface Tab {
  id: ActiveTab;
  label: string;
  icon: string;
}

@Component({
  selector: 'app-analysis-results',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, MatDividerModule],
  templateUrl: './analysis-results.html',
  styleUrl: './analysis-results.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AnalysisResultsComponent {
  readonly result = input.required<AnalysisResult>();
  readonly newAnalysis = output<void>();

  protected readonly activeTab = signal<ActiveTab>('summary');

  protected readonly tabs: Tab[] = [
    { id: 'summary', label: 'Executive Summary', icon: 'summarize' },
    { id: 'findings', label: 'Key Findings', icon: 'search_insights' },
    { id: 'risks', label: 'Risks Identified', icon: 'warning' },
    { id: 'recommendations', label: 'Recommendations', icon: 'lightbulb' },
    { id: 'actions', label: 'Action Items', icon: 'task_alt' },
    { id: 'sources', label: 'Referenced Sources', icon: 'source' },
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

  protected getSeverityClass(severity: RiskItem['severity']): string {
    switch (severity) {
      case 'Critical': return 'severity--critical';
      case 'High': return 'severity--high';
      case 'Medium': return 'severity--medium';
      default: return 'severity--low';
    }
  }

  protected trackTab(_index: number, tab: Tab): string {
    return tab.id;
  }

  protected trackFinding(_index: number, finding: KeyFinding): string {
    return finding.title;
  }

  protected trackRisk(_index: number, risk: RiskItem): string {
    return risk.title;
  }

  protected trackRecommendation(_index: number, rec: AnalysisRecommendation): string {
    return rec.title;
  }

  protected trackAction(_index: number, action: ActionItem): string {
    return action.description;
  }

  protected trackCitation(_index: number, citation: AnalysisCitation): string {
    return `${citation.documentName}-${citation.pageNumber}-${citation.paragraphReference}`;
  }
}
