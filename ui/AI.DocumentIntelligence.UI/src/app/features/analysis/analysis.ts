import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';

import { AnalysisApiService } from './services/analysis-api.service';
import { AnalysisCapability } from './models/analysis.models';
import { AnalysisUploadComponent } from './components/analysis-upload/analysis-upload';
import { CapabilitySelectorComponent } from './components/capability-selector/capability-selector';
import { AnalysisResultsComponent } from './components/analysis-results/analysis-results';

@Component({
  selector: 'app-analysis',
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    AnalysisUploadComponent,
    CapabilitySelectorComponent,
    AnalysisResultsComponent,
  ],
  templateUrl: './analysis.html',
  styleUrl: './analysis.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AnalysisComponent {
  protected readonly service = inject(AnalysisApiService);

  protected readonly files = signal<File[]>([]);
  protected readonly selectedCapability = signal<AnalysisCapability>('ExecutiveSummary');
  protected readonly customQuestion = signal<string>('');

  protected readonly canAnalyse = computed(
    () =>
      this.files().length >= 1 &&
      this.files().length <= 4 &&
      !this.service.loading() &&
      (this.selectedCapability() !== 'CustomQuestion' ||
        this.customQuestion().trim().length > 0),
  );

  protected startAnalysis(): void {
    if (!this.canAnalyse()) return;
    const question =
      this.selectedCapability() === 'CustomQuestion'
        ? this.customQuestion().trim()
        : undefined;
    this.service.runAnalysis(this.files(), this.selectedCapability(), question);
  }

  protected reset(): void {
    this.files.set([]);
    this.selectedCapability.set('ExecutiveSummary');
    this.customQuestion.set('');
    this.service.reset();
  }

  protected onFilesChange(files: File[]): void {
    this.files.set(files);
  }

  protected onCapabilityChange(capability: AnalysisCapability): void {
    this.selectedCapability.set(capability);
  }

  protected onCustomQuestionChange(question: string): void {
    this.customQuestion.set(question);
  }

  protected dismissError(): void {
    this.service.setError(null);
  }
}
