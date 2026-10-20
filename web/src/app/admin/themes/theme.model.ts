/** Un thème tel que renvoyé par GET /api/admin/themes. */
export interface Theme {
  id: number;
  name: string;
  icon: string | null;
  questionCount: number;
}

/** Corps de la création (POST) et du renommage (PUT) d'un thème. */
export interface SaveTheme {
  name: string;
  icon: string | null;
}
