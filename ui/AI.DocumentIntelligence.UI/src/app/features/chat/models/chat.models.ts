/**
 * Chat feature models — field names mirror the backend contracts exactly:
 * Application/Contracts/Chat/{ChatRequest, ChatResponse, ChatTurn}.cs
 * Application/Contracts/AI/AiRole.cs
 * Application/Features/Chat/GetChatSessions/ChatSessionSummaryDto.cs
 * Application/Features/Chat/GetChatSession/ChatSessionDetailDto.cs
 */

/**
 * AiRole enum values from Application/Contracts/AI/AiRole.cs.
 * The backend serialises enum members as their string names (PascalCase).
 */
export type AiRole = 'System' | 'User' | 'Assistant';

/**
 * A single prior turn in a conversation — ChatTurn.cs.
 * Sent as part of the history array in the chat request.
 */
export interface ChatTurn {
  role: AiRole;
  content: string;
}

/** Citation shape from the backend — used in ChatResponse and ChatSessionDetailDto. */
export interface ChatCitation {
  documentId: string;
  documentName: string;
  pageNumber: number;
  paragraphReference: string;
  snippet: string;
  confidenceScore: number;
}

/** Token usage from Application/Contracts/AI/TokenUsage.cs */
export interface TokenUsage {
  promptTokens: number;
  completionTokens: number;
  totalTokens: number;
  estimatedCost: number;
}

/**
 * Request body for POST /api/v1/chat — ChatRequest.cs / ChatCommand.cs.
 * For a new conversation use sessionId = '00000000-0000-0000-0000-000000000000'.
 */
export interface ChatRequest {
  sessionId: string;
  documentIds: string[];
  message: string;
  history: ChatTurn[];
}

/**
 * Response from POST /api/v1/chat — ChatResponse.cs.
 * sessionId is the backend-assigned GUID to use for subsequent turns.
 */
export interface ChatResponse {
  answer: string;
  citations: ChatCitation[];
  usage: TokenUsage;
  sessionId: string;
}

/**
 * Summary view of a chat session — ChatSessionSummaryDto.cs.
 * Returned by GET /api/v1/chat/sessions.
 */
export interface ChatSessionSummary {
  id: string;
  title: string;
  documentIds: string[];
  status: string;
  messageCount: number;
  createdAt: string;
  updatedAt: string | null;
}

/** Citation on an individual chat message — ChatMessageCitationDto.cs */
export interface ChatMessageCitation {
  documentName: string;
  page: number;
  paragraph: string;
  confidence: number;
}

/** A single message in a session detail — ChatMessageDto.cs */
export interface ChatMessageDetail {
  id: string;
  ordinal: number;
  role: string;
  content: string;
  citations: ChatMessageCitation[];
  createdAt: string;
}

/**
 * Detailed view of a single chat session — ChatSessionDetailDto.cs.
 * Returned by GET /api/v1/chat/sessions/{id}.
 */
export interface ChatSessionDetail {
  id: string;
  documentIds: string[];
  status: string;
  messages: ChatMessageDetail[];
  createdAt: string;
  updatedAt: string | null;
}

/**
 * Unified chat message model used by the UI to render conversation turns.
 * Populated from both optimistic local state and from ChatSessionDetailDto messages.
 */
export interface ChatMessage {
  id: string;
  sessionId: string;
  role: 'user' | 'assistant';
  content: string;
  citations: ChatCitation[];
  createdAt: string;
  isStreaming?: boolean;
}

/**
 * Simplified session view used by the UI sidebar — built from ChatSessionSummary.
 */
export interface ChatSession {
  id: string;
  title: string;
  documentIds: string[];
  status: string;
  messageCount: number;
  createdAt: string;
  updatedAt: string | null;
}

export const PROMPT_SUGGESTIONS: string[] = [
  'What are the payment terms?',
  'List all identified risks.',
  'Summarize section 4.',
  'Compare pricing between documents.',
  'Which vendor offers the best value?',
];

/** Empty GUID used for the first message in a new conversation. */
export const EMPTY_SESSION_ID = '00000000-0000-0000-0000-000000000000';
