import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth';

/**
 * Ajoute le jeton aux appels vers l'API. Si l'API répond 401 (jeton expiré ou invalide),
 * déconnecte et renvoie vers l'écran de connexion. L'échec du login lui-même est laissé
 * au formulaire, qui affiche son propre message.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith('/api/')) {
    return next(request);
  }

  const auth = inject(AuthService);
  const router = inject(Router);
  const token = auth.token();
  const authorized = token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;

  return next(authorized).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401 && !request.url.startsWith('/api/auth/')) {
        auth.logout();
        void router.navigate(['/admin/login']);
      }

      return throwError(() => error);
    }),
  );
};
