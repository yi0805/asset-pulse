import { Component, input } from '@angular/core';

@Component({
  selector: 'app-loading-state',
  template: `<p class="state-message" role="status">{{ message() }}</p>`,
  styleUrl: './states.css',
})
export class LoadingState {
  readonly message = input('Loading…');
}
