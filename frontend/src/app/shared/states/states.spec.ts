import { TestBed } from '@angular/core/testing';
import { EmptyState } from './empty-state';
import { ErrorState } from './error-state';
import { LoadingState } from './loading-state';

describe('shared state components', () => {
  it('renders loading feedback with a status role', () => {
    const fixture = TestBed.createComponent(LoadingState);
    fixture.componentRef.setInput('message', 'Loading assets…');
    fixture.detectChanges();

    expect(
      (fixture.nativeElement as HTMLElement).querySelector('[role="status"]')?.textContent,
    ).toContain('Loading assets…');
  });

  it('renders the supplied empty message', () => {
    const fixture = TestBed.createComponent(EmptyState);
    fixture.componentRef.setInput('message', 'No matching assets.');
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No matching assets.');
  });

  it('renders an error and emits retry only when activated', () => {
    const fixture = TestBed.createComponent(ErrorState);
    const retry = vi.fn();
    fixture.componentInstance.retry.subscribe(retry);
    fixture.componentRef.setInput('message', 'The alarms could not be loaded.');
    fixture.componentRef.setInput('canRetry', true);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('[role="alert"]')?.textContent).toContain(
      'The alarms could not be loaded.',
    );
    element.querySelector<HTMLButtonElement>('button')?.click();

    expect(retry).toHaveBeenCalledTimes(1);
  });
});
