import { inject } from '@angular/core';
import { CanActivateChildFn, CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth';

/** Laisse passer vers le back-office (et ses pages enfants) seulement un administrateur connecté. */
export const adminGuard: CanActivateFn & CanActivateChildFn = () =>
  inject(AuthService).isLoggedIn() || inject(Router).createUrlTree(['/admin/login']);
