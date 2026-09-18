import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-error-state',
  template: `
    <section class="state-message state-error" role="alert">
      <p>{{ message() }}</p>
      @if (canRetry()) {
        <button type="button" (click)="retry.emit()">Try again</button>
      }
    </section>
  `,
  styleUrl: './states.css',
})
export class ErrorState {
  readonly message = input.required<string>();
  readonly canRetry = input(false);
  readonly retry = output<void>();
}
