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
    // canActivate ne se rejoue pas quand on navigue entre les pages enfants :
    // canActivateChild revérifie la session à chaque changement de page du back-office.
    canActivate: [adminGuard],
    canActivateChild: [adminGuard],
    loadComponent: () => import('./admin/admin-shell/admin-shell').then((m) => m.AdminShell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'themes' },
      {
        path: 'themes',
        title: 'Thèmes – Quiz',
        loadComponent: () => import('./admin/themes/theme-list').then((m) => m.ThemeList),
      },
    ],
  },
];
