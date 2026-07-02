import { ChangeDetectionStrategy, Component, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import { PROMPT_SUGGESTIONS } from '../../models/chat.models';

@Component({
  selector: 'app-prompt-suggestions',
  standalone: true,
  imports: [MatButtonModule, MatIconModule],
  templateUrl: './prompt-suggestions.html',
  styleUrl: './prompt-suggestions.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PromptSuggestionsComponent {
  readonly suggestionSelect = output<string>();

  protected readonly suggestions = PROMPT_SUGGESTIONS;

  protected select(suggestion: string): void {
    this.suggestionSelect.emit(suggestion);
  }

  protected trackSuggestion(index: number, _s: string): number {
    return index;
  }
}
