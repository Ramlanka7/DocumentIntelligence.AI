import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';

import {
  AnalysisCapability,
  AnalysisCapabilityOption,
  ANALYSIS_CAPABILITY_OPTIONS,
  EXAMPLE_QUERIES,
} from '../../models/analysis.models';

@Component({
  selector: 'app-capability-selector',
  standalone: true,
  imports: [FormsModule, MatIconModule, MatButtonModule, MatInputModule, MatFormFieldModule],
  templateUrl: './capability-selector.html',
  styleUrl: './capability-selector.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CapabilitySelectorComponent {
  readonly selected = input.required<AnalysisCapability>();
  readonly customQuestion = input<string>('');

  readonly selectedChange = output<AnalysisCapability>();
  readonly customQuestionChange = output<string>();

  protected readonly options: AnalysisCapabilityOption[] = ANALYSIS_CAPABILITY_OPTIONS;
  protected readonly exampleQueries: string[] = EXAMPLE_QUERIES;

  protected get isCustom(): boolean {
    return this.selected() === 'CustomQuestion';
  }

  protected selectCapability(value: AnalysisCapability): void {
    this.selectedChange.emit(value);
  }

  protected onCustomQuestionInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.customQuestionChange.emit(input.value);
  }

  protected useExampleQuery(query: string): void {
    if (!this.isCustom) {
      this.selectedChange.emit('CustomQuestion');
    }
    this.customQuestionChange.emit(query);
  }
}
