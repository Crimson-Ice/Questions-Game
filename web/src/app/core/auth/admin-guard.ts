import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth';

/** Laisse passer vers le back-office seulement un administrateur connecté (jeton non expiré). */
export const adminGuard: CanActivateFn = () =>
  inject(AuthService).isLoggedIn() || inject(Router).createUrlTree(['/admin/login']);
