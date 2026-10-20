import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ThemeList } from './theme-list';
import { Theme } from './theme.model';

const cinema: Theme = { id: 1, name: 'Cinéma', icon: '🎬', questionCount: 30 };
const empty: Theme = { id: 2, name: 'Vide', icon: null, questionCount: 0 };

describe('ThemeList when the list cannot be loaded', () => {
  it('shows a loading error', async () => {
    TestBed.configureTestingModule({
      imports: [ThemeList],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const fixture = TestBed.createComponent(ThemeList);
    TestBed.tick();

    TestBed.inject(HttpTestingController)
      .expectOne('/api/admin/themes')
      .flush(null, { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Impossible de charger les thèmes.');
  });
});

describe('ThemeList', () => {
  let fixture: ComponentFixture<ThemeList>;
  let backend: HttpTestingController;
  let element: HTMLElement;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [ThemeList],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    backend = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ThemeList);
    element = fixture.nativeElement as HTMLElement;
    await respondWithThemes([cinema, empty]);
  });

  afterEach(() => {
    backend.verify();
    vi.restoreAllMocks();
  });

  /**
   * Répond à la requête de chargement (ou de rechargement) de la liste. TestBed.tick() exécute
   * le httpResource, qui envoie alors sa requête ; on ne peut pas attendre whenStable() avant,
   * car Angular considère l'application instable tant qu'une requête est en cours.
   */
  async function respondWithThemes(themes: Theme[]): Promise<void> {
    TestBed.tick();
    backend.expectOne({ method: 'GET', url: '/api/admin/themes' }).flush(themes);
    await fixture.whenStable();
  }

  function rows(): HTMLElement[] {
    return Array.from(element.querySelectorAll<HTMLElement>('[data-testid=theme-row]'));
  }

  function button(row: HTMLElement, label: string): HTMLButtonElement {
    return Array.from(row.querySelectorAll('button')).find((b) => b.textContent?.trim() === label)!;
  }

  function type(input: HTMLInputElement, value: string): void {
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  it('lists the themes with their question count', () => {
    const cells = rows().map((row) =>
      Array.from(row.querySelectorAll('td'))
        .slice(0, 3)
        .map((cell) => cell.textContent?.trim()),
    );
    expect(cells).toEqual([
      ['🎬', 'Cinéma', '30'],
      ['', 'Vide', '0'],
    ]);
  });

  it('disables deletion of a theme that still has questions', () => {
    const [cinemaRow, emptyRow] = rows();

    expect(button(cinemaRow, 'Supprimer').disabled).toBe(true);
    expect(button(cinemaRow, 'Supprimer').title).toContain('déplacez');
    expect(button(emptyRow, 'Supprimer').disabled).toBe(false);
  });

  it('creates a theme then reloads the list', async () => {
    const [name, icon] = Array.from(element.querySelectorAll<HTMLInputElement>('form input'));
    type(name, 'Sport');
    type(icon, '   ');
    element.querySelector('form')!.dispatchEvent(new Event('submit'));

    const request = backend.expectOne({ method: 'POST', url: '/api/admin/themes' });
    expect(request.request.body).toEqual({ name: 'Sport', icon: null });
    request.flush({ id: 3, name: 'Sport', icon: null, questionCount: 0 });

    await respondWithThemes([cinema, empty, { id: 3, name: 'Sport', icon: null, questionCount: 0 }]);
    expect(rows()).toHaveLength(3);
    expect(name.value).toBe('');
  });

  it('does not send an empty name', async () => {
    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();

    backend.expectNone({ method: 'POST', url: '/api/admin/themes' });
    expect(element.textContent).toContain('Le nom est obligatoire');
  });

  it('shows the API message when the name is already taken', async () => {
    type(element.querySelector<HTMLInputElement>('form input')!, 'cinéma');
    element.querySelector('form')!.dispatchEvent(new Event('submit'));

    backend
      .expectOne({ method: 'POST', url: '/api/admin/themes' })
      .flush(
        { title: 'Ce nom de thème est déjà utilisé.', detail: 'Un thème nommé « cinéma » existe déjà.' },
        { status: 409, statusText: 'Conflict' },
      );
    await fixture.whenStable();

    expect(element.querySelector('[role=alert]')?.textContent).toContain('Un thème nommé « cinéma » existe déjà.');
  });

  it('renames a theme inline', async () => {
    button(rows()[0], 'Renommer').click();
    await fixture.whenStable();

    const nameInput = rows()[0].querySelector<HTMLInputElement>('input[aria-label="Nom du thème"]')!;
    expect(nameInput.value).toBe('Cinéma');
    type(nameInput, 'Films');
    rows()[0].querySelector('form')!.dispatchEvent(new Event('submit'));

    const request = backend.expectOne({ method: 'PUT', url: '/api/admin/themes/1' });
    expect(request.request.body).toEqual({ name: 'Films', icon: '🎬' });
    request.flush({ ...cinema, name: 'Films' });

    await respondWithThemes([{ ...cinema, name: 'Films' }, empty]);
    expect(rows()[0].textContent).toContain('Films');
    expect(rows()[0].querySelector('input')).toBeNull();
  });

  it('shows why an add is refused when the icon is too long', async () => {
    const [name, icon] = Array.from(element.querySelectorAll<HTMLInputElement>('form input'));
    type(name, 'Sport');
    type(icon, 'x'.repeat(51));
    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();

    backend.expectNone({ method: 'POST', url: '/api/admin/themes' });
    expect(element.textContent).toContain("L'icône fait 50 caractères au plus.");
  });

  it('refuses an empty name when renaming, with a message', async () => {
    button(rows()[0], 'Renommer').click();
    await fixture.whenStable();

    type(rows()[0].querySelector<HTMLInputElement>('input[aria-label="Nom du thème"]')!, '');
    rows()[0].querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();

    backend.expectNone({ method: 'PUT', url: '/api/admin/themes/1' });
    expect(rows()[0].textContent).toContain('Le nom est obligatoire');
  });

  it('cancels a rename without calling the API', async () => {
    button(rows()[0], 'Renommer').click();
    await fixture.whenStable();

    button(rows()[0], 'Annuler').click();
    await fixture.whenStable();

    expect(rows()[0].querySelector('input')).toBeNull();
    expect(rows()[0].textContent).toContain('Cinéma');
  });

  it('shows the API message when a deletion fails', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);

    button(rows()[1], 'Supprimer').click();
    backend
      .expectOne({ method: 'DELETE', url: '/api/admin/themes/2' })
      .flush({ title: 'Ce thème contient des questions.' }, { status: 409, statusText: 'Conflict' });
    await fixture.whenStable();

    expect(element.querySelector('[role=alert]')?.textContent).toContain('Ce thème contient des questions.');
  });

  it('gives each row action an accessible name', () => {
    expect(button(rows()[0], 'Supprimer').getAttribute('aria-label')).toBe('Supprimer le thème Cinéma');
    expect(button(rows()[0], 'Renommer').getAttribute('aria-label')).toBe('Renommer le thème Cinéma');
  });

  it('deletes an empty theme after confirmation', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);

    button(rows()[1], 'Supprimer').click();

    backend.expectOne({ method: 'DELETE', url: '/api/admin/themes/2' }).flush(null);
    await respondWithThemes([cinema]);
    expect(rows()).toHaveLength(1);
  });

  it('does nothing when deletion is not confirmed', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false);

    button(rows()[1], 'Supprimer').click();

    backend.expectNone({ method: 'DELETE', url: '/api/admin/themes/2' });
  });
});
