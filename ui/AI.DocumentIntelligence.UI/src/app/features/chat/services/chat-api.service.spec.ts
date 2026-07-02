import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';

import { environment } from '../../../../environments/environment';
import { ChatApiService } from './chat-api.service';
import { ChatSession, ChatMessage } from '../models/chat.models';

const API = environment.apiBaseUrl;

const makeMockSession = (id: string): ChatSession => ({
  id,
  title: `Session ${id}`,
  documentIds: ['doc-1'],
  documentNames: ['Test.pdf'],
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  messageCount: 0,
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

  it('loadSessions should GET /chat/sessions and populate signal', () => {
    const mockSessions: ChatSession[] = [makeMockSession('s1'), makeMockSession('s2')];
    service.loadSessions();

    const req = httpMock.expectOne(`${API}/chat/sessions`);
    expect(req.request.method).toBe('GET');
    req.flush(mockSessions);

    expect(service.sessions()).toHaveSize(2);
    expect(service.sessionsLoading()).toBe(false);
  });

  it('loadSessions should fall back to mock data on HTTP error', () => {
    service.loadSessions();
    const req = httpMock.expectOne(`${API}/chat/sessions`);
    req.error(new ProgressEvent('network error'));

    expect(service.sessions().length).toBeGreaterThan(0);
    expect(service.sessionsLoading()).toBe(false);
  });

  it('createSession should POST /chat/sessions', () => {
    service.createSession({ documentIds: ['doc-1'], title: 'My Session' });

    const req = httpMock.expectOne(`${API}/chat/sessions`);
    expect(req.request.method).toBe('POST');

    const newSession = makeMockSession('s-new');
    req.flush(newSession);

    expect(service.sessions()).toContain(newSession);
    expect(service.activeSession()).toEqual(newSession);
  });

  it('createSession should fall back to mock session on HTTP error', () => {
    service.createSession({ documentIds: ['doc-1'], title: 'Fallback Session' });
    const req = httpMock.expectOne(`${API}/chat/sessions`);
    req.error(new ProgressEvent('network error'));

    expect(service.sessions().length).toBe(1);
    expect(service.activeSession()).not.toBeNull();
    expect(service.activeSession()?.title).toBe('Fallback Session');
  });

  it('loadSession should GET /chat/sessions/{id}/messages', () => {
    // Pre-load sessions so activeSession lookup works
    const s1 = makeMockSession('s1');
    service['_sessions'].set([s1]);

    service.loadSession('s1');

    const req = httpMock.expectOne(`${API}/chat/sessions/s1/messages`);
    expect(req.request.method).toBe('GET');

    const messages: ChatMessage[] = [
      {
        id: 'm1',
        sessionId: 's1',
        role: 'user',
        content: 'Hello',
        citations: [],
        createdAt: new Date().toISOString(),
      },
    ];
    req.flush(messages);

    expect(service.messages()).toHaveSize(1);
    expect(service.loading()).toBe(false);
  });

  it('deleteSession should DELETE /chat/sessions/{id}', () => {
    const s1 = makeMockSession('s1');
    service['_sessions'].set([s1]);

    service.deleteSession('s1');
    const req = httpMock.expectOne(`${API}/chat/sessions/s1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);

    expect(service.sessions()).toHaveSize(0);
  });

  it('deleteSession should remove session locally on HTTP error', () => {
    const s1 = makeMockSession('s1');
    service['_sessions'].set([s1]);

    service.deleteSession('s1');
    const req = httpMock.expectOne(`${API}/chat/sessions/s1`);
    req.error(new ProgressEvent('network error'));

    expect(service.sessions()).toHaveSize(0);
  });

  it('setError should update error signal', () => {
    service.setError('Something went wrong');
    expect(service.error()).toBe('Something went wrong');
    service.setError(null);
    expect(service.error()).toBeNull();
  });

  it('clearActiveSession should reset active session and messages', () => {
    service['_activeSession'].set(makeMockSession('s1'));
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

  describe('sendMessage', () => {
    function setupActiveSession(): string {
      service.createSession({ documentIds: ['doc-1'], title: 'Test Session' });
      httpMock.expectOne(`${API}/chat/sessions`).error(new ProgressEvent('error'));
      return service.activeSession()!.id;
    }

    it('should optimistically add user and streaming-placeholder messages', () => {
      const id = setupActiveSession();
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
      httpMock.expectOne(`${API}/chat/sessions/${id}/messages`).flush({
        messageId: 'ai-1',
        content: 'Here are the risks...',
        citations: [],
        createdAt: new Date().toISOString(),
      });
    });

    it('should resolve assistant message with citations on HTTP success', () => {
      const id = setupActiveSession();
      service.sendMessage('What are the payment terms?');

      httpMock.expectOne(`${API}/chat/sessions/${id}/messages`).flush({
        messageId: 'ai-resp-1',
        content: 'Payment is due net-30.',
        citations: [
          { documentName: 'Test.pdf', pageNumber: 4, paragraphRef: '§5.2', confidenceScore: 0.96 },
        ],
        createdAt: new Date().toISOString(),
      });

      const msgs = service.messages();
      expect(msgs[1].id).toBe('ai-resp-1');
      expect(msgs[1].content).toBe('Payment is due net-30.');
      expect(msgs[1].citations).toHaveSize(1);
      expect(msgs[1].citations[0].confidenceScore).toBe(0.96);
      expect(msgs[1].isStreaming).toBe(false);
      expect(service.loading()).toBe(false);
      expect(service.streamingMessageId()).toBeNull();
    });

    it('should fall back to cited mock answer after HTTP error', fakeAsync(() => {
      const id = setupActiveSession();
      service.sendMessage('What are the risks?');

      httpMock.expectOne(`${API}/chat/sessions/${id}/messages`).error(new ProgressEvent('error'));

      tick(1500); // advance RxJS delay(1500) in mock fallback

      const msgs = service.messages();
      expect(msgs).toHaveSize(2);
      expect(msgs[1].role).toBe('assistant');
      expect(msgs[1].content.length).toBeGreaterThan(0);
      expect(msgs[1].citations.length).toBeGreaterThan(0);
      expect(msgs[1].isStreaming).toBe(false);
      expect(service.loading()).toBe(false);
      expect(service.streamingMessageId()).toBeNull();
    }));
  });
});
