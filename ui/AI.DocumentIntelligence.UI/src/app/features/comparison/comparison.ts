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

import { ComparisonApiService } from './services/comparison-api.service';
import { ComparisonType } from './models/comparison.models';
import { DocumentUploadComponent } from './components/document-upload/document-upload';
import { ComparisonTypeSelectorComponent } from './components/comparison-type-selector/comparison-type-selector';
import { ResultsPanelComponent } from './components/results-panel/results-panel';

@Component({
  selector: 'app-comparison',
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    DocumentUploadComponent,
    ComparisonTypeSelectorComponent,
    ResultsPanelComponent,
  ],
  templateUrl: './comparison.html',
  styleUrl: './comparison.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ComparisonComponent {
  protected readonly service = inject(ComparisonApiService);

  protected readonly files = signal<File[]>([]);
  protected readonly selectedType = signal<ComparisonType>('side-by-side');

  protected readonly canCompare = computed(
    () => this.files().length >= 2 && this.files().length <= 4 && !this.service.loading(),
  );

  protected startComparison(): void {
    if (!this.canCompare()) return;
    this.service.runComparison(this.files(), this.selectedType());
  }

  protected reset(): void {
    this.files.set([]);
    this.selectedType.set('side-by-side');
    this.service.reset();
  }

  protected onTypeChange(type: ComparisonType): void {
    this.selectedType.set(type);
  }

  protected onFilesChange(files: File[]): void {
    this.files.set(files);
  }

  protected dismissError(): void {
    this.service.setError(null);
  }
}
