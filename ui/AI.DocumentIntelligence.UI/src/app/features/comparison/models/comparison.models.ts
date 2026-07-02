export type ComparisonType =
  | 'side-by-side'
  | 'version'
  | 'contract'
  | 'policy'
  | 'custom';

export interface ComparisonTypeOption {
  value: ComparisonType;
  label: string;
  description: string;
  icon: string;
}

export interface DocumentInfo {
  id: string;
  name: string;
  size: number;
}

export interface Citation {
  documentName: string;
  pageNumber: number;
  paragraphRef: string;
  confidenceScore: number; // 0–1
}

export type DiffStatus = 'added' | 'removed' | 'modified';

export interface DiffEntry {
  id: string;
  status: DiffStatus;
  section: string;
  contentBefore?: string;
  contentAfter?: string;
  changeType: 'clause' | 'pricing' | 'risk' | 'compliance' | 'general';
  citations: Citation[];
}

export interface ComparisonResult {
  id: string;
  executiveOverview: string;
  keyDifferences: DiffEntry[];
  riskAnalysis: string;
  recommendations: string[];
  changeLog: DiffEntry[];
  citations: Citation[];
}

export interface UploadDocumentResponse {
  id: string;
  name: string;
  size: number;
  contentType: string;
  uploadedAt: string;
}

export interface CreateComparisonRequest {
  documentIds: string[];
  comparisonType: ComparisonType;
}

export interface ComparisonJobResponse {
  id: string;
  status: 'pending' | 'processing' | 'completed' | 'failed';
  result?: ComparisonResult;
  errorMessage?: string;
}

export const COMPARISON_TYPE_OPTIONS: ComparisonTypeOption[] = [
  {
    value: 'side-by-side',
    label: 'Side-by-Side Comparison',
    description: 'View documents in parallel columns to spot differences visually.',
    icon: 'compare',
  },
  {
    value: 'version',
    label: 'Version Comparison',
    description: 'Track how a document evolved across versions over time.',
    icon: 'history',
  },
  {
    value: 'contract',
    label: 'Contract Comparison',
    description: 'Analyze clause-level changes, obligations, and legal risk deltas.',
    icon: 'gavel',
  },
  {
    value: 'policy',
    label: 'Policy Comparison',
    description: 'Identify compliance gaps and policy divergences between documents.',
    icon: 'policy',
  },
  {
    value: 'custom',
    label: 'Custom Comparison',
    description: 'Define your own comparison criteria and focus areas.',
    icon: 'tune',
  },
];
