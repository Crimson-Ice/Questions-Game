import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  templateUrl: './login.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  /** Formulaire typé : NonNullableFormBuilder donne des champs string (jamais null) après reset. */
  protected readonly form = inject(NonNullableFormBuilder).group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { email, password } = this.form.getRawValue();
    this.submitting.set(true);
    this.error.set(null);

    this.auth.login(email, password).subscribe({
      next: () => void this.router.navigate(['/admin']),
      error: (error: unknown) => {
        this.submitting.set(false);
        this.error.set(
          error instanceof HttpErrorResponse && error.status === 401
            ? 'Identifiants invalides.'
            : 'Connexion impossible pour le moment. Réessayez.',
        );
      },
    });
  }
}
