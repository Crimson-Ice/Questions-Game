import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { adminGuard } from './admin-guard';
import { AuthService } from './auth';

describe('adminGuard', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient()] });
  });

  // Un guard fonctionnel utilise inject() : on l'appelle donc dans un contexte d'injection.
  const runGuard = () =>
    TestBed.runInInjectionContext(() => adminGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot));

  it('lets a logged-in administrator through', () => {
    vi.spyOn(TestBed.inject(AuthService), 'isLoggedIn').mockReturnValue(true);

    expect(runGuard()).toBe(true);
  });

  it('redirects to the login page otherwise', () => {
    vi.spyOn(TestBed.inject(AuthService), 'isLoggedIn').mockReturnValue(false);

    const result = runGuard();

    expect(result).toBeInstanceOf(UrlTree);
    expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/admin/login');
  });
});
