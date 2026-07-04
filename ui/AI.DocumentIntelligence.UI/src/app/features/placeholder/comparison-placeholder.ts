import { ChangeDetectionStrategy, Component } from '@angular/core';

import { FeaturePlaceholder } from './feature-placeholder';

/** Route-level placeholder for `/compare` — feature internals land in T11. */
@Component({
  selector: 'app-comparison-placeholder',
  standalone: true,
  imports: [FeaturePlaceholder],
  template: `<app-feature-placeholder
    icon="compare_arrows"
    title="Comparison Mode"
    description="Document comparison is coming soon. Compare multiple documents and identify key differences using intelligent analysis."
  />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ComparisonPlaceholder {}
