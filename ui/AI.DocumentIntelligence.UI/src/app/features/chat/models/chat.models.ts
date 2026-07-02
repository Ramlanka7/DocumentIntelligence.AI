import { Citation } from '../../../core/models/citation.model';

export type { Citation };

export interface ChatSession {
  id: string;
  title: string;
  documentIds: string[];
  documentNames: string[];
  createdAt: string;
  updatedAt: string;
  messageCount: number;
}

export interface ChatMessage {
  id: string;
  sessionId: string;
  role: 'user' | 'assistant';
  content: string;
  citations: Citation[];
  createdAt: string;
  isStreaming?: boolean;
}

export interface SendMessageRequest {
  sessionId: string;
  content: string;
}

export interface SendMessageResponse {
  messageId: string;
  content: string;
  citations: Citation[];
  createdAt: string;
}

export interface CreateSessionRequest {
  documentIds: string[];
  title?: string;
}

export const PROMPT_SUGGESTIONS: string[] = [
  'What are the payment terms?',
  'List all identified risks.',
  'Summarize section 4.',
  'Compare pricing between documents.',
  'Which vendor offers the best value?',
];
