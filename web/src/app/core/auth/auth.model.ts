/** Réponse de POST /api/auth/login. */
export interface LoginResponse {
  accessToken: string;
  /** Date ISO 8601 au-delà de laquelle l'API refusera le jeton. */
  expiresAt: string;
}
