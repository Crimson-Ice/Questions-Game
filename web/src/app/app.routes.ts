import { Routes } from '@angular/router';
import { adminGuard } from './core/auth/admin-guard';

// loadComponent : chaque écran est chargé à la demande, dans son propre fichier JavaScript.
export const routes: Routes = [
  {
    path: 'admin/login',
    title: 'Connexion – Quiz',
    loadComponent: () => import('./admin/login/login').then((m) => m.Login),
  },
  {
    path: 'admin',
    title: 'Back-office – Quiz',
    canActivate: [adminGuard],
    loadComponent: () => import('./admin/admin-shell/admin-shell').then((m) => m.AdminShell),
  },
];
