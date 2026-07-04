/**
 * Analysis feature models — field names mirror the backend contracts exactly:
 * Application/Contracts/Analysis/{AnalysisResult, KeyFinding, RiskItem, ActionItem, Recommendation}.cs
 * Application/Contracts/Citation.cs
 */

/** Full citation as returned by the API. */
export interface AnalysisCitation {
  documentId: string;
  documentName: string;
  pageNumber: number;
  paragraphReference: string;
  snippet: string;
  confidenceScore: number;
}

/** KeyFinding.cs — Title, Detail, Citations */
export interface KeyFinding {
  title: string;
  detail: string;
  citations: AnalysisCitation[];
}

/** RiskItem.cs — Title, Description, Severity, Citations */
export interface RiskItem {
  title: string;
  description: string;
  severity: 'Low' | 'Medium' | 'High' | 'Critical';
  citations: AnalysisCitation[];
}

/** Recommendation.cs — Title, Detail, Citations */
export interface AnalysisRecommendation {
  title: string;
  detail: string;
  citations: AnalysisCitation[];
}

/** ActionItem.cs — Description, Owner, Citations */
export interface ActionItem {
  description: string;
  owner: string | null;
  citations: AnalysisCitation[];
}

/**
 * AnalysisResult.cs — ExecutiveSummary, KeyFindings, Risks, Recommendations, ActionItems, Sources
 */
export interface AnalysisResult {
  executiveSummary: string;
  keyFindings: KeyFinding[];
  risks: RiskItem[];
  recommendations: AnalysisRecommendation[];
  actionItems: ActionItem[];
  sources: AnalysisCitation[];
}

/** AnalyzeDocumentsCommand — DocumentIds, Capability, CustomQuestion */
export interface AnalyzeDocumentsRequest {
  documentIds: string[];
  capability: AnalysisCapability;
  customQuestion?: string;
}

/**
 * The 8 capabilities from Domain/Enums/AnalysisCapability.cs.
 * Values MUST match the C# enum member names exactly (PascalCase) for JSON serialisation.
 */
export type AnalysisCapability =
  | 'ExecutiveSummary'
  | 'KeyInsights'
  | 'ActionItems'
  | 'RiskAssessment'
  | 'ComplianceReview'
  | 'FinancialAnalysis'
  | 'SentimentAnalysis'
  | 'CustomQuestion';

export interface AnalysisCapabilityOption {
  value: AnalysisCapability;
  label: string;
  description: string;
  icon: string;
}

/** Response from POST /api/v1/documents — mirrors UploadDocumentResponse.cs */
export interface UploadDocumentResponse {
  documentId: string;
  fileName: string;
  status: string;
}

/** Upload status for a single file in the analysis upload list. */
export type FileUploadStatus = 'pending' | 'uploading' | 'success' | 'error';

export interface AnalysisFile {
  file: File;
  status: FileUploadStatus;
  uploadedId?: string;
  errorMessage?: string;
  progress: number;
}

export const ANALYSIS_CAPABILITY_OPTIONS: AnalysisCapabilityOption[] = [
  {
    value: 'ExecutiveSummary',
    label: 'Executive Summary',
    description: 'Generate a concise, high-level summary of all key points in the document.',
    icon: 'summarize',
  },
  {
    value: 'KeyInsights',
    label: 'Key Insights',
    description: 'Surface the most important insights and patterns found across the content.',
    icon: 'lightbulb',
  },
  {
    value: 'ActionItems',
    label: 'Action Items',
    description: 'Extract concrete tasks and follow-up actions mentioned in the documents.',
    icon: 'task_alt',
  },
  {
    value: 'RiskAssessment',
    label: 'Risk Assessment',
    description: 'Identify and rate risks with severity levels and supporting evidence.',
    icon: 'warning',
  },
  {
    value: 'ComplianceReview',
    label: 'Compliance Review',
    description: 'Evaluate compliance gaps and flag regulatory obligations.',
    icon: 'policy',
  },
  {
    value: 'FinancialAnalysis',
    label: 'Financial Analysis',
    description: 'Analyse financial data, figures, and economic implications.',
    icon: 'trending_up',
  },
  {
    value: 'SentimentAnalysis',
    label: 'Sentiment Analysis',
    description: 'Determine the overall tone and sentiment across the documents.',
    icon: 'mood',
  },
  {
    value: 'CustomQuestion',
    label: 'Custom Question',
    description: 'Ask a specific question and get a grounded, citation-backed answer.',
    icon: 'help_outline',
  },
];

export const EXAMPLE_QUERIES: string[] = [
  'Summarize this document',
  'Identify risks',
  'Extract action items',
  'Generate executive summary',
  'List key findings',
  'Explain important sections',
];
