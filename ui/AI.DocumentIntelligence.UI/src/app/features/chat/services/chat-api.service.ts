import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import {
  ChatMessage,
  ChatRequest,
  ChatResponse,
  ChatSession,
  ChatSessionDetail,
  ChatSessionSummary,
  ChatTurn,
  EMPTY_SESSION_ID,
} from '../models/chat.models';

@Injectable({ providedIn: 'root' })
export class ChatApiService {
  private readonly http = inject(HttpClient);
  private readonly apiBase = environment.apiBaseUrl;

  // Private writable signals
  private readonly _sessions = signal<ChatSession[]>([]);
  private readonly _activeSession = signal<ChatSession | null>(null);
  private readonly _messages = signal<ChatMessage[]>([]);
  private readonly _loading = signal(false);
  private readonly _sessionsLoading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _streamingMessageId = signal<string | null>(null);

  // Public readonly signals
  readonly sessions = this._sessions.asReadonly();
  readonly activeSession = this._activeSession.asReadonly();
  readonly messages = this._messages.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly sessionsLoading = this._sessionsLoading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly streamingMessageId = this._streamingMessageId.asReadonly();

  /**
   * Load all sessions for the current user.
   * GET /api/v1/chat/sessions → ChatSessionSummaryDto[]
   */
  loadSessions(): void {
    this._sessionsLoading.set(true);
    this._error.set(null);

    this.http.get<ChatSessionSummary[]>(`${this.apiBase}/chat/sessions`).subscribe({
      next: (summaries) => {
        this._sessions.set(summaries.map(this.summaryToSession));
        this._sessionsLoading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this._sessionsLoading.set(false);
        if (err.status === 403) {
          this._error.set('You do not have permission to view chat sessions.');
        } else {
          this._error.set('Failed to load chat sessions. Please try again.');
        }
      },
    });
  }

  /**
   * Load a single session with its full message history.
   * GET /api/v1/chat/sessions/{id} → ChatSessionDetailDto
   */
  loadSession(id: string): void {
    this._loading.set(true);
    this._error.set(null);

    this.http.get<ChatSessionDetail>(`${this.apiBase}/chat/sessions/${id}`).subscribe({
      next: (detail) => {
        const session = this._sessions().find((s) => s.id === id) ?? null;
        this._activeSession.set(session);
        this._messages.set(
          detail.messages.map((m) => ({
            id: m.id,
            sessionId: id,
            role: m.role.toLowerCase() === 'assistant' ? 'assistant' : 'user',
            content: m.content,
            citations: m.citations.map((c) => ({
              documentId: '',
              documentName: c.documentName,
              pageNumber: c.page,
              paragraphReference: c.paragraph,
              snippet: '',
              confidenceScore: c.confidence,
            })),
            createdAt: m.createdAt,
          })),
        );
        this._loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this._loading.set(false);
        if (err.status === 404) {
          this._error.set('Chat session not found.');
        } else if (err.status === 403) {
          this._error.set('You do not have permission to view this session.');
        } else {
          this._error.set('Failed to load session history. Please try again.');
        }
      },
    });
  }

  /**
   * Send a message in a chat session.
   * POST /api/v1/chat — body: { sessionId, documentIds, message, history }
   *
   * For the first message in a new conversation supply sessionId = EMPTY_SESSION_ID.
   * The response returns the real sessionId for subsequent turns.
   */
  sendMessage(content: string, documentIds: string[] = []): void {
    const session = this._activeSession();
    this._error.set(null);

    const isNewSession = session === null || session.id === EMPTY_SESSION_ID;
    const sessionId = session?.id ?? EMPTY_SESSION_ID;
    const docs = session?.documentIds ?? documentIds;

    // Build history from current messages (exclude any still-streaming placeholder)
    const history: ChatTurn[] = this._messages()
      .filter((m) => !m.isStreaming)
      .map((m) => ({
        role: m.role === 'assistant' ? 'Assistant' : 'User',
        content: m.content,
      }));

    // Optimistically append user message
    const userMessage: ChatMessage = {
      id: `msg-user-${Date.now()}`,
      sessionId,
      role: 'user',
      content,
      citations: [],
      createdAt: new Date().toISOString(),
    };
    this._messages.update((msgs) => [...msgs, userMessage]);

    // Streaming placeholder
    const placeholderId = `msg-assistant-${Date.now()}`;
    const placeholder: ChatMessage = {
      id: placeholderId,
      sessionId,
      role: 'assistant',
      content: '',
      citations: [],
      createdAt: new Date().toISOString(),
      isStreaming: true,
    };
    this._messages.update((msgs) => [...msgs, placeholder]);
    this._streamingMessageId.set(placeholderId);
    this._loading.set(true);

    const request: ChatRequest = {
      sessionId,
      documentIds: docs,
      message: content,
      history,
    };

    this.http.post<ChatResponse>(`${this.apiBase}/chat`, request).subscribe({
      next: (response) => {
        const assistantMessage: ChatMessage = {
          id: `msg-${response.sessionId}-${Date.now()}`,
          sessionId: response.sessionId,
          role: 'assistant',
          content: response.answer,
          citations: response.citations,
          createdAt: new Date().toISOString(),
          isStreaming: false,
        };

        this._messages.update((msgs) =>
          msgs.map((m) => (m.id === placeholderId ? assistantMessage : m)),
        );

        if (isNewSession) {
          // Backend assigned a real session ID — register the new session
          const newSession: ChatSession = {
            id: response.sessionId,
            title: content.slice(0, 80),
            documentIds: docs,
            status: 'InProgress',
            messageCount: 2,
            createdAt: new Date().toISOString(),
            updatedAt: new Date().toISOString(),
          };
          this._sessions.update((list) => [newSession, ...list.filter((s) => s.id !== EMPTY_SESSION_ID)]);
          this._activeSession.set(newSession);
        } else {
          // Existing session — update counts
          this._activeSession.update((s) =>
            s !== null
              ? { ...s, messageCount: s.messageCount + 2, updatedAt: new Date().toISOString() }
              : s,
          );
          this._sessions.update((list) =>
            list.map((s) =>
              s.id === response.sessionId
                ? { ...s, messageCount: s.messageCount + 2, updatedAt: new Date().toISOString() }
                : s,
            ),
          );
        }

        this._streamingMessageId.set(null);
        this._loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        // Remove streaming placeholder on error
        this._messages.update((msgs) => msgs.filter((m) => m.id !== placeholderId));
        this._streamingMessageId.set(null);
        this._loading.set(false);

        if (err.status === 403) {
          this._error.set('You do not have permission to send messages.');
        } else if (err.status === 400) {
          this._error.set('Invalid message request. Please check your inputs.');
        } else {
          this._error.set('Failed to send message. Please try again.');
        }
      },
    });
  }

  /**
   * Delete a chat session.
   * DELETE /api/v1/chat/sessions/{id}
   */
  deleteSession(id: string): void {
    this.http.delete(`${this.apiBase}/chat/sessions/${id}`).subscribe({
      next: () => {
        this.removeSessionLocally(id);
      },
      error: (err: HttpErrorResponse) => {
        if (err.status === 404) {
          // Already gone — clean up locally
          this.removeSessionLocally(id);
        } else if (err.status === 403) {
          this._error.set('You do not have permission to delete this session.');
        } else {
          this._error.set('Failed to delete session. Please try again.');
        }
      },
    });
  }

  /**
   * Start a new conversation without creating a server-side session upfront.
   * The real sessionId is returned on the first POST /chat call.
   */
  startNewConversation(documentIds: string[]): void {
    this._activeSession.set(null);
    this._messages.set([]);
    this._error.set(null);

    // Store documentIds in a transient session stub so sendMessage can pick them up
    const stub: ChatSession = {
      id: EMPTY_SESSION_ID,
      title: 'New Conversation',
      documentIds,
      status: 'Pending',
      messageCount: 0,
      createdAt: new Date().toISOString(),
      updatedAt: null,
    };
    this._activeSession.set(stub);
  }

  /** Raw HTTP call — exposed for testing and composability. */
  ask(request: ChatRequest): Observable<ChatResponse> {
    return this.http.post<ChatResponse>(`${this.apiBase}/chat`, request);
  }

  private removeSessionLocally(id: string): void {
    this._sessions.update((list) => list.filter((s) => s.id !== id));
    if (this._activeSession()?.id === id) {
      this._activeSession.set(null);
      this._messages.set([]);
    }
  }

  private summaryToSession(dto: ChatSessionSummary): ChatSession {
    return {
      id: dto.id,
      title: dto.title,
      documentIds: dto.documentIds,
      status: dto.status,
      messageCount: dto.messageCount,
      createdAt: dto.createdAt,
      updatedAt: dto.updatedAt,
    };
  }

  setError(msg: string | null): void {
    this._error.set(msg);
  }

  clearActiveSession(): void {
    this._activeSession.set(null);
    this._messages.set([]);
    this._error.set(null);
  }
}
