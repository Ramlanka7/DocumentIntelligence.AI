import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, forkJoin, map, of, switchMap, throwError, timer } from 'rxjs';

import { environment } from '../../../environments/environment';

/** Minimal shape of GET /api/v1/documents/{id} needed for readiness polling. */
interface DocumentStatusResponse {
  id: string;
  status: string;
  failureReason?: string | null;
}

/**
 * Waits for uploaded documents to finish background ingestion.
 *
 * Uploads return documents in `Processing` state; the server ingests (chunk → embed →
 * index) asynchronously and rejects AI operations until a document is `Processed`.
 * Feature services call `waitForProcessed(ids)` between upload and analyze/compare.
 */
@Injectable({ providedIn: 'root' })
export class DocumentReadinessService {
  private static readonly POLL_INTERVAL_MS = 1500;
  private static readonly MAX_ATTEMPTS = 80; // × 1.5 s ≈ 2 minutes ceiling

  private readonly http = inject(HttpClient);
  private readonly apiBase = environment.apiBaseUrl;

  /**
   * Resolves when every document reaches `Processed`.
   * Errors when any document reaches `Failed` or the timeout elapses.
   */
  waitForProcessed(documentIds: string[]): Observable<void> {
    if (documentIds.length === 0) {
      return of(undefined);
    }

    return this.pollOnce(documentIds, 0);
  }

  private pollOnce(documentIds: string[], attempt: number): Observable<void> {
    return forkJoin(
      documentIds.map((id) =>
        this.http.get<DocumentStatusResponse>(`${this.apiBase}/documents/${id}`),
      ),
    ).pipe(
      switchMap((documents) => {
        const failed = documents.find((d) => d.status === 'Failed');
        if (failed) {
          return throwError(
            () => new Error(failed.failureReason ?? 'Document processing failed.'),
          );
        }

        if (documents.every((d) => d.status === 'Processed')) {
          return of(undefined);
        }

        if (attempt >= DocumentReadinessService.MAX_ATTEMPTS) {
          return throwError(
            () => new Error('Documents are taking too long to process. Please try again shortly.'),
          );
        }

        return timer(DocumentReadinessService.POLL_INTERVAL_MS).pipe(
          switchMap(() => this.pollOnce(documentIds, attempt + 1)),
        );
      }),
      map(() => undefined),
    );
  }
}
