import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';

import { environment } from '../../../../environments/environment';
import { ChatApiService } from './chat-api.service';
import { ChatResponse, ChatSession, ChatSessionSummary, EMPTY_SESSION_ID } from '../models/chat.models';

const API = environment.apiBaseUrl;

const makeMockSummary = (id: string): ChatSessionSummary => ({
  id,
  title: `Session ${id}`,
  documentIds: ['doc-1'],
  status: 'InProgress',
  messageCount: 2,
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
});

const makeMockResponse = (sessionId: string): ChatResponse => ({
  answer: 'This is the AI answer.',
  citations: [
    {
      documentId: 'doc-1',
      documentName: 'Test.pdf',
      pageNumber: 3,
      paragraphReference: '§2.1',
      snippet: 'Relevant excerpt.',
      confidenceScore: 0.94,
    },
  ],
  usage: {
    promptTokens: 100,
    completionTokens: 50,
    totalTokens: 150,
    estimatedCost: 0.001,
  },
  sessionId,
});

describe('ChatApiService', () => {
  let service: ChatApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), ChatApiService],
    });
    service = TestBed.inject(ChatApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should initialise signals with defaults', () => {
    expect(service.sessions()).toEqual([]);
    expect(service.activeSession()).toBeNull();
    expect(service.messages()).toEqual([]);
    expect(service.loading()).toBe(false);
    expect(service.sessionsLoading()).toBe(false);
    expect(service.error()).toBeNull();
    expect(service.streamingMessageId()).toBeNull();
  });

  // ── loadSessions ─────────────────────────────────────────────────────────────

  it('loadSessions should GET /chat/sessions and populate signal', () => {
    const summaries: ChatSessionSummary[] = [makeMockSummary('s1'), makeMockSummary('s2')];
    service.loadSessions();

    const req = httpMock.expectOne(`${API}/chat/sessions`);
    expect(req.request.method).toBe('GET');
    req.flush(summaries);

    expect(service.sessions()).toHaveSize(2);
    expect(service.sessionsLoading()).toBe(false);
    expect(service.error()).toBeNull();
  });

  it('loadSessions should set error signal (not mock data) on HTTP error', () => {
    service.loadSessions();
    const req = httpMock.expectOne(`${API}/chat/sessions`);
    req.flush('Server error', { status: 500, statusText: 'Internal Server Error' });

    // No fabricated sessions — sessions stays empty, error is set
    expect(service.sessions()).toHaveSize(0);
    expect(service.sessionsLoading()).toBe(false);
    expect(service.error()).toBeTruthy();
  });

  // ── loadSession ───────────────────────────────────────────────────────────────

  it('loadSession should GET /chat/sessions/{id} (not /chat/sessions/{id}/messages)', () => {
    service['_sessions'].set([
      {
        id: 's1', title: 'Test', documentIds: ['doc-1'],
        status: 'InProgress', messageCount: 0,
        createdAt: new Date().toISOString(), updatedAt: null,
      },
    ]);

    service.loadSession('s1');

    const req = httpMock.expectOne(`${API}/chat/sessions/s1`);
    expect(req.request.method).toBe('GET');
    req.flush({
      id: 's1',
      documentIds: ['doc-1'],
      status: 'InProgress',
      messages: [
        {
          id: 'm1',
          ordinal: 0,
          role: 'User',
          content: 'Hello',
          citations: [],
          createdAt: new Date().toISOString(),
        },
        {
          id: 'm2',
          ordinal: 1,
          role: 'Assistant',
          content: 'Hi there',
          citations: [
            { documentName: 'Test.pdf', page: 1, paragraph: '§1.1', confidence: 0.9 },
          ],
          createdAt: new Date().toISOString(),
        },
      ],
      createdAt: new Date().toISOString(),
      updatedAt: null,
    });

    expect(service.messages()).toHaveSize(2);
    expect(service.messages()[0].role).toBe('user');
    expect(service.messages()[1].role).toBe('assistant');
    expect(service.messages()[1].citations).toHaveSize(1);
    expect(service.loading()).toBe(false);
  });

  it('loadSession should set error signal on HTTP error', () => {
    service.loadSession('s1');
    const req = httpMock.expectOne(`${API}/chat/sessions/s1`);
    req.flush('Not found', { status: 404, statusText: 'Not Found' });

    expect(service.messages()).toHaveSize(0);
    expect(service.loading()).toBe(false);
    expect(service.error()).toBeTruthy();
  });

  // ── deleteSession ─────────────────────────────────────────────────────────────

  it('deleteSession should DELETE /chat/sessions/{id}', () => {
    service['_sessions'].set([
      {
        id: 's1', title: 'Test', documentIds: ['doc-1'],
        status: 'InProgress', messageCount: 0,
        createdAt: new Date().toISOString(), updatedAt: null,
      },
    ]);

    service.deleteSession('s1');
    const req = httpMock.expectOne(`${API}/chat/sessions/s1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);

    expect(service.sessions()).toHaveSize(0);
  });

  it('deleteSession should remove locally on 404 (already gone)', () => {
    service['_sessions'].set([
      {
        id: 's1', title: 'Test', documentIds: [],
        status: 'InProgress', messageCount: 0,
        createdAt: new Date().toISOString(), updatedAt: null,
      },
    ]);

    service.deleteSession('s1');
    const req = httpMock.expectOne(`${API}/chat/sessions/s1`);
    req.flush(null, { status: 404, statusText: 'Not Found' });

    expect(service.sessions()).toHaveSize(0);
  });

  it('deleteSession should set error on non-404 failure', () => {
    service['_sessions'].set([
      {
        id: 's1', title: 'Test', documentIds: [],
        status: 'InProgress', messageCount: 0,
        createdAt: new Date().toISOString(), updatedAt: null,
      },
    ]);

    service.deleteSession('s1');
    const req = httpMock.expectOne(`${API}/chat/sessions/s1`);
    req.flush('Server error', { status: 500, statusText: 'Internal Server Error' });

    // Not removed (delete failed)
    expect(service.sessions()).toHaveSize(1);
    expect(service.error()).toBeTruthy();
  });

  // ── setError / clearActiveSession ─────────────────────────────────────────────

  it('setError should update error signal', () => {
    service.setError('Something went wrong');
    expect(service.error()).toBe('Something went wrong');
    service.setError(null);
    expect(service.error()).toBeNull();
  });

  it('clearActiveSession should reset active session and messages', () => {
    service['_activeSession'].set({
      id: 's1', title: 'Test', documentIds: [],
      status: 'InProgress', messageCount: 0,
      createdAt: new Date().toISOString(), updatedAt: null,
    });
    service['_messages'].set([
      {
        id: 'm1',
        sessionId: 's1',
        role: 'user',
        content: 'Hi',
        citations: [],
        createdAt: new Date().toISOString(),
      },
    ]);
    service.clearActiveSession();
    expect(service.activeSession()).toBeNull();
    expect(service.messages()).toHaveSize(0);
  });

  // ── startNewConversation ──────────────────────────────────────────────────────

  it('startNewConversation should set active session stub with EMPTY_SESSION_ID', () => {
    service.startNewConversation(['doc-1', 'doc-2']);
    const session = service.activeSession();
    expect(session).not.toBeNull();
    expect(session!.id).toBe(EMPTY_SESSION_ID);
    expect(session!.documentIds).toEqual(['doc-1', 'doc-2']);
    expect(service.messages()).toHaveSize(0);
  });

  // ── sendMessage ───────────────────────────────────────────────────────────────

  it('sendMessage should POST to /chat (not /chat/sessions/{id}/messages)', () => {
    service.startNewConversation(['doc-1']);
    service.sendMessage('What are the payment terms?');

    const req = httpMock.expectOne(`${API}/chat`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.message).toBe('What are the payment terms?');
    expect(req.request.body.documentIds).toEqual(['doc-1']);
    expect(req.request.body.sessionId).toBe(EMPTY_SESSION_ID);

    const response = makeMockResponse('real-session-id');
    req.flush(response);

    expect(service.loading()).toBe(false);
  });

  it('sendMessage should optimistically add user and placeholder messages', () => {
    service.startNewConversation(['doc-1']);
    service.sendMessage('List all risks');

    const msgs = service.messages();
    expect(msgs).toHaveSize(2);
    expect(msgs[0].role).toBe('user');
    expect(msgs[0].content).toBe('List all risks');
    expect(msgs[1].role).toBe('assistant');
    expect(msgs[1].isStreaming).toBe(true);
    expect(service.loading()).toBe(true);
    expect(service.streamingMessageId()).not.toBeNull();

    // Cleanup pending request
    httpMock.expectOne(`${API}/chat`).flush(makeMockResponse('sid-1'));
  });

  it('sendMessage should resolve placeholder with cited AI answer on success', () => {
    service.startNewConversation(['doc-1']);
    service.sendMessage('What are the payment terms?');

    const response = makeMockResponse('sid-1');
    httpMock.expectOne(`${API}/chat`).flush(response);

    const msgs = service.messages();
    expect(msgs).toHaveSize(2);
    expect(msgs[1].content).toBe('This is the AI answer.');
    expect(msgs[1].citations).toHaveSize(1);
    expect(msgs[1].citations[0].confidenceScore).toBe(0.94);
    expect(msgs[1].citations[0].paragraphReference).toBe('§2.1');
    expect(msgs[1].isStreaming).toBe(false);
    expect(service.loading()).toBe(false);
    expect(service.streamingMessageId()).toBeNull();
  });

  it('sendMessage should assign real sessionId from response for new conversation', () => {
    service.startNewConversation(['doc-1']);
    service.sendMessage('Hello');

    httpMock.expectOne(`${API}/chat`).flush(makeMockResponse('real-id-from-backend'));

    expect(service.activeSession()?.id).toBe('real-id-from-backend');
    expect(service.sessions().some((s) => s.id === 'real-id-from-backend')).toBe(true);
  });

  it('sendMessage should include prior history in subsequent turns', () => {
    // Set up an existing session with one prior exchange
    service['_activeSession'].set({
      id: 'existing-session',
      title: 'Test',
      documentIds: ['doc-1'],
      status: 'InProgress',
      messageCount: 2,
      createdAt: new Date().toISOString(),
      updatedAt: null,
    });
    service['_messages'].set([
      {
        id: 'm-u1', sessionId: 'existing-session',
        role: 'user', content: 'First question.',
        citations: [], createdAt: new Date().toISOString(),
      },
      {
        id: 'm-a1', sessionId: 'existing-session',
        role: 'assistant', content: 'First answer.',
        citations: [], createdAt: new Date().toISOString(),
      },
    ]);

    service.sendMessage('Second question.');

    const req = httpMock.expectOne(`${API}/chat`);
    const body = req.request.body;
    expect(body.sessionId).toBe('existing-session');
    expect(body.history).toHaveSize(2);
    expect(body.history[0].role).toBe('User');
    expect(body.history[1].role).toBe('Assistant');
    req.flush(makeMockResponse('existing-session'));
  });

  it('sendMessage should remove placeholder and set error signal on HTTP error', () => {
    service.startNewConversation(['doc-1']);
    service.sendMessage('Failing question');

    const req = httpMock.expectOne(`${API}/chat`);
    req.flush('Server error', { status: 500, statusText: 'Internal Server Error' });

    // Placeholder removed — no fabricated answer
    expect(service.messages()).toHaveSize(1); // only user message remains
    expect(service.messages()[0].role).toBe('user');
    expect(service.loading()).toBe(false);
    expect(service.streamingMessageId()).toBeNull();
    expect(service.error()).toBeTruthy();
  });

  it('ask should POST to /chat and return ChatResponse observable', () => {
    const request = {
      sessionId: EMPTY_SESSION_ID,
      documentIds: ['doc-1'],
      message: 'Question',
      history: [],
    };
    service.ask(request).subscribe((response) => {
      expect(response.answer).toBe('This is the AI answer.');
      expect(response.citations).toHaveSize(1);
      expect(response.sessionId).toBeTruthy();
    });

    const req = httpMock.expectOne(`${API}/chat`);
    expect(req.request.method).toBe('POST');
    req.flush(makeMockResponse('returned-session-id'));
  });
});
