import { ChangeDetectionStrategy, Component } from '@angular/core';

import { FeaturePlaceholder } from './feature-placeholder';

/** Route-level placeholder for `/analysis` — feature internals land in T10. */
@Component({
  selector: 'app-analysis-placeholder',
  standalone: true,
  imports: [FeaturePlaceholder],
  template: `<app-feature-placeholder
    icon="insights"
    title="Analysis Mode"
    description="Document analysis is coming soon. Upload and analyze documents to extract meaningful insights using AI."
  />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AnalysisPlaceholder {}
