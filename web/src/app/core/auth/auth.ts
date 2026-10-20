import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
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

  /** Jeton à envoyer à l'API, ou null si personne n'est connecté. */
  readonly token = computed(() => this.session()?.accessToken ?? null);

  /** Vrai si un jeton est présent et pas encore expiré. */
  isLoggedIn(): boolean {
    const session = this.session();
    return session !== null && Date.parse(session.expiresAt) > Date.now();
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
    return JSON.parse(stored) as LoginResponse;
  } catch {
    return null;
  }
}
