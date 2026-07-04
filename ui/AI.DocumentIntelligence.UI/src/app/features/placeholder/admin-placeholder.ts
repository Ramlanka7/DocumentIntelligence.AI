import { ChangeDetectionStrategy, Component } from '@angular/core';

import { FeaturePlaceholder } from './feature-placeholder';

/** Route-level placeholder for `/admin` — feature internals land in T13. Admin-only route. */
@Component({
  selector: 'app-admin-placeholder',
  standalone: true,
  imports: [FeaturePlaceholder],
  template: `<app-feature-placeholder
    icon="admin_panel_settings"
    title="Admin Dashboard"
    description="The admin dashboard is coming soon. Manage users, review audit logs, and monitor platform usage."
  />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminPlaceholder {}
