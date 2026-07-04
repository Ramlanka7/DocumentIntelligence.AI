import type { AnalysisRecommendation, RiskItem } from '../../analysis/models/analysis.models';

/**
 * ComparisonType values MUST match the C# Domain/Enums/ComparisonType enum member names
 * exactly (PascalCase) for JSON serialisation.
 */
export type ComparisonType =
  | 'SideBySide'
  | 'Version'
  | 'Contract'
  | 'Policy'
  | 'Custom';

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

/** Citation shape from Application/Contracts/Citation.cs (camelCase serialised) */
export interface Citation {
  documentId: string;
  documentName: string;
  pageNumber: number;
  paragraphReference: string;
  snippet: string;
  confidenceScore: number;
}

/** DifferenceType values from Application/Contracts/Comparison/DifferenceType.cs */
export type DifferenceType = 'Added' | 'Removed' | 'Modified';

/** DocumentDifference.cs — Type, Section, Before, After, Summary, Citations */
export interface DocumentDifference {
  type: DifferenceType;
  section: string;
  before: string | null;
  after: string | null;
  summary: string;
  citations: Citation[];
}

/**
 * ComparisonResult.cs — ExecutiveOverview, Differences, Risks, Recommendations, Sources
 * Fields mirror the backend record (camelCase serialised).
 */
export interface ComparisonResult {
  executiveOverview: string;
  differences: DocumentDifference[];
  risks: RiskItem[];
  recommendations: AnalysisRecommendation[];
  sources: Citation[];
}

/** Response from POST /api/v1/documents — mirrors UploadDocumentResponse.cs */
export interface UploadDocumentResponse {
  documentId: string;
  fileName: string;
  status: string;
}

/** Request body for POST /api/v1/comparison */
export interface CompareDocumentsRequest {
  documentIds: string[];
  comparisonType: ComparisonType;
  customInstructions?: string;
}

export const COMPARISON_TYPE_OPTIONS: ComparisonTypeOption[] = [
  {
    value: 'SideBySide',
    label: 'Side-by-Side Comparison',
    description: 'View documents in parallel columns to spot differences visually.',
    icon: 'compare',
  },
  {
    value: 'Version',
    label: 'Version Comparison',
    description: 'Track how a document evolved across versions over time.',
    icon: 'history',
  },
  {
    value: 'Contract',
    label: 'Contract Comparison',
    description: 'Analyze clause-level changes, obligations, and legal risk deltas.',
    icon: 'gavel',
  },
  {
    value: 'Policy',
    label: 'Policy Comparison',
    description: 'Identify compliance gaps and policy divergences between documents.',
    icon: 'policy',
  },
  {
    value: 'Custom',
    label: 'Custom Comparison',
    description: 'Define your own comparison criteria and focus areas.',
    icon: 'tune',
  },
];
