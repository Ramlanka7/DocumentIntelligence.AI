import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { DocumentReadinessService } from '../../../core/services/document-readiness.service';
import {
  CompareDocumentsRequest,
  ComparisonResult,
  ComparisonType,
  DocumentInfo,
  UploadDocumentResponse,
} from '../models/comparison.models';

@Injectable({ providedIn: 'root' })
export class ComparisonApiService {
  private readonly http = inject(HttpClient);
  private readonly readiness = inject(DocumentReadinessService);
  private readonly apiBase = environment.apiBaseUrl;

  // Private writable signals — mutated only within this service
  private readonly _uploadedDocs = signal<DocumentInfo[]>([]);
  private readonly _comparisonType = signal<ComparisonType>('SideBySide');
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _result = signal<ComparisonResult | null>(null);

  // Public readonly signals — consumed by components
  readonly uploadedDocs = this._uploadedDocs.asReadonly();
  readonly comparisonType = this._comparisonType.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly result = this._result.asReadonly();

  /**
   * Upload a single document.
   * POST /api/v1/documents — multipart form field name: file
   * Response: { documentId, fileName, status }
   */
  uploadDocument(file: File): Observable<UploadDocumentResponse> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<UploadDocumentResponse>(`${this.apiBase}/documents`, formData);
  }

  /**
   * Compare documents synchronously.
   * POST /api/v1/comparison — body: { documentIds, comparisonType, customInstructions? }
   * Returns ComparisonResult directly (no job polling).
   */
  compareDocuments(request: CompareDocumentsRequest): Observable<ComparisonResult> {
    return this.http.post<ComparisonResult>(`${this.apiBase}/comparison`, request);
  }

  /**
   * Orchestrates the full comparison flow:
   *  1. Upload all files (multipart) sequentially.
   *  2. POST /api/v1/comparison with collected document IDs and type.
   *
   * On any HTTP error the error signal is set and no fabricated data is served.
   */
  runComparison(files: File[], type: ComparisonType, customInstructions?: string): void {
    this._loading.set(true);
    this._error.set(null);
    this._result.set(null);

    this.uploadSequential(files, 0, [], type, customInstructions);
  }

  private uploadSequential(
    files: File[],
    index: number,
    collectedIds: string[],
    type: ComparisonType,
    customInstructions?: string,
  ): void {
    if (index >= files.length) {
      // Ingestion is asynchronous server-side: wait until every document is
      // Processed before comparing, otherwise the AI call is rejected
      // (Document.NotProcessed) or would run without retrieval context.
      this.readiness.waitForProcessed(collectedIds).subscribe({
        next: () => this.submitComparison(collectedIds, type, customInstructions),
        error: (err: unknown) => {
          this._loading.set(false);
          // Surface readiness-service messages (timeouts, failed documents) but
          // never raw HTTP error bodies.
          if (!(err instanceof HttpErrorResponse) && err instanceof Error) {
            this._error.set(err.message);
          } else {
            this._error.set('Failed to check document processing status. Please try again.');
          }
        },
      });
      return;
    }

    this.uploadDocument(files[index]).subscribe({
      next: (response) => {
        collectedIds.push(response.documentId);
        this.uploadSequential(files, index + 1, collectedIds, type, customInstructions);
      },
      error: (err: HttpErrorResponse) => {
        this._loading.set(false);
        if (err.status === 403) {
          this._error.set('You do not have permission to upload documents. The Analyst role is required.');
        } else {
          this._error.set(`Failed to upload "${files[index].name}". Please try again.`);
        }
      },
    });
  }

  private submitComparison(
    documentIds: string[],
    type: ComparisonType,
    customInstructions?: string,
  ): void {
    const request: CompareDocumentsRequest = {
      documentIds,
      comparisonType: type,
      ...(customInstructions ? { customInstructions } : {}),
    };

    this.compareDocuments(request).subscribe({
      next: (result) => {
        this._result.set(result);
        this._loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this._loading.set(false);
        if (err.status === 403) {
          this._error.set('You do not have permission to run comparisons. The Analyst role is required.');
        } else if (err.status === 400) {
          this._error.set('Invalid comparison request. Please check your inputs and try again.');
        } else {
          this._error.set('Comparison failed. Please try again.');
        }
      },
    });
  }

  addDocument(doc: DocumentInfo): void {
    this._uploadedDocs.update((docs) => [...docs, doc]);
  }

  removeDocument(id: string): void {
    this._uploadedDocs.update((docs) => docs.filter((d) => d.id !== id));
  }

  setComparisonType(type: ComparisonType): void {
    this._comparisonType.set(type);
  }

  setError(msg: string | null): void {
    this._error.set(msg);
  }

  reset(): void {
    this._uploadedDocs.set([]);
    this._comparisonType.set('SideBySide');
    this._loading.set(false);
    this._error.set(null);
    this._result.set(null);
  }
}
