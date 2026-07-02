import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import {
  ComparisonType,
  ComparisonTypeOption,
  COMPARISON_TYPE_OPTIONS,
} from '../../models/comparison.models';

@Component({
  selector: 'app-comparison-type-selector',
  standalone: true,
  imports: [MatButtonModule, MatIconModule],
  templateUrl: './comparison-type-selector.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ComparisonTypeSelectorComponent {
  readonly selected = input<ComparisonType>('side-by-side');
  readonly selectedChange = output<ComparisonType>();

  protected readonly options: ComparisonTypeOption[] = COMPARISON_TYPE_OPTIONS;

  protected select(type: ComparisonType): void {
    this.selectedChange.emit(type);
  }

  protected isSelected(type: ComparisonType): boolean {
    return this.selected() === type;
  }
}
