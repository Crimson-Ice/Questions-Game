# CLAUDE.md — Quiz multijoueur

## Le projet

Quiz multijoueur en temps réel, sur PC uniquement, avec un back-office de gestion des questions.

- Un **hôte** crée une session : il choisit des thèmes, le nombre de questions (20 par défaut) et les durées.
- Les **joueurs** rejoignent avec un code et un pseudo, sans compte. L'hôte joue aussi.
- Le serveur tire les questions au hasard parmi les thèmes choisis, la partie avance automatiquement, et le classement s'affiche après chaque question.
- Un **administrateur** gère thèmes et questions dans un back-office protégé.

La note de cadrage complète (règles du jeu, modèle de données, jalons) est dans `docs/cadrage.md`. En cas de doute fonctionnel, elle fait foi.

Le développeur apprend .NET et Angular avec ce projet. Tu dois donc expliquer brièvement tes choix quand ils ne sont pas évidents (patterns .NET, API Angular, configuration).

## Structure du repo

```
Quiz.slnx
├── src/
│   ├── Quiz.Api/          # ASP.NET Core : API REST, hub SignalR, auth, config, DI
│   ├── Quiz.Game/         # Moteur de jeu PUR : états, tirage, scoring
│   └── Quiz.Data/         # EF Core : entités, DbContext, migrations, seed
├── tests/
│   ├── Quiz.Game.Tests/   # Tests unitaires du moteur (xUnit)
│   └── Quiz.Api.Tests/    # Tests d'intégration API + hub (WebApplicationFactory)
├── web/                   # Front Angular (jeu + back-office)
├── e2e/                   # Tests Playwright de bout en bout
├── seed/                  # Banque de questions initiale (JSON)
├── docs/                  # Cadrage, décisions d'architecture
└── docker-compose.yml
```

## Commandes

```bash
# Tout lancer en local (API + front + PostgreSQL)
docker compose up

# Back-end
dotnet build
dotnet test
dotnet ef migrations add <Nom> --project src/Quiz.Data --startup-project src/Quiz.Api

# Front-end (depuis web/)
npm ci
npm start
npm test
npm run lint

# Bout en bout (depuis e2e/)
npx playwright test
```

Avant de déclarer une tâche terminée : `dotnet build`, `dotnet test`, `npm test` et `npm run lint` passent sans erreur ni avertissement nouveau.

## Règles d'architecture (non négociables)

1. **`Quiz.Game` ne référence aucun autre projet ni aucun package d'infrastructure** : ni SignalR, ni EF Core, ni ASP.NET. C'est du C# pur.
2. **L'horloge et l'aléatoire sont injectés** dans le moteur (`TimeProvider`, et une abstraction pour le générateur aléatoire). Jamais de `DateTime.Now`, `DateTime.UtcNow` ou `new Random()` dans `Quiz.Game`.
3. **Le serveur fait autorité.** Le score, le chrono et la validité d'une réponse sont calculés côté serveur. Le client reçoit une deadline et affiche ; il ne décide jamais.
4. **La bonne réponse n'est jamais envoyée au client avant la phase de révélation**, y compris à l'hôte. Les DTO de question envoyés pendant la phase de jeu n'ont pas de champ `IsCorrect`.
5. **Une partie = un seul point d'entrée sérialisé.** Toute commande qui modifie l'état d'une partie passe par ce point (verrou par partie ou `Channel<T>`). Pas de modification concurrente de l'état.
6. **Le hub SignalR reste fin** : il traduit les messages en commandes pour le moteur et diffuse les événements. Aucune règle métier dans le hub.
7. **Les entités EF Core ne sortent pas de l'API** : on expose des DTO.

## Conventions back-end (C#)

- .NET LTS, nullable activé, `TreatWarningsAsErrors` activé.
- Nommage C# standard : `PascalCase` pour types et membres publics, `_camelCase` pour les champs privés, suffixe `Async` sur les méthodes asynchrones.
- Validation des entrées avec FluentValidation, erreurs renvoyées en `ProblemDetails`.
- Un fichier par type. Pas de classes « fourre-tout » (`Utils`, `Helpers`).
- Les règles métier du back-office sont testées : une question a 2 à 4 réponses dont **exactement une** correcte ; un thème qui contient des questions ne peut pas être supprimé.

## Conventions front-end (Angular)

- Angular récent : standalone components, signals pour l'état local, pas de NgModules.
- Formulaires réactifs typés. Le formulaire de question utilise un `FormArray` pour les réponses, avec un validateur « exactement une bonne réponse ».
- Un service unique encapsule la connexion SignalR (`@microsoft/signalr`) et expose l'état de la partie sous forme de signals.
- Le back-office est derrière une route protégée par un guard.
- Pas de logique de score ou de chrono côté front : uniquement de l'affichage.

## Tests

- Toute logique du moteur de jeu est couverte par des tests unitaires : transitions d'état, tirage (sans doublon, répartition équilibrée, pool insuffisant), scoring, réponses tardives ou en double.
- Les tests du moteur sont **déterministes** : horloge factice et graine aléatoire fixe. Aucun `Thread.Sleep` ni `Task.Delay` réel dans les tests.
- Un bug corrigé = un test qui le reproduit, écrit avant la correction.
- Ne jamais supprimer ou affaiblir un test pour faire passer la CI. Si un test semble faux, le signaler au lieu de le modifier.

## Mode de travail : guidage

Le développeur écrit le code lui-même, y compris Docker et la CI. Ton rôle est de le guider et de relire, pas d'écrire à sa place.

- **Tu ne modifies aucun fichier du projet**, sauf demande explicite pour un changement précis.
- Chaque étape suit le même cycle : explication courte du concept (avec un lien vers la doc officielle si utile), consigne (commande à lancer ou fichier à écrire), le développeur le fait, puis tu relis son diff, lances build, tests et lint, et commentes ce qui peut être amélioré et pourquoi.
- **L'aide diminue avec le temps** : la première fois qu'une notion apparaît, tu donnes le code complet expliqué ligne par ligne (il le tape lui-même) ; ensuite, seulement la consigne et des indices.
- **Moteur de jeu** : tu écris les tests, le développeur écrit le code qui les fait passer.
- Tu restes responsable de la vérification (build, tests, lint) et de la rédaction des descriptions de PR.

## Façon de travailler

- **Petites étapes.** Une fonctionnalité à la fois, avec ses tests. Propose un plan court avant toute tâche qui touche plus de 3 fichiers, et attends la validation.
- **Pas de dépendance nouvelle** (package NuGet ou npm) sans le signaler et expliquer pourquoi.
- **Pas d'anticipation** : n'implémente que ce qui est demandé. Les évolutions hors périmètre (mobile, équipes, Redis…) sont listées dans `docs/cadrage.md` et ne doivent pas être commencées.
- Si une consigne de ce fichier entre en conflit avec une demande, signale le conflit au lieu de trancher seul.
- Messages de commit en français, au format `type(portée): description` (par exemple `feat(game): tirage équilibré des questions`).

## Branches et pull requests

- **Jamais de commit direct sur `main`.** Une branche par fonctionnalité ou correctif, nommée `type/description-courte` (par exemple `feat/tirage-questions`, `fix/reponse-tardive`).
- **Une PR = un sujet.** Une PR reste petite et relisible : idéalement moins de 400 lignes modifiées, hors fichiers générés (migrations, lockfiles). Au-delà, découpe en plusieurs PR.
- **Description de PR obligatoire**, en français, avec ces sections :
  - **Contexte** : quel jalon ou quelle fonctionnalité, et pourquoi.
  - **Changements** : liste courte des modifications principales.
  - **Choix techniques** : les décisions non évidentes et les alternatives écartées.
  - **Tests** : ce qui est testé, et comment vérifier à la main si besoin.
  - **Points d'attention** : ce que le relecteur doit regarder de près.
- **La CI doit être verte** avant le merge. Ne jamais contourner une CI rouge.
- Les PR sont relues et mergées par le développeur, jamais par toi. Ton rôle s'arrête à l'ouverture de la PR (avec `gh pr create` si disponible).
- Merge en **squash** : le titre de la PR suit le même format que les commits et devient le commit sur `main`.
