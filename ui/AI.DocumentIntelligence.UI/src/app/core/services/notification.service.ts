import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

/**
 * Thin wrapper around `MatSnackBar` so the rest of the app (interceptors, feature
 * services) has a single, typed entry point for user-facing toast notifications.
 */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly snackBar = inject(MatSnackBar);

  error(message: string): void {
    this.snackBar.open(message, 'Dismiss', {
      duration: 6000,
      panelClass: ['adip-snackbar', 'adip-snackbar-error'],
      horizontalPosition: 'end',
      verticalPosition: 'top',
    });
  }

  info(message: string): void {
    this.snackBar.open(message, 'Dismiss', {
      duration: 4000,
      panelClass: ['adip-snackbar', 'adip-snackbar-info'],
      horizontalPosition: 'end',
      verticalPosition: 'top',
    });
  }
}
