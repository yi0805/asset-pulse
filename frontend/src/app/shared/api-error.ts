import { HttpErrorResponse } from '@angular/common/http';

export interface ApiError {
  status: number;
  title: string;
  detail?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

interface ProblemDetails {
  status?: unknown;
  title?: unknown;
  detail?: unknown;
  traceId?: unknown;
  errors?: unknown;
}

export function mapApiError(response: HttpErrorResponse): ApiError {
  const problem = isRecord(response.error) ? (response.error as ProblemDetails) : undefined;
  const errors = toFieldErrors(problem?.errors);

  return {
    status: typeof problem?.status === 'number' ? problem.status : response.status,
    title: typeof problem?.title === 'string' ? problem.title : 'Request failed',
    ...(typeof problem?.detail === 'string' ? { detail: problem.detail } : {}),
    ...(typeof problem?.traceId === 'string' ? { traceId: problem.traceId } : {}),
    ...(errors ? { errors } : {}),
  };
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function toFieldErrors(value: unknown): Record<string, string[]> | undefined {
  if (!isRecord(value)) {
    return undefined;
  }

  const entries = Object.entries(value).filter(
    ([, messages]) =>
      Array.isArray(messages) && messages.every((message) => typeof message === 'string'),
  ) as [string, string[]][];

  return entries.length > 0 ? Object.fromEntries(entries) : undefined;
}
