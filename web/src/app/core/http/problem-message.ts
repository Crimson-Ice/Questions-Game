import { HttpErrorResponse } from '@angular/common/http';

const fallback = 'Une erreur est survenue. Réessayez.';

/**
 * Message lisible à partir d'une erreur de l'API, qui répond en ProblemDetails :
 * les erreurs de validation (400) dans `errors`, les autres (409…) dans `detail` ou `title`.
 */
export function problemMessage(error: unknown): string {
  if (!(error instanceof HttpErrorResponse) || typeof error.error !== 'object' || error.error === null) {
    return fallback;
  }

  const problem = error.error as { title?: unknown; detail?: unknown; errors?: unknown };
  if (typeof problem.errors === 'object' && problem.errors !== null) {
    const messages = Object.values(problem.errors).flat().filter((m): m is string => typeof m === 'string');
    if (messages.length > 0) {
      return messages.join(' ');
    }
  }

  if (typeof problem.detail === 'string') {
    return problem.detail;
  }

  return typeof problem.title === 'string' ? problem.title : fallback;
}
