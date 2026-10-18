using System.Reflection;

namespace Quiz.Game.Tests;

public class ArchitectureTests
{
    // Règle d'architecture n°1 : Quiz.Game est du C# pur.
    // Seules les assemblies de la bibliothèque standard .NET sont autorisées.
    [Fact]
    public void QuizGame_ReferencesOnlyBaseClassLibrary()
    {
        var gameAssembly = Assembly.Load("Quiz.Game");

        var forbidden = gameAssembly.GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .Where(name => !IsBaseClassLibrary(name))
            .ToList();

        Assert.Empty(forbidden);
    }

    private static bool IsBaseClassLibrary(string assemblyName) =>
        assemblyName is "netstandard" or "mscorlib"
        || assemblyName == "System"
        || assemblyName.StartsWith("System.", StringComparison.Ordinal);
}
