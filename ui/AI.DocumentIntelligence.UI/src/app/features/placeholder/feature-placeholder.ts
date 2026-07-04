import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

/**
 * Generic "coming soon" placeholder used by routes whose feature internals are built in
 * later tasks (T10 analysis, T11 comparison, T12 chat, T13 admin). Keeps the shell's
 * routing/guards meaningful without building out any feature UI ahead of scope.
 */
@Component({
  selector: 'app-feature-placeholder',
  standalone: true,
  imports: [MatCardModule, MatIconModule],
  templateUrl: './feature-placeholder.html',
  styleUrl: './feature-placeholder.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FeaturePlaceholder {
  readonly icon = input.required<string>();
  readonly title = input.required<string>();
  readonly description = input<string>('This feature is under active development.');
}
