import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, delay, switchMap } from 'rxjs';

import { environment } from '../../../../environments/environment';
import {
  ComparisonJobResponse,
  ComparisonResult,
  ComparisonType,
  CreateComparisonRequest,
  DocumentInfo,
  UploadDocumentResponse,
  DiffEntry,
  Citation,
} from '../models/comparison.models';

function buildMockResult(documentIds: string[], comparisonType: ComparisonType): ComparisonResult {
  const mockCitations: Citation[] = [
    {
      documentName: 'Document A.pdf',
      pageNumber: 3,
      paragraphRef: '§2.1',
      confidenceScore: 0.94,
    },
    {
      documentName: 'Document B.pdf',
      pageNumber: 5,
      paragraphRef: '§3.4',
      confidenceScore: 0.87,
    },
  ];

  const keyDifferences: DiffEntry[] = [
    {
      id: 'diff-001',
      status: 'modified',
      section: 'Payment Terms',
      contentBefore: 'Payment due within 30 days of invoice date.',
      contentAfter: 'Payment due within 45 days of invoice date.',
      changeType: 'pricing',
      citations: [
        { documentName: 'Document A.pdf', pageNumber: 2, paragraphRef: '§4.1', confidenceScore: 0.96 },
        { documentName: 'Document B.pdf', pageNumber: 3, paragraphRef: '§4.1', confidenceScore: 0.95 },
      ],
    },
    {
      id: 'diff-002',
      status: 'added',
      section: 'Indemnification Clause',
      contentBefore: undefined,
      contentAfter:
        'Either party shall indemnify and hold harmless the other party from any claims arising from gross negligence or wilful misconduct.',
      changeType: 'clause',
      citations: [
        { documentName: 'Document B.pdf', pageNumber: 7, paragraphRef: '§6.2', confidenceScore: 0.91 },
      ],
    },
    {
      id: 'diff-003',
      status: 'removed',
      section: 'Arbitration Waiver',
      contentBefore:
        'The parties waive their right to arbitration and agree to resolve disputes exclusively in state court.',
      contentAfter: undefined,
      changeType: 'compliance',
      citations: [
        { documentName: 'Document A.pdf', pageNumber: 9, paragraphRef: '§8.3', confidenceScore: 0.89 },
      ],
    },
    {
      id: 'diff-004',
      status: 'modified',
      section: 'Liability Cap',
      contentBefore: 'Total liability shall not exceed $500,000 USD.',
      contentAfter: 'Total liability shall not exceed $1,000,000 USD.',
      changeType: 'risk',
      citations: [
        { documentName: 'Document A.pdf', pageNumber: 10, paragraphRef: '§9.1', confidenceScore: 0.98 },
        { documentName: 'Document B.pdf', pageNumber: 11, paragraphRef: '§9.1', confidenceScore: 0.97 },
      ],
    },
  ];

  const changeLog: DiffEntry[] = [
    ...keyDifferences,
    {
      id: 'diff-005',
      status: 'added',
      section: 'Force Majeure',
      contentBefore: undefined,
      contentAfter:
        'Neither party shall be liable for delays or failures in performance resulting from circumstances beyond their reasonable control, including natural disasters and pandemics.',
      changeType: 'general',
      citations: [
        { documentName: 'Document B.pdf', pageNumber: 12, paragraphRef: '§10.5', confidenceScore: 0.88 },
      ],
    },
    {
      id: 'diff-006',
      status: 'modified',
      section: 'Governing Law',
      contentBefore: 'This Agreement shall be governed by the laws of the State of California.',
      contentAfter: 'This Agreement shall be governed by the laws of the State of New York.',
      changeType: 'compliance',
      citations: [
        { documentName: 'Document A.pdf', pageNumber: 14, paragraphRef: '§12.1', confidenceScore: 0.99 },
        { documentName: 'Document B.pdf', pageNumber: 15, paragraphRef: '§12.1', confidenceScore: 0.99 },
      ],
    },
  ];

  const typeLabel =
    comparisonType === 'side-by-side'
      ? 'Side-by-Side'
      : comparisonType === 'version'
        ? 'Version'
        : comparisonType === 'contract'
          ? 'Contract'
          : comparisonType === 'policy'
            ? 'Policy'
            : 'Custom';

  return {
    id: `comparison-${Date.now()}`,
    executiveOverview: `${typeLabel} comparison of ${documentIds.length} document(s) completed. AI analysis identified 6 significant differences spanning payment terms, liability provisions, dispute resolution mechanisms, and compliance obligations. The revised document (Document B) introduces a higher liability cap and new indemnification language that materially alters risk exposure. The removal of the arbitration waiver and the jurisdiction change from California to New York represent significant legal risk shifts that require immediate attention from legal counsel.`,
    keyDifferences,
    riskAnalysis:
      'The comparison reveals a net increase in financial exposure. The liability cap doubling from $500K to $1M represents a 100% increase in maximum financial risk. The removal of the arbitration waiver exposes both parties to potentially longer and costlier litigation in state courts. The new indemnification clause, while mutual, broadens the scope of liability scenarios. The jurisdiction change from California to New York may require adaptation of compliance programs to New York-specific regulations. Overall risk rating: HIGH — legal review recommended before execution.',
    recommendations: [
      'Engage legal counsel to review the doubling of the liability cap before signing.',
      'Assess whether the removal of the arbitration waiver aligns with your dispute resolution strategy.',
      'Verify that your compliance framework is compatible with New York governing law.',
      'Request clarification on the scope of the new indemnification clause, particularly around "wilful misconduct" definitions.',
      'Consider negotiating a mutual limitation on the force majeure clause to include notice requirements.',
      'Conduct a full redline review with all stakeholders before final execution.',
    ],
    changeLog,
    citations: mockCitations,
  };
}

@Injectable({ providedIn: 'root' })
export class ComparisonApiService {
  private readonly http = inject(HttpClient);
  private readonly apiBase = environment.apiBaseUrl;

  // Private writable signals — mutated only within this service
  private readonly _uploadedDocs = signal<DocumentInfo[]>([]);
  private readonly _comparisonType = signal<ComparisonType>('side-by-side');
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _result = signal<ComparisonResult | null>(null);

  // Public readonly signals — consumed by components
  readonly uploadedDocs = this._uploadedDocs.asReadonly();
  readonly comparisonType = this._comparisonType.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly result = this._result.asReadonly();

  uploadDocument(file: File): Observable<UploadDocumentResponse> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<UploadDocumentResponse>(`${this.apiBase}/documents/upload`, formData);
  }

  createComparison(request: CreateComparisonRequest): Observable<ComparisonJobResponse> {
    return this.http.post<ComparisonJobResponse>(`${this.apiBase}/comparisons`, request);
  }

  getComparison(id: string): Observable<ComparisonJobResponse> {
    return this.http.get<ComparisonJobResponse>(`${this.apiBase}/comparisons/${id}`);
  }

  /**
   * Orchestrates the full comparison flow:
   *  1. Upload all files (multipart)
   *  2. POST /comparisons with document IDs and type
   *  3. Poll GET /comparisons/{id} until completed
   *
   * Falls back to mock data when the backend is unavailable so the UI remains
   * fully functional for demonstration.
   */
  runComparison(files: File[], type: ComparisonType): void {
    this._loading.set(true);
    this._error.set(null);
    this._result.set(null);

    // Attempt real API calls; fall back to mock on error.
    this.uploadDocument(files[0]).subscribe({
      next: (firstDoc) => {
        const docIds: string[] = [firstDoc.id];
        const remainingUploads = files.slice(1).map((f) => this.uploadDocument(f));
        this.uploadRemainingAndCompare(remainingUploads, docIds, type);
      },
      error: () => {
        // Backend unavailable — use mock data for demonstration.
        this.runMockComparison(files, type);
      },
    });
  }

  private uploadRemainingAndCompare(
    uploads: Observable<UploadDocumentResponse>[],
    collectedIds: string[],
    type: ComparisonType,
  ): void {
    if (uploads.length === 0) {
      this.submitComparison(collectedIds, type);
      return;
    }

    const [first, ...rest] = uploads;
    first.subscribe({
      next: (doc) => {
        collectedIds.push(doc.id);
        this.uploadRemainingAndCompare(rest, collectedIds, type);
      },
      error: () => {
        this._loading.set(false);
        this._error.set('Failed to upload one or more documents. Please try again.');
      },
    });
  }

  private submitComparison(documentIds: string[], type: ComparisonType): void {
    const request: CreateComparisonRequest = { documentIds, comparisonType: type };
    this.createComparison(request).subscribe({
      next: (job) => {
        this.pollComparison(job.id);
      },
      error: () => {
        this._loading.set(false);
        this._error.set('Failed to start comparison. Please try again.');
      },
    });
  }

  private pollComparison(jobId: string): void {
    this.getComparison(jobId).subscribe({
      next: (job) => {
        if (job.status === 'completed' && job.result) {
          this._result.set(job.result);
          this._loading.set(false);
        } else if (job.status === 'failed') {
          this._error.set(job.errorMessage ?? 'Comparison failed. Please try again.');
          this._loading.set(false);
        } else {
          // Still processing — poll again after 2 seconds
          of(null)
            .pipe(
              delay(2000),
              switchMap(() => this.getComparison(jobId)),
            )
            .subscribe({
              next: (retryJob) => {
                if (retryJob.status === 'completed' && retryJob.result) {
                  this._result.set(retryJob.result);
                } else if (retryJob.status === 'failed') {
                  this._error.set(retryJob.errorMessage ?? 'Comparison failed. Please try again.');
                } else {
                  this._error.set('Comparison is taking longer than expected. Please refresh.');
                }
                this._loading.set(false);
              },
              error: () => {
                this._loading.set(false);
                this._error.set('Lost connection while waiting for comparison result.');
              },
            });
        }
      },
      error: () => {
        this._loading.set(false);
        this._error.set('Failed to retrieve comparison result. Please try again.');
      },
    });
  }

  private runMockComparison(files: File[], type: ComparisonType): void {
    const mockDocIds = files.map((_, i) => `mock-doc-${i + 1}`);
    of(buildMockResult(mockDocIds, type))
      .pipe(delay(2500))
      .subscribe((mockResult) => {
        this._result.set(mockResult);
        this._loading.set(false);
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
    this._comparisonType.set('side-by-side');
    this._loading.set(false);
    this._error.set(null);
    this._result.set(null);
  }
}
