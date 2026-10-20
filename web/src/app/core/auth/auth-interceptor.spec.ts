import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { AuthService } from './auth';
import { authInterceptor } from './auth-interceptor';

describe('authInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let auth: AuthService;
  let router: Router;

  beforeEach(() => {
    sessionStorage.setItem(
      'quiz.admin.session',
      JSON.stringify({ accessToken: 'jeton', expiresAt: new Date(Date.now() + 3_600_000).toISOString() }),
    );
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(withInterceptors([authInterceptor])), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
  });

  afterEach(() => {
    backend.verify();
    sessionStorage.clear();
  });

  it('adds the bearer token to API calls', () => {
    http.get('/api/admin/themes').subscribe();

    expect(backend.expectOne('/api/admin/themes').request.headers.get('Authorization')).toBe('Bearer jeton');
  });

  it('does not send the token outside the API', () => {
    http.get('/assets/config.json').subscribe();

    expect(backend.expectOne('/assets/config.json').request.headers.has('Authorization')).toBe(false);
  });

  it('never sends the token to another origin, even on an /api path', () => {
    http.get('https://exemple.test/api/x').subscribe();

    expect(backend.expectOne('https://exemple.test/api/x').request.headers.has('Authorization')).toBe(false);
  });

  it('logs out and redirects to the login page when the API answers 401', () => {
    http.get('/api/admin/themes').subscribe({ error: () => undefined });

    backend.expectOne('/api/admin/themes').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(auth.isLoggedIn()).toBe(false);
    expect(router.navigate).toHaveBeenCalledWith(['/admin/login']);
  });

  it('leaves a failed login to the login form', () => {
    http.post('/api/auth/login', {}).subscribe({ error: () => undefined });

    backend.expectOne('/api/auth/login').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(auth.isLoggedIn()).toBe(true);
    expect(router.navigate).not.toHaveBeenCalled();
  });
});
