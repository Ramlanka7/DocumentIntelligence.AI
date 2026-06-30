import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { RouterOutlet } from '@angular/router';

/**
 * Placeholder application shell. Hosts the top-level dark-theme layout only;
 * no product features live here yet — those land in later tasks (T09+).
 */
@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, MatToolbarModule, MatIconModule],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Shell {
  protected readonly title = signal('AI Document Intelligence Platform');
}
