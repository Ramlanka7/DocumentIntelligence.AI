import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';

import { ChatSession } from '../../models/chat.models';

@Component({
  selector: 'app-chat-session-list',
  standalone: true,
  imports: [DatePipe, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatTooltipModule],
  templateUrl: './chat-session-list.html',
  styleUrl: './chat-session-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChatSessionListComponent {
  readonly sessions = input.required<ChatSession[]>();
  readonly activeSessionId = input<string | null>(null);
  readonly loading = input(false);

  readonly sessionSelect = output<string>();
  readonly newSession = output<void>();
  readonly deleteSession = output<string>();

  protected select(id: string): void {
    this.sessionSelect.emit(id);
  }

  protected delete(event: MouseEvent, id: string): void {
    event.stopPropagation();
    this.deleteSession.emit(id);
  }

  protected trackSession(_index: number, session: ChatSession): string {
    return session.id;
  }
}
