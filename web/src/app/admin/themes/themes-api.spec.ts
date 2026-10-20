import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ThemesApi } from './themes-api';

describe('ThemesApi', () => {
  let api: ThemesApi;
  let backend: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(ThemesApi);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  it('creates a theme', () => {
    api.create({ name: 'Cinéma', icon: '🎬' }).subscribe();

    const request = backend.expectOne('/api/admin/themes');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ name: 'Cinéma', icon: '🎬' });
  });

  it('updates a theme', () => {
    api.update(3, { name: 'Cinéma', icon: null }).subscribe();

    const request = backend.expectOne('/api/admin/themes/3');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ name: 'Cinéma', icon: null });
  });

  it('deletes a theme', () => {
    api.delete(3).subscribe();

    expect(backend.expectOne('/api/admin/themes/3').request.method).toBe('DELETE');
  });
});
