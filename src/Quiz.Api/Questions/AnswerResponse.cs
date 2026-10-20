namespace Quiz.Api.Questions;

/// <summary>
/// Une réponse vue par l'administrateur, bonne réponse comprise.
/// Réservé au back-office : ne jamais réutiliser pour le jeu (règle d'architecture n°4).
/// </summary>
public sealed record AnswerResponse(int Id, string Text, bool IsCorrect);
