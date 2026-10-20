import { httpResource } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable, finalize } from 'rxjs';
import { problemMessage } from '../../core/http/problem-message';
import { SaveTheme, Theme } from './theme.model';
import { ThemesApi, themesUrl } from './themes-api';

/** Formulaire typé d'un thème : chaque champ est un FormControl<string>, jamais null. */
type ThemeForm = FormGroup<{ name: FormControl<string>; icon: FormControl<string> }>;

@Component({
  selector: 'app-theme-list',
  imports: [ReactiveFormsModule],
  templateUrl: './theme-list.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ThemeList {
  private readonly api = inject(ThemesApi);
  private readonly fb = inject(NonNullableFormBuilder);

  /** La liste, chargée dès l'ouverture de l'écran et rechargée après chaque modification. */
  protected readonly themes = httpResource<Theme[]>(() => themesUrl);

  protected readonly createForm = this.themeForm();
  protected readonly editForm = this.themeForm();
  protected readonly editingId = signal<number | null>(null);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly blockedDeletion = "Supprimez ou déplacez d'abord ses questions.";

  protected create(): void {
    this.save(this.createForm, (theme) => this.api.create(theme), () => this.createForm.reset());
  }

  protected startEdit(theme: Theme): void {
    this.error.set(null);
    this.editingId.set(theme.id);
    this.editForm.setValue({ name: theme.name, icon: theme.icon ?? '' });
  }

  protected cancelEdit(): void {
    this.editingId.set(null);
  }

  protected saveEdit(id: number): void {
    this.save(this.editForm, (theme) => this.api.update(id, theme), () => this.editingId.set(null));
  }

  protected remove(theme: Theme): void {
    if (!confirm(`Supprimer le thème « ${theme.name} » ?`)) {
      return;
    }

    this.run(this.api.delete(theme.id), () => undefined);
  }

  private themeForm(): ThemeForm {
    return this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      icon: ['', Validators.maxLength(50)],
    });
  }

  private save(
    form: ThemeForm,
    send: (theme: SaveTheme) => Observable<unknown>,
    onSuccess: () => void,
  ): void {
    if (form.invalid) {
      form.markAllAsTouched();
      return;
    }

    const { name, icon } = form.getRawValue();
    this.run(send({ name, icon: icon.trim() === '' ? null : icon }), onSuccess);
  }

  /** Envoie une modification, affiche l'erreur de l'API s'il y en a une, puis recharge la liste. */
  private run(request: Observable<unknown>, onSuccess: () => void): void {
    this.saving.set(true);
    this.error.set(null);
    request.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: () => {
        onSuccess();
        this.themes.reload();
      },
      error: (error: unknown) => this.error.set(problemMessage(error)),
    });
  }
}
