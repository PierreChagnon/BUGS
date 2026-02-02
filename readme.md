flowchart TD

%% === Étape 1 : SessionManager ===
A["SessionManager - CreateSession()"] -->|"Appel API /api/session"| B["API Server"]
B -->|"Réponse : game_session_id"| C["GameManager - BeginFirstRound()"]

%% === Étape 2 : GameManager ===
C --> D["GameManager - StartNewRound()"]
D -->|"screenCounter++"| E["TrialManager - StartNewTrial()"]

%% === Étape 3 : TrialManager ===
E -->|"Crée un TrialData\nAssocie game_session_id\nPrépare map_config"| F["Boucle de Gameplay"]

%% === Étape 4 : Gameplay Loop ===
F --> G["GridMover - Player bouge"]
G --> H["GameManager.OnPlayerStep(cell)"]
H -->|"steps++ / check traps / update score + HUD / RecordMove()"| I{"Joueur atteint un nuage ?"}

I -->|"Non"| F
I -->|"Oui"| J["GameManager.OnCloudCollected()"]

%% === Étape 5 : Fin de manche ===
J -->|"Appelé par BugCloud.OnTriggerEnter()"| K["Fin de manche"]
K -->|"Détermine le nuage choisi\nVérifie si c’est le meilleur\nBloque les inputs\nMet à jour le score"| L["Appels TrialManager"]

%% === Appels TrialManager depuis OnCloudCollected ===
L --> M1["TrialManager.SetOptimalPathLength()"]
L --> M2["TrialManager.EndCurrentTrial()"]
L --> M3["TrialManager.SendTrials()"]

%% === Étape 6 : TrialManager suite ===
M1 --> N1["Enregistre la longueur optimale"]
M2 --> N2["Remplit TrialData\n(choice, correct, end_timestamp)"]
M3 --> N3["SendTrialsCoroutine()\n→ POST /api/trials\n→ Vide la mémoire locale"]

%% === Étape 7 : Game Over UI ===
N3 --> O["Game Over UI\nAffiche score et stats"]
O --> P["Bouton Restart\n→ GameManager.RestartRound()"]

%% === Étape 8 : Nouvelle manche ===
P --> Q["Scene Reload\n→ Retour à StartNewRound()"]
