import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { of, delay } from 'rxjs';

import { environment } from '../../../../environments/environment';
import {
  ChatSession,
  ChatMessage,
  CreateSessionRequest,
  SendMessageResponse,
  Citation,
} from '../models/chat.models';

// ── Mock data ──────────────────────────────────────────────────────────────────

const MOCK_SESSIONS: ChatSession[] = [
  {
    id: 'session-001',
    title: 'Vendor Contract Review Q4',
    documentIds: ['doc-001', 'doc-002'],
    documentNames: ['Vendor Agreement A.pdf', 'Vendor Agreement B.pdf'],
    createdAt: new Date(Date.now() - 86400000 * 2).toISOString(),
    updatedAt: new Date(Date.now() - 3600000).toISOString(),
    messageCount: 8,
  },
  {
    id: 'session-002',
    title: 'Policy Compliance Analysis',
    documentIds: ['doc-003'],
    documentNames: ['IT Security Policy 2024.pdf'],
    createdAt: new Date(Date.now() - 86400000 * 5).toISOString(),
    updatedAt: new Date(Date.now() - 86400000).toISOString(),
    messageCount: 4,
  },
  {
    id: 'session-003',
    title: 'Procurement RFP Evaluation',
    documentIds: ['doc-004', 'doc-005', 'doc-006'],
    documentNames: ['RFP Response Alpha.pdf', 'RFP Response Beta.pdf', 'RFP Response Gamma.pdf'],
    createdAt: new Date(Date.now() - 86400000 * 10).toISOString(),
    updatedAt: new Date(Date.now() - 86400000 * 2).toISOString(),
    messageCount: 12,
  },
];

function buildMockAnswer(question: string, session: ChatSession): SendMessageResponse {
  const q = question.toLowerCase();
  const docA = session.documentNames[0] ?? 'Document A.pdf';
  const docB = session.documentNames[1] ?? 'Document B.pdf';

  const baseCitations: Citation[] = [
    {
      documentName: docA,
      pageNumber: 3,
      paragraphRef: '§2.1',
      confidenceScore: 0.94,
    },
    {
      documentName: docB,
      pageNumber: 5,
      paragraphRef: '§3.4',
      confidenceScore: 0.87,
    },
  ];

  if (q.includes('payment')) {
    return {
      messageId: `msg-${Date.now()}`,
      content:
        'The payment terms outlined in the documents specify net-30 conditions with a 2% early payment discount for settlement within 10 days. Late payments are subject to a 1.5% monthly interest charge. Document A specifies payment by wire transfer only, while Document B allows ACH and check payments. Both documents require a valid purchase order number on all invoices.',
      citations: [
        { documentName: docA, pageNumber: 4, paragraphRef: '§5.2', confidenceScore: 0.96 },
        { documentName: docB, pageNumber: 6, paragraphRef: '§5.1', confidenceScore: 0.91 },
      ],
      createdAt: new Date().toISOString(),
    };
  }

  if (q.includes('risk')) {
    return {
      messageId: `msg-${Date.now()}`,
      content:
        'The identified risks across the documents include: (1) Force majeure clauses that may be triggered by supply chain disruptions; (2) Indemnification obligations that could expose the company to uncapped liability; (3) IP ownership ambiguities in co-development scenarios; (4) Data processing terms that may conflict with GDPR Article 28 requirements. The liability cap of $1M in Document B is double that of Document A ($500K), representing increased financial exposure.',
      citations: [
        { documentName: docA, pageNumber: 9, paragraphRef: '§8.3', confidenceScore: 0.89 },
        { documentName: docB, pageNumber: 11, paragraphRef: '§9.1', confidenceScore: 0.92 },
      ],
      createdAt: new Date().toISOString(),
    };
  }

  if (q.includes('summar') || q.includes('section 4')) {
    return {
      messageId: `msg-${Date.now()}`,
      content:
        'Section 4 covers the Scope of Services and Deliverables. It defines the primary deliverables as monthly status reports, quarterly business reviews, and an annual performance audit. The section also specifies service level agreements (SLAs) with 99.5% uptime guarantees and a maximum 4-hour response time for critical incidents. Penalties for SLA breaches are calculated at 2% of monthly fees per percentage point below the target.',
      citations: [
        { documentName: docA, pageNumber: 7, paragraphRef: '§4.1', confidenceScore: 0.98 },
        { documentName: docA, pageNumber: 8, paragraphRef: '§4.3', confidenceScore: 0.95 },
      ],
      createdAt: new Date().toISOString(),
    };
  }

  if (q.includes('pric') || q.includes('compar')) {
    return {
      messageId: `msg-${Date.now()}`,
      content:
        'Pricing comparison across documents: Document A proposes a fixed monthly retainer of $15,000 with additional T&M rates of $250/hr for out-of-scope work. Document B offers a tiered model starting at $12,000/month with volume discounts kicking in above 1,000 hours annually. Over a 3-year contract horizon, Document B is approximately 18% more cost-effective assuming current usage patterns. However, Document B includes fewer bundled services in the base tier.',
      citations: [
        { documentName: docA, pageNumber: 2, paragraphRef: '§3.1', confidenceScore: 0.97 },
        { documentName: docB, pageNumber: 3, paragraphRef: '§3.2', confidenceScore: 0.93 },
      ],
      createdAt: new Date().toISOString(),
    };
  }

  if (q.includes('vendor') || q.includes('value') || q.includes('best')) {
    return {
      messageId: `msg-${Date.now()}`,
      content:
        'Based on a weighted value analysis across price, service breadth, SLA commitments, and contractual risk, Vendor B (represented in Document B) offers the best overall value. While the base price is slightly lower and volume discounts are more aggressive, the key differentiators are a broader service catalog, stronger data security guarantees (ISO 27001 certified), and a 99.9% uptime SLA vs 99.5% for Vendor A. The main trade-off is a longer minimum contract term (24 months vs 12 months).',
      citations: [
        { documentName: docA, pageNumber: 1, paragraphRef: '§1.2', confidenceScore: 0.88 },
        { documentName: docB, pageNumber: 2, paragraphRef: '§1.3', confidenceScore: 0.91 },
      ],
      createdAt: new Date().toISOString(),
    };
  }

  // Generic fallback answer
  return {
    messageId: `msg-${Date.now()}`,
    content:
      `Based on the documents in this session, I've analyzed the content to answer your question. The documents cover contractual terms, service obligations, and compliance requirements. Key sections relevant to your query are found throughout the documents. For more specific information, please refine your question or refer to the cited passages below.`,
    citations: baseCitations,
    createdAt: new Date().toISOString(),
  };
}

// ── Service ────────────────────────────────────────────────────────────────────

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

  loadSessions(): void {
    this._sessionsLoading.set(true);
    this._error.set(null);

    this.http.get<ChatSession[]>(`${this.apiBase}/chat/sessions`).subscribe({
      next: (sessions) => {
        this._sessions.set(sessions);
        this._sessionsLoading.set(false);
      },
      error: () => {
        // Backend unavailable — use mock sessions
        this._sessions.set([...MOCK_SESSIONS]);
        this._sessionsLoading.set(false);
      },
    });
  }

  loadSession(id: string): void {
    this._loading.set(true);
    this._error.set(null);

    const session = this._sessions().find((s) => s.id === id) ?? null;
    this._activeSession.set(session);

    this.http.get<ChatMessage[]>(`${this.apiBase}/chat/sessions/${id}/messages`).subscribe({
      next: (messages) => {
        this._messages.set(messages);
        this._loading.set(false);
      },
      error: () => {
        // Mock fallback: return empty history
        this._messages.set([]);
        this._loading.set(false);
      },
    });
  }

  createSession(req: CreateSessionRequest): void {
    this._sessionsLoading.set(true);
    this._error.set(null);

    this.http.post<ChatSession>(`${this.apiBase}/chat/sessions`, req).subscribe({
      next: (session) => {
        this._sessions.update((list) => [session, ...list]);
        this._activeSession.set(session);
        this._messages.set([]);
        this._sessionsLoading.set(false);
      },
      error: () => {
        // Mock fallback: create local session
        const mockSession: ChatSession = {
          id: `session-${Date.now()}`,
          title: req.title ?? 'New Chat Session',
          documentIds: req.documentIds,
          documentNames: req.documentIds.map((id, i) => `Document ${i + 1}.pdf`),
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          messageCount: 0,
        };
        this._sessions.update((list) => [mockSession, ...list]);
        this._activeSession.set(mockSession);
        this._messages.set([]);
        this._sessionsLoading.set(false);
      },
    });
  }

  sendMessage(content: string): void {
    const session = this._activeSession();
    if (!session) return;

    this._error.set(null);

    // Optimistically append user message
    const userMessage: ChatMessage = {
      id: `msg-user-${Date.now()}`,
      sessionId: session.id,
      role: 'user',
      content,
      citations: [],
      createdAt: new Date().toISOString(),
    };
    this._messages.update((msgs) => [...msgs, userMessage]);

    // Add a placeholder streaming assistant message
    const placeholderId = `msg-assistant-${Date.now()}`;
    const placeholder: ChatMessage = {
      id: placeholderId,
      sessionId: session.id,
      role: 'assistant',
      content: '',
      citations: [],
      createdAt: new Date().toISOString(),
      isStreaming: true,
    };
    this._messages.update((msgs) => [...msgs, placeholder]);
    this._streamingMessageId.set(placeholderId);
    this._loading.set(true);

    this.http
      .post<SendMessageResponse>(`${this.apiBase}/chat/sessions/${session.id}/messages`, {
        sessionId: session.id,
        content,
      })
      .subscribe({
        next: (response) => {
          this.resolveAssistantMessage(placeholderId, session.id, response);
        },
        error: () => {
          // Mock fallback with realistic delay
          of(buildMockAnswer(content, session))
            .pipe(delay(1500))
            .subscribe((mockResponse) => {
              this.resolveAssistantMessage(placeholderId, session.id, mockResponse);
            });
        },
      });
  }

  private resolveAssistantMessage(
    placeholderId: string,
    sessionId: string,
    response: SendMessageResponse,
  ): void {
    const assistantMessage: ChatMessage = {
      id: response.messageId,
      sessionId,
      role: 'assistant',
      content: response.content,
      citations: response.citations,
      createdAt: response.createdAt,
      isStreaming: false,
    };

    this._messages.update((msgs) =>
      msgs.map((m) => (m.id === placeholderId ? assistantMessage : m)),
    );

    // Update session message count
    this._activeSession.update((s) =>
      s ? { ...s, messageCount: s.messageCount + 2, updatedAt: new Date().toISOString() } : s,
    );
    this._sessions.update((list) =>
      list.map((s) =>
        s.id === sessionId
          ? { ...s, messageCount: s.messageCount + 2, updatedAt: new Date().toISOString() }
          : s,
      ),
    );

    this._streamingMessageId.set(null);
    this._loading.set(false);
  }

  deleteSession(id: string): void {
    this.http.delete(`${this.apiBase}/chat/sessions/${id}`).subscribe({
      next: () => {
        this.removeSessionLocally(id);
      },
      error: () => {
        // Mock fallback: remove from local signal
        this.removeSessionLocally(id);
      },
    });
  }

  private removeSessionLocally(id: string): void {
    this._sessions.update((list) => list.filter((s) => s.id !== id));
    if (this._activeSession()?.id === id) {
      this._activeSession.set(null);
      this._messages.set([]);
    }
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
