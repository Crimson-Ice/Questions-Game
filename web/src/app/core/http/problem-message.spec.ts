import { HttpErrorResponse } from '@angular/common/http';
import { problemMessage } from './problem-message';

const httpError = (status: number, body: unknown) => new HttpErrorResponse({ status, error: body });

describe('problemMessage', () => {
  it('joins validation messages', () => {
    const error = httpError(400, { title: 'Validation', errors: { Name: ['Le nom est obligatoire.'], Icon: ['Trop long.'] } });

    expect(problemMessage(error)).toBe('Le nom est obligatoire. Trop long.');
  });

  it('prefers the detail of a problem', () => {
    const error = httpError(409, { title: 'Ce nom de thème est déjà utilisé.', detail: 'Un thème nommé « Sport » existe déjà.' });

    expect(problemMessage(error)).toBe('Un thème nommé « Sport » existe déjà.');
  });

  it('falls back to the title', () => {
    expect(problemMessage(httpError(409, { title: 'Conflit.' }))).toBe('Conflit.');
  });

  it('returns a generic message for anything else', () => {
    expect(problemMessage(httpError(500, null))).toBe('Une erreur est survenue. Réessayez.');
    expect(problemMessage(new Error('boom'))).toBe('Une erreur est survenue. Réessayez.');
  });
});
