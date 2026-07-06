import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { DocumentReadinessService } from '../../../core/services/document-readiness.service';
import {
  AnalysisCapability,
  AnalysisFile,
  AnalysisResult,
  AnalyzeDocumentsRequest,
  UploadDocumentResponse,
} from '../models/analysis.models';

@Injectable({ providedIn: 'root' })
export class AnalysisApiService {
  private readonly http = inject(HttpClient);
  private readonly readiness = inject(DocumentReadinessService);
  private readonly apiBase = environment.apiBaseUrl;

  // Private writable signals
  private readonly _analysisFiles = signal<AnalysisFile[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _result = signal<AnalysisResult | null>(null);
  private readonly _forbidden = signal(false);

  // Public readonly signals
  readonly analysisFiles = this._analysisFiles.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly result = this._result.asReadonly();
  readonly forbidden = this._forbidden.asReadonly();

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
   * Run an analysis against already-uploaded documents.
   * POST /api/v1/analysis — body: { documentIds, capability, customQuestion? }
   */
  analyzeDocuments(request: AnalyzeDocumentsRequest): Observable<AnalysisResult> {
    return this.http.post<AnalysisResult>(`${this.apiBase}/analysis`, request);
  }

  /**
   * Orchestrates the full analysis flow:
   *  1. Upload all files (multipart) sequentially — updates per-file status.
   *  2. POST /api/v1/analysis with collected document IDs and capability.
   *
   * On any HTTP error the error signal is set and no fabricated data is served.
   */
  runAnalysis(files: File[], capability: AnalysisCapability, customQuestion?: string): void {
    this._loading.set(true);
    this._error.set(null);
    this._result.set(null);
    this._forbidden.set(false);

    const analysisFiles: AnalysisFile[] = files.map((file) => ({
      file,
      status: 'uploading',
      progress: 0,
    }));
    this._analysisFiles.set(analysisFiles);

    this.uploadSequential(files, 0, [], capability, customQuestion);
  }

  private uploadSequential(
    files: File[],
    index: number,
    collectedIds: string[],
    capability: AnalysisCapability,
    customQuestion?: string,
  ): void {
    if (index >= files.length) {
      // Ingestion is asynchronous server-side: wait until every document is
      // Processed before analyzing, otherwise the AI call is rejected
      // (Document.NotProcessed) or would run without retrieval context.
      this.readiness.waitForProcessed(collectedIds).subscribe({
        next: () => this.submitAnalysis(collectedIds, capability, customQuestion),
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
        this.updateFileStatus(index, 'success', response.documentId);
        this.uploadSequential(files, index + 1, collectedIds, capability, customQuestion);
      },
      error: (err: HttpErrorResponse) => {
        this.updateFileStatus(index, 'error', undefined, 'Upload failed');
        this._loading.set(false);
        if (err.status === 403) {
          this._forbidden.set(true);
          this._error.set('You do not have permission to upload documents. The Analyst role is required.');
        } else {
          this._error.set(`Failed to upload "${files[index].name}". Please try again.`);
        }
      },
    });
  }

  private submitAnalysis(
    documentIds: string[],
    capability: AnalysisCapability,
    customQuestion?: string,
  ): void {
    const request: AnalyzeDocumentsRequest = {
      documentIds,
      capability,
      ...(customQuestion ? { customQuestion } : {}),
    };

    this.analyzeDocuments(request).subscribe({
      next: (result) => {
        this._result.set(result);
        this._loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this._loading.set(false);
        if (err.status === 403) {
          this._forbidden.set(true);
          this._error.set('You do not have permission to run analysis. The Analyst role is required.');
        } else if (err.status === 400) {
          this._error.set('Invalid analysis request. Please check your inputs and try again.');
        } else {
          this._error.set('Analysis failed. Please try again.');
        }
      },
    });
  }

  private updateFileStatus(
    index: number,
    status: AnalysisFile['status'],
    uploadedId?: string,
    errorMessage?: string,
  ): void {
    this._analysisFiles.update((files) =>
      files.map((f, i) =>
        i === index
          ? { ...f, status, uploadedId, errorMessage, progress: status === 'success' ? 100 : f.progress }
          : f,
      ),
    );
  }

  setError(msg: string | null): void {
    this._error.set(msg);
  }

  reset(): void {
    this._analysisFiles.set([]);
    this._loading.set(false);
    this._error.set(null);
    this._result.set(null);
    this._forbidden.set(false);
  }
}
