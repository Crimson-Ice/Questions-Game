namespace Quiz.Api.Tests.Infrastructure;

/// <summary>
/// Les classes de test marquées <c>[Collection(ApiCollection.Name)]</c> partagent une seule
/// <see cref="QuizApiFactory"/>, donc un seul conteneur PostgreSQL.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<QuizApiFactory>
{
    public const string Name = "Api";
}
