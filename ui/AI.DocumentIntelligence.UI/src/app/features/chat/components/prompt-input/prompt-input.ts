import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
  signal,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-prompt-input',
  standalone: true,
  imports: [MatButtonModule, MatIconModule],
  templateUrl: './prompt-input.html',
  styleUrl: './prompt-input.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PromptInputComponent {
  readonly disabled = input(false);
  readonly messageSend = output<string>();

  protected readonly value = signal('');

  protected send(): void {
    const content = this.value().trim();
    if (!content || this.disabled()) return;
    this.messageSend.emit(content);
    this.value.set('');
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.send();
    }
  }

  protected onInput(event: Event): void {
    this.value.set((event.target as HTMLInputElement).value);
  }

  setPrompt(text: string): void {
    this.value.set(text);
  }
}
