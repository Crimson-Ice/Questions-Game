import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { SaveTheme, Theme } from './theme.model';

export const themesUrl = '/api/admin/themes';

/** Modifications des thèmes. La lecture de la liste passe par un httpResource dans l'écran. */
@Injectable({ providedIn: 'root' })
export class ThemesApi {
  private readonly http = inject(HttpClient);

  create(theme: SaveTheme): Observable<Theme> {
    return this.http.post<Theme>(themesUrl, theme);
  }

  update(id: number, theme: SaveTheme): Observable<Theme> {
    return this.http.put<Theme>(`${themesUrl}/${id}`, theme);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${themesUrl}/${id}`);
  }
}
