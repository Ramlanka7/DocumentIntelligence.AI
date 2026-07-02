import {
  ChangeDetectionStrategy,
  Component,
  inject,
  OnInit,
  signal,
  ViewChild,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';

import { ChatApiService } from './services/chat-api.service';
import { ChatSessionListComponent } from './components/chat-session-list/chat-session-list';
import { MessageListComponent } from './components/message-list/message-list';
import { PromptInputComponent } from './components/prompt-input/prompt-input';
import { PromptSuggestionsComponent } from './components/prompt-suggestions/prompt-suggestions';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    ChatSessionListComponent,
    MessageListComponent,
    PromptInputComponent,
    PromptSuggestionsComponent,
  ],
  templateUrl: './chat.html',
  styleUrl: './chat.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChatComponent implements OnInit {
  protected readonly service = inject(ChatApiService);

  protected readonly showNewSessionForm = signal(false);
  protected readonly newSessionTitle = signal('');
  protected readonly newSessionDocs = signal('');
  protected readonly sidebarOpen = signal(true);

  @ViewChild(PromptInputComponent) private promptInput?: PromptInputComponent;

  ngOnInit(): void {
    this.service.loadSessions();
  }

  protected selectSession(id: string): void {
    this.service.loadSession(id);
  }

  protected openNewSessionForm(): void {
    this.showNewSessionForm.set(true);
    this.newSessionTitle.set('');
    this.newSessionDocs.set('');
  }

  protected cancelNewSession(): void {
    this.showNewSessionForm.set(false);
  }

  protected createSession(): void {
    const title = this.newSessionTitle().trim();
    const docs = this.newSessionDocs()
      .split(',')
      .map((d) => d.trim())
      .filter(Boolean);

    this.service.createSession({
      documentIds: docs.length ? docs : ['default'],
      title: title || undefined,
    });
    this.showNewSessionForm.set(false);
  }

  protected sendMessage(content: string): void {
    this.service.sendMessage(content);
  }

  protected onSuggestionSelect(suggestion: string): void {
    this.promptInput?.setPrompt(suggestion);
  }

  protected deleteSession(id: string): void {
    this.service.deleteSession(id);
  }

  protected toggleSidebar(): void {
    this.sidebarOpen.update((v) => !v);
  }

  protected dismissError(): void {
    this.service.setError(null);
  }

  protected onTitleInput(event: Event): void {
    this.newSessionTitle.set((event.target as HTMLInputElement).value);
  }

  protected onDocsInput(event: Event): void {
    this.newSessionDocs.set((event.target as HTMLInputElement).value);
  }
}
