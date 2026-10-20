import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../core/auth/auth';
import { Login } from './login';

describe('Login', () => {
  const auth = { login: vi.fn() };

  beforeEach(() => {
    auth.login.mockReset();
    TestBed.configureTestingModule({
      imports: [Login],
      providers: [provideRouter([]), { provide: AuthService, useValue: auth }],
    });
  });

  async function render() {
    const fixture = TestBed.createComponent(Login);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;
    const fill = (selector: string, value: string) => {
      const input = element.querySelector<HTMLInputElement>(selector)!;
      input.value = value;
      input.dispatchEvent(new Event('input'));
    };
    const submit = async () => {
      element.querySelector('form')!.dispatchEvent(new Event('submit'));
      await fixture.whenStable();
    };
    return { element, fill, submit };
  }

  it('does not call the API while the form is invalid', async () => {
    const { element, submit } = await render();

    await submit();

    expect(auth.login).not.toHaveBeenCalled();
    expect(element.textContent).toContain('Saisissez une adresse e-mail valide.');
    expect(element.textContent).toContain('Le mot de passe est obligatoire.');
  });

  it('logs in and goes to the back-office', async () => {
    auth.login.mockReturnValue(of(undefined));
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const { fill, submit } = await render();

    fill('input[type=email]', 'admin@quiz.test');
    fill('input[type=password]', 'secret');
    await submit();

    expect(auth.login).toHaveBeenCalledWith('admin@quiz.test', 'secret');
    expect(navigate).toHaveBeenCalledWith(['/admin']);
  });

  it('shows an error message when the credentials are rejected', async () => {
    auth.login.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 401 })));
    const { element, fill, submit } = await render();

    fill('input[type=email]', 'admin@quiz.test');
    fill('input[type=password]', 'mauvais');
    await submit();

    expect(element.querySelector('[role=alert]')?.textContent).toContain('Identifiants invalides.');
  });
});
