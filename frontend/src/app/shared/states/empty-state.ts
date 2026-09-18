import { Component, input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  template: `<p class="state-message" role="status">{{ message() }}</p>`,
  styleUrl: './states.css',
})
export class EmptyState {
  readonly message = input.required<string>();
}
