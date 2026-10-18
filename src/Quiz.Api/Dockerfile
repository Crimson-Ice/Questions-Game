# Contexte de build : la racine du repo (l'API a besoin de Quiz.Game, Quiz.Data et des fichiers communs).

# --- Étape 1 : compilation avec le SDK complet ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Fichiers communs et .csproj d'abord : la restauration NuGet reste en cache tant qu'ils ne changent pas.
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/Quiz.Api/Quiz.Api.csproj src/Quiz.Api/
COPY src/Quiz.Game/Quiz.Game.csproj src/Quiz.Game/
COPY src/Quiz.Data/Quiz.Data.csproj src/Quiz.Data/
RUN dotnet restore src/Quiz.Api/Quiz.Api.csproj

COPY src/ src/
RUN dotnet publish src/Quiz.Api/Quiz.Api.csproj --configuration Release --output /app --no-restore

# --- Étape 2 : image finale avec le seul runtime ASP.NET Core ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Utilisateur non root fourni par l'image Microsoft.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Quiz.Api.dll"]
