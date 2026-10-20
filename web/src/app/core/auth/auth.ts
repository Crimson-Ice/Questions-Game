import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, map, tap } from 'rxjs';
import { LoginResponse } from './auth.model';

const storageKey = 'quiz.admin.session';

/**
 * Session de l'administrateur : le jeton JWT et sa date d'expiration, gardés dans
 * sessionStorage (effacés à la fermeture de l'onglet).
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly session = signal<LoginResponse | null>(readStoredSession());

  /**
   * Jeton à envoyer à l'API, ou null si personne n'est connecté ou si le jeton a expiré.
   * C'est une méthode et non un computed : l'expiration dépend de l'heure, qu'un computed
   * ne surveille pas (il ne se recalcule que quand la session change).
   */
  token(): string | null {
    const session = this.session();
    return session !== null && Date.parse(session.expiresAt) > Date.now() ? session.accessToken : null;
  }

  /** Vrai si un jeton est présent et pas encore expiré. */
  isLoggedIn(): boolean {
    return this.token() !== null;
  }

  login(email: string, password: string): Observable<void> {
    return this.http.post<LoginResponse>('/api/auth/login', { email, password }).pipe(
      tap((session) => {
        sessionStorage.setItem(storageKey, JSON.stringify(session));
        this.session.set(session);
      }),
      map(() => undefined),
    );
  }

  logout(): void {
    sessionStorage.removeItem(storageKey);
    this.session.set(null);
  }
}

function readStoredSession(): LoginResponse | null {
  const stored = sessionStorage.getItem(storageKey);
  if (stored === null) {
    return null;
  }

  try {
    const value: unknown = JSON.parse(stored);
    return isLoginResponse(value) ? value : null;
  } catch {
    return null;
  }
}

/** sessionStorage peut contenir n'importe quoi : on vérifie la forme au lieu de la supposer. */
function isLoginResponse(value: unknown): value is LoginResponse {
  if (typeof value !== 'object' || value === null) {
    return false;
  }

  const { accessToken, expiresAt } = value as Record<string, unknown>;
  return typeof accessToken === 'string' && typeof expiresAt === 'string';
}
