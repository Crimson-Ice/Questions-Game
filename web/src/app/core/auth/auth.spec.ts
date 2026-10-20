import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth';
import { LoginResponse } from './auth.model';

const storageKey = 'quiz.admin.session';
const inOneHour = () => new Date(Date.now() + 3_600_000).toISOString();

describe('AuthService', () => {
  beforeEach(() => sessionStorage.clear());

  function setup(): { auth: AuthService; http: HttpTestingController } {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    return { auth: TestBed.inject(AuthService), http: TestBed.inject(HttpTestingController) };
  }

  it('is logged out when nothing is stored', () => {
    const { auth } = setup();

    expect(auth.isLoggedIn()).toBe(false);
    expect(auth.token()).toBeNull();
  });

  it('stores the session after a successful login', () => {
    const { auth, http } = setup();
    const session: LoginResponse = { accessToken: 'jeton', expiresAt: inOneHour() };

    auth.login('admin@quiz.test', 'secret').subscribe();
    const request = http.expectOne('/api/auth/login');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ email: 'admin@quiz.test', password: 'secret' });
    request.flush(session);

    expect(auth.isLoggedIn()).toBe(true);
    expect(auth.token()).toBe('jeton');
    expect(JSON.parse(sessionStorage.getItem(storageKey)!)).toEqual(session);
    http.verify();
  });

  it('restores a stored session when the app starts', () => {
    sessionStorage.setItem(storageKey, JSON.stringify({ accessToken: 'jeton', expiresAt: inOneHour() }));

    const { auth } = setup();

    expect(auth.isLoggedIn()).toBe(true);
  });

  it('considers an expired session as logged out', () => {
    const expired = new Date(Date.now() - 1000).toISOString();
    sessionStorage.setItem(storageKey, JSON.stringify({ accessToken: 'jeton', expiresAt: expired }));

    const { auth } = setup();

    expect(auth.isLoggedIn()).toBe(false);
    expect(auth.token()).toBeNull();
  });

  it('ignores a stored session with an unexpected shape', () => {
    sessionStorage.setItem(storageKey, JSON.stringify({ accessToken: 'jeton' }));

    const { auth } = setup();

    expect(auth.isLoggedIn()).toBe(false);
    expect(auth.token()).toBeNull();
  });

  it('ignores a corrupted stored session', () => {
    sessionStorage.setItem(storageKey, '{pas du json');

    const { auth } = setup();

    expect(auth.isLoggedIn()).toBe(false);
  });

  it('clears the session on logout', () => {
    sessionStorage.setItem(storageKey, JSON.stringify({ accessToken: 'jeton', expiresAt: inOneHour() }));
    const { auth } = setup();

    auth.logout();

    expect(auth.isLoggedIn()).toBe(false);
    expect(sessionStorage.getItem(storageKey)).toBeNull();
  });
});
