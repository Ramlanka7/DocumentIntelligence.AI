import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { Router, RouterLink, RouterOutlet } from '@angular/router';

import { AuthStore } from '../core/auth/auth-store';

/**
 * Application shell: hosts the dark-theme toolbar, primary navigation, auth-aware user
 * menu, and the routed outlet. Every other feature route renders inside `<router-outlet>`.
 */
@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    MatToolbarModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    MatDividerModule,
  ],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Shell {
  private readonly authStore = inject(AuthStore);
  private readonly router = inject(Router);

  protected readonly title = signal('AI Document Intelligence Platform');
  protected readonly isAuthenticated = this.authStore.isAuthenticated;
  protected readonly user = this.authStore.user;

  protected readonly navLinks: ReadonlyArray<{ label: string; path: string }> = [
    { label: 'Analysis', path: '/analysis' },
    { label: 'Comparison', path: '/compare' },
    { label: 'Chat', path: '/chat' },
  ];

  /** True when the authenticated user has the Admin role. */
  protected readonly isAdmin = computed(() => this.user()?.role === 'Admin');

  protected signOut(): void {
    this.authStore.logout().subscribe(() => {
      void this.router.navigateByUrl('/login');
    });
  }
}
