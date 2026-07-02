import {
  ChangeDetectionStrategy,
  Component,
  effect,
  ElementRef,
  input,
  ViewChild,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { ChatMessage, Citation } from '../../models/chat.models';
import { CitationChipComponent } from '../citation-chip/citation-chip';

@Component({
  selector: 'app-message-list',
  standalone: true,
  imports: [DatePipe, MatIconModule, MatProgressSpinnerModule, CitationChipComponent],
  templateUrl: './message-list.html',
  styleUrl: './message-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MessageListComponent {
  readonly messages = input.required<ChatMessage[]>();
  readonly streamingMessageId = input<string | null>(null);

  @ViewChild('scrollAnchor') private scrollAnchor!: ElementRef<HTMLDivElement>;

  constructor() {
    effect(() => {
      this.messages(); // subscribe to changes
      queueMicrotask(() => {
        this.scrollAnchor?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'end' });
      });
    });
  }

  protected trackMessage(_index: number, message: ChatMessage): string {
    return message.id;
  }

  protected trackCitation(_index: number, citation: Citation): string {
    return `${citation.documentName}-${citation.pageNumber}-${citation.paragraphRef}`;
  }
}
