# Note de cadrage — Quiz multijoueur

Oct 18, 2026 · @Antonin

## Contexte et objectifs

Le projet est un quiz multijoueur en temps réel sur PC, avec un back-office de gestion des questions. Il sert de projet portfolio pour la recherche d'un CDI de développeur full-stack.

**Objectifs :**

- Montrer une maîtrise opérationnelle d'**ASP.NET Core** et d'**Angular**, absents du parcours actuel (React/Django).
- Montrer du **temps réel** (SignalR), de la gestion de **concurrence** et un **état de partie côté serveur**.
- Montrer une appli de gestion classique bien faite : CRUD, validation, pagination, authentification.
- Pratiquer le **développement piloté par agents IA** (cadrage, prompts, relecture, tests), compétence désormais attendue par certains recruteurs.
- Disposer d'une démo jouable en entretien : le recruteur rejoint une partie.

## Concept et parcours utilisateur

Un hôte crée une session, choisit des thèmes, ses amis rejoignent avec un code, et tout le monde joue la même série de questions tirées au hasard. Le jeu est **PC uniquement**, et **l'hôte joue** comme les autres.

**Trois profils :**

- **Hôte** : crée et configure la session, lance la partie, peut la mettre en pause. Il joue aussi.
- **Joueur** : rejoint avec un code et un pseudo, sans compte.
- **Administrateur** : gère la banque de thèmes et de questions dans le back-office.

**Déroulé d'une partie :**

1. L'hôte crée une session : thèmes, nombre de questions (20 par défaut), durées.
2. Le serveur génère un code de session. L'hôte rejoint son propre lobby.
3. Les joueurs saisissent le code et un pseudo, et apparaissent dans le lobby.
4. L'hôte clique sur « Lancer la partie ». Le serveur tire les questions.
5. Pour chaque question : question chronométrée, révélation de la réponse, classement.
6. Après la dernière question : podium final et résultats enregistrés.

## Périmètre

Le MVP couvre une partie complète jouable et un back-office suffisant pour alimenter la banque de questions.

| Dans le MVP | Hors périmètre (évolutions possibles) |
| --- | --- |
| Création de session avec choix des thèmes et du nombre de questions | Version mobile |
| Lobby avec code de session | Comptes joueurs, historique personnel |
| Partie en temps réel : chrono, révélation, classement, podium | Mode équipe |
| Reconnexion d'un joueur, transfert du rôle d'hôte | Questions avec images, vrai/faux, remise en ordre |
| Back-office : thèmes, questions, import/export JSON | Plusieurs serveurs (backplane Redis) |
| Statistiques par question (taux de bonnes réponses) | Comptes administrateurs multiples avec rôles |
| Banque initiale : 4 à 6 thèmes, environ 30 questions chacun | Contributions de questions par les joueurs |

## Fonctionnalités

L'application se découpe en trois espaces : la création de session, le jeu et le back-office.

**Création de session (hôte)**

- Choix d'un ou plusieurs thèmes, avec le nombre de questions disponibles affiché pour chacun.
- Choix du nombre de questions (20 par défaut) et des durées (question, révélation, classement).
- Code de session court et lisible, valable le temps de la partie.

**Lobby et jeu (hôte et joueurs)**

- Rejoindre avec un code et un pseudo unique dans la session.
- Liste des joueurs présents en direct, bouton « Lancer la partie » pour l'hôte.
- Écran de question : énoncé, 2 à 4 réponses, compte à rebours, indicateur « X joueurs ont répondu ».
- Écran de révélation : bonne réponse et répartition des réponses.
- Écran de classement : scores, places gagnées ou perdues.
- Podium final, puis enregistrement des résultats.
- Bouton pause réservé à l'hôte.

**Back-office (administrateur)**

- Accès protégé par authentification.
- Thèmes : création, renommage, suppression interdite tant qu'ils contiennent des questions.
- Questions : liste paginée, recherche texte, filtre par thème, création et modification.
- Formulaire : énoncé, thème, 2 à 4 réponses dont exactement une correcte.
- Import et export JSON de la banque de questions.
- Statistiques : taux de bonnes réponses par question, pour repérer les questions trop faciles, trop dures ou mal formulées.

## Règles du jeu

La partie avance seule, à un rythme fixé par le serveur. L'hôte joue comme les autres et ne contrôle que la pause.

**Rythme**

- Une question se termine quand le chrono atteint zéro (20 secondes par défaut) ou dès que tous les joueurs connectés ont répondu.
- Suivent la révélation (environ 5 secondes) puis le classement (environ 5 secondes).
- Une seule réponse par joueur et par question. Une réponse arrivée après la deadline est ignorée.

**Scoring (proposition à valider)**

- Mauvaise réponse ou pas de réponse : 0 point.
- Bonne réponse : de 1000 points (réponse immédiate) à 500 points (réponse à la dernière seconde), dégressif linéairement.
- Le temps de réponse est mesuré côté serveur.
- À égalité de points, le joueur au temps de réponse cumulé le plus faible passe devant.

**Tirage des questions**

- Questions tirées au hasard parmi les thèmes choisis, sans doublon dans une partie.
- Répartition équilibrée entre les thèmes (20 questions et 4 thèmes donnent 5 questions par thème), le reste complété au hasard.
- Ordre des réponses mélangé à chaque question.
- Si les thèmes choisis contiennent moins de questions que demandé, la création de session le signale et propose le maximum disponible.

## Architecture et stack technique

Une seule application Angular parle à un seul serveur ASP.NET Core : en HTTP pour le back-office, en WebSocket (SignalR) pour le jeu.

&#91;embedded content: architecture · navigateur, serveur, base de données\]

Le moteur de jeu, mis en avant, détient l'état des parties en cours ; seuls la banque de questions et les résultats passent par la base.

| Couche | Technologies |
| --- | --- |
| Front-end | Angular récent (standalone components, signals), formulaires réactifs, client `@microsoft/signalr` |
| Back-end | ASP.NET Core (.NET LTS), SignalR, Entity Framework Core, FluentValidation, authentification JWT |
| Base de données | PostgreSQL |
| Tests | xUnit, `WebApplicationFactory`, tests Angular, Playwright |
| Outillage | Docker Compose, GitHub Actions, agent de code (Claude Code ou équivalent) |

## Modèle de données

Seules la banque de questions et les résultats sont persistés. L'état d'une partie en cours vit en mémoire sur le serveur.

| Entité | Champs principaux | Relations |
| --- | --- | --- |
| Theme | Id, Nom, Icône | Contient des Questions |
| Question | Id, Énoncé, Difficulté (facultative), CreatedAt | Appartient à un Theme, possède 2 à 4 Answers |
| Answer | Id, Texte, EstCorrecte | Appartient à une Question |
| GameSession | Id, Code, Statut, NbQuestions, Durées, CreatedAt, EndedAt | Référence les Themes choisis |
| PlayerResult | Id, Pseudo, ScoreFinal, Rang | Appartient à une GameSession |
| AnswerLog | Id, Correcte, TempsDeRéponseMs | Lie une Question à une GameSession, sert aux statistiques |
| AdminUser | Id, Email, MotDePasseHaché | Aucune |

## Points techniques clés et qualité

Le serveur fait autorité sur tout ce qui touche au score : le client affiche, il ne décide jamais.

**Points techniques**

- **Anti-triche** : la bonne réponse n'est jamais envoyée au client avant la révélation, hôte compris. Le chrono et les points sont calculés côté serveur. Le client reçoit une deadline et affiche le compte à rebours.
- **Moteur de jeu pur** : la logique (états, tirage, scoring) vit dans une bibliothèque sans dépendance à SignalR. L'horloge et le générateur aléatoire sont injectables, pour des tests déterministes.
- **Machine à états** : Lobby, puis Question, Révélation et Classement en boucle, puis Fin. La pause est un état accessible depuis chaque étape de jeu.
- **Concurrence** : toutes les commandes d'une partie passent par un seul point d'entrée sérialisé (verrou par partie ou file de commandes `Channel<T>`).
- **Déconnexions** : un jeton de session stocké dans le navigateur permet à un joueur de reprendre sa place et son score. Si l'hôte ne revient pas au bout de 30 secondes, le rôle passe au joueur suivant.
- **Montée en charge** : l'état en mémoire limite le jeu à un seul serveur. Le passage à plusieurs nécessiterait un backplane Redis et un état externalisé.

**Exigences de qualité**

- Tests unitaires du moteur de jeu (xUnit) : tirage, scoring, transitions, cas limites.
- Tests d'intégration de l'API et du hub SignalR (`WebApplicationFactory`).
- Tests des composants Angular et validateurs de formulaires.
- Un test Playwright de bout en bout : un hôte et trois joueurs jouent une partie complète.
- CI GitHub Actions bloquante : build, tests, lint.
- Lancement local en une commande avec Docker Compose.
- README avec captures ou GIF de démo.

## Jalons de réalisation

Le back-office vient en premier : c'est la partie la plus classique, pour prendre en main .NET, Angular et le pilotage d'agent, et la banque de questions sera prête quand le jeu arrivera.

1. **Socle** : solution .NET, projet Angular, Docker Compose, CI, `CLAUDE.md` et conventions.
2. **Modèle de données et back-office** : entités, migrations, authentification admin, CRUD thèmes et questions, import JSON de la banque initiale.
3. **Création de session** : choix des thèmes et paramètres, génération du code.
4. **Moteur de jeu** : machine à états, tirage, scoring, tests unitaires.
5. **Hub SignalR et écrans de jeu** : lobby, question, révélation, classement, podium.
6. **Robustesse** : reconnexion, transfert d'hôte, pause, réponses en double ou tardives.
7. **Finitions** : statistiques par question, export JSON, test Playwright de bout en bout, README et démo.

## Risques et points ouverts

Le risque principal est de laisser l'agent IA écrire du code .NET ou Angular non compris. La règle : rien n'est mergé sans pouvoir être expliqué.

**Risques**

- **Questions erronées** : une banque générée par IA contiendra des erreurs factuelles. Relecture obligatoire avant import.
- **Périmètre qui grossit** : s'en tenir au MVP avant toute évolution.
- **Timers et concurrence** : bugs difficiles à reproduire. D'où l'horloge injectable et les tests du moteur.
- **Stacks nouvelles** : courbe d'apprentissage sur .NET et Angular. Commencer par le back-office pour s'y faire.

**Points ouverts**

- [ ] Nom du projet
- [ ] Formule de scoring définitive
- [ ] Nombre maximum de joueurs par session
- [x] Choix PostgreSQL ou SQL Server → **PostgreSQL** (image Docker légère, en place depuis le jalon 1)
- [ ] Hébergement de la démo en ligne (VPS ou Azure)
- [x] Liste des thèmes de la banque initiale → **Cinéma, Géographie, Histoire, Sciences, Musique, Sport** (questions écrites par Claude, relues par le développeur avant import ; livrées un thème par PR, Cinéma en premier)
