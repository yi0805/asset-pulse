import { HttpErrorResponse } from '@angular/common/http';
import { mapApiError } from './api-error';

describe('mapApiError', () => {
  it('maps Problem Details fields without changing their text', () => {
    const error = mapApiError(
      new HttpErrorResponse({
        status: 404,
        error: {
          status: 404,
          title: 'Asset not found',
          detail: 'No asset exists with id 12.',
          traceId: 'trace-1',
        },
      }),
    );

    expect(error).toEqual({
      status: 404,
      title: 'Asset not found',
      detail: 'No asset exists with id 12.',
      traceId: 'trace-1',
    });
  });

  it('maps ValidationProblemDetails field errors', () => {
    const error = mapApiError(
      new HttpErrorResponse({
        status: 400,
        error: { title: 'Validation failed', errors: { page: ['Page must be positive.'] } },
      }),
    );

    expect(error.errors).toEqual({ page: ['Page must be positive.'] });
  });

  it('uses a safe fallback for unstructured HTTP failures', () => {
    const error = mapApiError(
      new HttpErrorResponse({ status: 0, error: new Error('Network unavailable') }),
    );

    expect(error).toEqual({ status: 0, title: 'Request failed' });
  });
});
