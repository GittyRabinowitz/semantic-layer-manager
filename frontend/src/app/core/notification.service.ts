import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

/** Centralised user notifications (success toasts + error reporting). */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly snack = inject(MatSnackBar);

  success(message: string, durationMs = 3000): void {
    this.snack.open(message, 'OK', { duration: durationMs });
  }

  error(message: string, err?: unknown): void {
    const detail = err instanceof HttpErrorResponse ? ` (${err.status})` : '';
    if (err) console.error(message, err);
    this.snack.open(message + detail, 'Dismiss', { duration: 6000 });
  }
}
