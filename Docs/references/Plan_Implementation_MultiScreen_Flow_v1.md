# BUGS GAME — Plan d'implémentation : Flow multi-écran

**Version 1.0 — 3 mars 2026**
*Document technique destiné au développeur Unity*

---

## Statut du document

| Élément | Statut |
|---------|--------|
| Séquence des écrans | Verrouillé |
| Type de transitions | Verrouillé — Fade noir |
| Mountain screen (design) | Verrouillé — UI 2 zones |
| Consent + bouton quitter | Verrouillé — Dans ce scope |
| Score cumulé ou indépendant | À vérifier avec le client |
| Questionnaires in-game | Hors scope (chantier suivant) |
| Cyberball | Hors scope |

---

## 1. Vue d'ensemble du flow

Ce document spécifie le système de navigation multi-écran du jeu BUGS. Le flow organise l'expérience en sessions composées de blocs, eux-mêmes composés de trials. Chaque niveau de la hiérarchie possède ses propres écrans et transitions.

### 1.1 Hiérarchie de la session

| Niveau | Contenu | Fréquence |
|--------|---------|-----------|
| Session | 1 écran de consentement + N blocs + 1 écran de fin | 1 par participant |
| Bloc | 1 meta-choice + 1 mountain screen + N trials + 1 récapitulatif | N par session (paramètre chercheur) |
| Trial | 1 forest screen + 1 écran de résumé rapide | N par bloc (paramètre chercheur) |

### 1.2 Séquence complète

La séquence linéaire complète pour une session type :

```
CONSENT → [BLOC 1] → RECAP_BLOC → [BLOC 2] → RECAP_BLOC → ... → [BLOC N] → RECAP_BLOC → FIN
```

Détail d'un bloc :

```
META_CHOICE → MOUNTAIN → FOREST_1 → SUMMARY_1 → FOREST_2 → SUMMARY_2 → ... → FOREST_N → SUMMARY_N
```

Entre chaque écran : transition fade noir (durée configurable, défaut 0.5s).

---

## 2. Catalogue des écrans

Chaque écran est décrit avec son rôle, ses entrées/sorties de données, et ses interactions utilisateur.

### 2.1 Écran de consentement (CONSENT)

| Propriété | Valeur |
|-----------|--------|
| Identifiant | screen_type = 0 (d'après le CSV screen_id example) |
| Quand | Premier écran de la session, avant tout bloc |
| Rôle | Informer le participant sur l'expérience et recueillir son consentement éclairé |
| Contenu affiché | Texte de consentement (configurable par le chercheur via un fichier texte ou paramètre). Deux boutons : « J'accepte de participer » et « Je refuse » |
| Interaction | Clic sur un des deux boutons |
| Si accepte | Transition vers le premier bloc (META_CHOICE) |
| Si refuse | Écran de remerciement (« Merci, vous pouvez fermer cette fenêtre ») puis fin de session |
| Données CSV | Pas de colonne dédiée dans le CSV — mais le timestamp de début de session fait office de marqueur |

> **DESIGN** — Sobre et lisible. Texte centré, défilement si nécessaire. Boutons en bas de l'écran. Pas de timer.

### 2.2 Écran de meta-choice (META_CHOICE)

| Propriété | Valeur |
|-----------|--------|
| Identifiant | screen_type = meta_choice |
| Quand | Début de chaque bloc, avant le mountain screen |
| Fréquence | 1 fois par bloc |
| Rôle | Le participant choisit son type d'advisor pour le bloc entier |
| Options affichées | 3 choix : advisor moteur, advisor proximal, advisor distal. Chaque option montre un nom/icône et une description courte (configurable) |
| Mode free | Le participant clique sur l'advisor de son choix |
| Mode forced | Le système pré-sélectionne un advisor, le participant voit le choix mais ne peut pas le modifier (bouton grisé ou texte « choix imposé ») |
| Données CSV | advisor_type (colonne AB du BUGS CSV V1) |
| Persistance | L'advisor choisi est actif pour TOUS les trials du bloc courant |

> **DESIGN** — 3 cartes côte à côte (ou en colonne sur mobile). Chaque carte : icône + titre + description courte. Survol = highlight. Clic = sélection avec feedback visuel.

> **FREE/FORCED** — Le mode (free ou forced) est déterminé par le paramètre chercheur au niveau du bloc. En mode forced, l'UI affiche le choix mais le rend non-interactif.

### 2.3 Écran mountain / choix distal (MOUNTAIN)

| Propriété | Valeur |
|-----------|--------|
| Identifiant | screen_type = mountain / distal_choice |
| Quand | Après le meta-choice, avant les trials du bloc |
| Fréquence | 1 fois par bloc — la vallée choisie est fixe pour tout le bloc |
| Rôle | Le participant choisit entre deux vallées (zones) qui déterminent les paramètres de bugs pour les trials du bloc |
| Affichage | UI simple : deux zones côte à côte. Chaque zone montre un aperçu visuel (nom, illustration ou couleur distinctive). Pas de scène 3D. |
| Informations par vallée | Chaque vallée a : total_bugs_nb, green_bugs_nb, red_bugs_nb, visibility_noise. Ces valeurs sont générées par le système selon les paramètres du chercheur. Le participant ne voit PAS ces chiffres directement — il se fie au visuel et/ou au distal-advice. |
| Mode free | Le participant clique sur la vallée de son choix |
| Mode forced | Le système impose la vallée, l'UI montre le choix mais sans interaction |
| Distal advice | Si l'advisor choisi en meta-choice est de type distal ET qu'un conseil est distribué pour ce bloc (selon advice_given_frequency), le conseil est affiché : « L'advisor recommande la vallée [gauche/droite] ». L'advice peut être correct ou incorrect selon distal_advice_reliability. |
| Données CSV (état) | valley_total_bugs_nb, left_valley_green_bugs_nb, left_valley_red_bugs_nb, left_valley_visibility_noise, right_valley_green_bugs_nb, right_valley_red_bugs_nb, right_valley_visibility_noise, true_valley |
| Données CSV (advice) | distal_advice_given, distal_advice, valley_advice_explanation |
| Données CSV (input) | valley_choice, valley_advisor_choice_match |

> **DESIGN** — Deux panneaux côte à côte, fond distinct par vallée (ex. vert-bleu vs orange-brun). Label « Vallée A » / « Vallée B ». Hover = bordure highlight. Clic = sélection + feedback. Si distal advice actif : bandeau en haut « Conseil : vallée X recommandée ».

> **GÉNÉRATION** — Les paramètres des deux vallées (bugs, noise) sont générés au début du bloc par le système de génération procédurale, en utilisant la seed du bloc. La « true_valley » (la meilleure) est celle avec le meilleur ratio green/total ajusté du noise.

### 2.4 Écran forest (FOREST)

| Propriété | Valeur |
|-----------|--------|
| Identifiant | screen_type = forest / proximal_choice + motor_choice |
| Quand | Cœur de chaque trial, après le mountain screen (ou après le summary du trial précédent) |
| Fréquence | 1 par trial (N par bloc) |
| Rôle | Le participant navigue la grille, choisit un nuage de bugs, et collecte les bugs. C'est l'écran de gameplay principal. |
| Existant | Cet écran est déjà largement implémenté : grille, fog of war, bug clouds, traps, path optimal, step budget. Voir TDD v2.5 pour le détail. |
| Modification requise | Intégrer cet écran dans le flow multi-écran : il doit pouvoir être instancié, configuré avec les paramètres du bloc/trial courant, et signaler sa fin au flow controller. |
| Paramètres reçus du flow | block_id, trial_nb, seed du trial, paramètres de la vallée choisie (qui influencent les ranges de bugs), advisor actif, proximal/motor advice state |
| Signal de fin | Quand le joueur atteint un nuage de bugs (ou épuise ses pas), le forest signale « trial terminé » avec les données collectées |
| Données CSV | Toute la section Proximal-choice + Motor-choice du CSV V1 (colonnes AP à BS) |

> **INTÉGRATION** — Le forest screen existant doit être adapté pour recevoir sa configuration du FlowController plutôt que de la lire directement depuis SessionManager. Le TrialManager existant sera enrichi pour émettre un événement de fin de trial.

### 2.5 Écran de résumé de trial (TRIAL_SUMMARY)

| Propriété | Valeur |
|-----------|--------|
| Identifiant | Pas un screen_type séparé dans le CSV — fait partie du trial |
| Quand | Après chaque forest screen |
| Durée | Affichage bref (2-4 secondes, configurable) puis transition auto vers le trial suivant ou le récapitulatif de bloc |
| Contenu | Nombre de bugs verts collectés dans ce trial. Éventuellement : nombre de pas utilisés, pièges touchés. |
| Interaction | Aucune (ou clic pour passer plus vite) |
| Rôle | Feedback immédiat au participant entre deux trials |

> **DESIGN** — Overlay centré sur fond semi-transparent. Gros chiffre (nombre de bugs verts) + texte. Auto-disparition après timer.

### 2.6 Écran récapitulatif de bloc (BLOCK_RECAP)

| Propriété | Valeur |
|-----------|--------|
| Identifiant | Pas un screen_type séparé dans le CSV — marqueur de fin de bloc |
| Quand | Après le dernier trial de chaque bloc, avant le bloc suivant |
| Contenu | Récapitulatif du bloc : nombre total de bugs verts collectés sur le bloc, nombre de trials complétés, advisor utilisé, vallée choisie. Optionnel : performance comparative (« Vous avez collecté X bugs sur Y possibles »). |
| Interaction | Bouton « Continuer » pour passer au bloc suivant (ou à l'écran de fin si dernier bloc) |
| Rôle | Pause naturelle entre les blocs, permet au participant de souffler |

> **DESIGN** — Écran plein. Récapitulatif structuré avec chiffres mis en avant. Bouton « Bloc suivant » ou « Terminer » si dernier bloc.

### 2.7 Écran de fin (END)

| Propriété | Valeur |
|-----------|--------|
| Identifiant | Dernier écran de la session |
| Quand | Après le récapitulatif du dernier bloc |
| Contenu | Remerciement. Score final (si applicable). Message de debriefing configurable par le chercheur. |
| Interaction | Aucune (ou bouton « Fermer ») |
| Données | Timestamp de fin de session envoyé à l'API |

### 2.8 Overlay de sortie (QUIT_OVERLAY)

Accessible à tout moment pendant la session via un bouton persistant (coin supérieur droit, icône « X » ou « Quitter »). Ce n'est pas un écran à part entière mais un overlay modal.

| Propriété | Valeur |
|-----------|--------|
| Déclencheur | Bouton persistant visible sur TOUS les écrans (sauf le consentement initial) |
| Contenu | « Êtes-vous sûr de vouloir quitter l'expérience ? Vos données partielles seront conservées. » + boutons « Quitter » / « Continuer » |
| Si quitte | Sauvegarde des données partielles via l'API REST, puis écran de remerciement |
| Si continue | Retour à l'écran en cours, état préservé |
| Données | Si le participant quitte : flag « session_abandoned = true » + timestamp + dernier écran atteint |

---

## 3. Architecture technique

Cette section décrit l'architecture Unity recommandée pour implémenter le flow multi-écran.

### 3.1 Approche : scène unique + GameObjects activables

Plutôt que d'utiliser des scènes Unity séparées (qui imposent des temps de chargement et complexifient le partage de données), l'approche recommandée est une scène unique contenant tous les écrans sous forme de GameObjects racines activables/désactivables.

**Justification :** Le forest screen existant repose sur des Singletons (LevelRegistry, SessionManager) et un pipeline de génération procédurale séquencé par DefaultExecutionOrder. Charger/décharger des scènes risquerait de casser cette chaîne. Avec une scène unique, tous les Singletons persistent naturellement.

**Alternative envisageable :** Si la scène devient trop lourde, on peut séparer la Forest en scène additive (LoadSceneMode.Additive) tout en gardant une scène « Manager » persistante. Mais pour un premier jet, la scène unique est plus simple.

### 3.2 Hiérarchie de la scène

Structure recommandée du hierarchy panel Unity :

```
Scene: MainExperiment
  ├── [Managers]           (toujours actif)
  │   ├── FlowController
  │   ├── SessionManager   (existant)
  │   ├── TransitionManager
  │   ├── DataCollector
  │   └── QuitManager
  │
  ├── [UI_Persistent]      (toujours actif)
  │   ├── QuitButton
  │   ├── QuitOverlay
  │   └── FadeOverlay      (Canvas noir pour transitions)
  │
  ├── [Screen_Consent]     (activé/désactivé)
  ├── [Screen_MetaChoice]  (activé/désactivé)
  ├── [Screen_Mountain]    (activé/désactivé)
  ├── [Screen_Forest]      (activé/désactivé) ← contient le jeu existant
  ├── [Screen_TrialSummary](activé/désactivé)
  ├── [Screen_BlockRecap]  (activé/désactivé)
  └── [Screen_End]         (activé/désactivé)
```

### 3.3 Le FlowController — machine à états

Le FlowController est le chef d'orchestre du flow. C'est un Singleton qui gère la progression de la session via une machine à états (state machine).

| État | Écran actif | Transition vers | Déclencheur |
|------|-------------|-----------------|-------------|
| Consent | Screen_Consent | BlockStart OU SessionEnd | Bouton accepter / refuser |
| BlockStart | (interne) | MetaChoice | Automatique — initialise le bloc courant |
| MetaChoice | Screen_MetaChoice | Mountain | Choix de l'advisor (ou auto si forced) |
| Mountain | Screen_Mountain | TrialStart | Choix de la vallée (ou auto si forced) |
| TrialStart | (interne) | Forest | Automatique — initialise le trial courant |
| Forest | Screen_Forest | TrialSummary | Signal de fin du forest screen |
| TrialSummary | Screen_TrialSummary | TrialStart OU BlockRecap | Timer auto (ou clic). Si trials restants → TrialStart, sinon → BlockRecap |
| BlockRecap | Screen_BlockRecap | BlockStart OU SessionEnd | Bouton continuer. Si blocs restants → BlockStart, sinon → SessionEnd |
| SessionEnd | Screen_End | — | Terminal |

**Pseudo-code du FlowController :**

```csharp
public class FlowController : MonoBehaviour {
    public enum State { Consent, BlockStart, MetaChoice, Mountain,
                        TrialStart, Forest, TrialSummary,
                        BlockRecap, SessionEnd }

    private State _currentState;
    private int _currentBlockIndex;
    private int _currentTrialIndex;
    private BlockConfig _currentBlock;

    // Appelé par chaque écran quand il a terminé
    public void OnScreenComplete(ScreenResult result) {
        switch (_currentState) {
            case State.Consent:
                if (result.accepted) TransitionTo(State.BlockStart);
                else TransitionTo(State.SessionEnd);
                break;
            case State.MetaChoice:
                _currentBlock.advisorType = result.advisorType;
                TransitionTo(State.Mountain);
                break;
            case State.Mountain:
                _currentBlock.valleyChoice = result.valleyChoice;
                TransitionTo(State.TrialStart);
                break;
            case State.Forest:
                DataCollector.SaveTrialData(result.trialData);
                TransitionTo(State.TrialSummary);
                break;
            case State.TrialSummary:
                _currentTrialIndex++;
                if (_currentTrialIndex < _currentBlock.totalTrials)
                    TransitionTo(State.TrialStart);
                else TransitionTo(State.BlockRecap);
                break;
            case State.BlockRecap:
                _currentBlockIndex++;
                if (_currentBlockIndex < session.totalBlocks)
                    TransitionTo(State.BlockStart);
                else TransitionTo(State.SessionEnd);
                break;
        }
    }
}
```

### 3.4 TransitionManager — fade noir

Le TransitionManager gère les transitions visuelles entre écrans. Toutes les transitions utilisent un fade noir (écran noir progressif).

| Propriété | Valeur |
|-----------|--------|
| Type | Fade to black → switch screen → Fade from black |
| Durée totale | Configurable. Défaut : 0.5s fade-out + 0.5s fade-in = 1s total |
| Implémentation | Canvas UI plein écran avec un Image noir. Animation de l'alpha via coroutine ou DOTween. |
| Séquence | 1) Lancer fade-out (alpha 0→1). 2) Quand noir complet : désactiver l'ancien écran, activer le nouveau, initialiser le nouveau. 3) Lancer fade-in (alpha 1→0). |
| Sorting order | Le Canvas du fade doit être au-dessus de tout (sort order élevé, ex: 999) |

**Pseudo-code :**

```csharp
public IEnumerator Transition(GameObject fromScreen, GameObject toScreen, System.Action onMidpoint) {
    yield return FadeOut(fadeDuration);    // Noir complet
    fromScreen.SetActive(false);
    onMidpoint?.Invoke();                  // Init du nouvel écran
    toScreen.SetActive(true);
    yield return FadeIn(fadeDuration);     // Retour visible
}
```

### 3.5 Flux de données entre écrans

Les données circulent du FlowController vers les écrans (paramètres de configuration) et des écrans vers le FlowController (résultats de l'interaction).

| Source | Destination | Données |
|--------|-------------|---------|
| SessionManager | FlowController | Configuration globale de la session : nombre de blocs, trials par bloc, paramètres du chercheur |
| FlowController | Screen_MetaChoice | Liste des advisors disponibles, mode free/forced |
| Screen_MetaChoice | FlowController | advisor_type choisi |
| FlowController | Screen_Mountain | Paramètres des deux vallées (générés), distal advice (si applicable), mode free/forced |
| Screen_Mountain | FlowController | valley_choice, timestamps |
| FlowController | Screen_Forest | Seed du trial, paramètres de bugs (issus de la vallée choisie), advisor state, trial_nb, block_id |
| Screen_Forest | FlowController | TrialData complet (bugs collectés, path, traps, reward, timestamps…) |
| FlowController | DataCollector | Données accumulées par trial → envoi à l'API REST |
| FlowController | Screen_TrialSummary | Score du trial terminé |
| FlowController | Screen_BlockRecap | Score cumulé du bloc, stats du bloc |

---

## 4. Intégration avec le code existant

Le forest screen est déjà implémenté avec 18 scripts C#. L'objectif est de l'intégrer dans le flow sans casser son fonctionnement. Voici les modifications nécessaires sur le code existant.

### 4.1 Modifications de GameManager

**Situation actuelle :** GameManager gère le cycle de vie d'un round unique (setup → play → collect → end → restart). Il assume qu'il est le point d'entrée et qu'il recharge lui-même les rounds en boucle.

**Modification :** GameManager ne doit plus décider du timing des rounds. Il doit exposer deux points d'entrée :

```csharp
// Nouveau contrat de GameManager
public void StartRound(TrialConfig config);   // Appelé par FlowController
public event System.Action<TrialResult> OnRoundComplete;  // Signale la fin
```

Le FlowController appelle `StartRound()` avec la configuration du trial courant. Quand le round se termine, GameManager émet `OnRoundComplete` avec les résultats. GameManager ne fait plus de boucle auto — c'est le FlowController qui décide de relancer un round ou de passer au récapitulatif.

### 4.2 Modifications de SessionManager

**Situation actuelle :** SessionManager stocke la configuration de la session et la fournit aux scripts via un Singleton. Il lit les paramètres CLI au lancement.

**Modification :** SessionManager conserve son rôle de source de configuration globale, mais on lui ajoute :

```csharp
// Ajouts à SessionManager
public int TotalBlocks { get; }
public int TrialsPerBlock { get; }
public BlockConfig GetBlockConfig(int blockIndex);
public bool IsForced(ChoiceType choiceType, int blockIndex);
```

BlockConfig est un nouveau DTO (data transfer object) qui regroupe : advisor type (si forced), valley parameters, advice reliability, advice frequency, etc.

### 4.3 Modifications de TrialManager / DataCollector

**Situation actuelle :** TrialManager construit le TrialData et l'envoie à l'API REST après chaque trial.

**Modification :** TrialData doit être enrichi pour couvrir les ~75 colonnes du CSV V1. Plutôt que de tout modifier dans TrialManager, on introduit un DataCollector qui :

1. Reçoit les données de chaque écran (meta-choice, mountain, forest)
2. Les accumule dans un TrialDataRow complet
3. Envoie la row complète à l'API à la fin de chaque trial
4. Gère aussi les données de niveau bloc et session

TrialManager reste responsable de la collecte des données spécifiques au forest screen. DataCollector agrège les données de toutes les sources.

### 4.4 Impact sur les autres scripts

| Script existant | Impact | Modification |
|-----------------|--------|-------------|
| LevelRegistry | Aucun | Pas de changement — continue de gérer la grille |
| TilesSpawner | Faible | Doit pouvoir être ré-initialisé proprement entre les trials (clear + regenerate) |
| BugCloudSpawner | Faible | Les paramètres de bugs (totalBugs, greenRatio) doivent venir de la vallée choisie via TrialConfig |
| FogOfWar | Aucun | Fonctionne indépendamment |
| GridMover | Aucun pour ce chantier | Le motor-choice (touches dynamiques) sera un chantier séparé |
| PathSpawner | Aucun | Continue de calculer le chemin optimal |
| TrapSpawner | Faible | Le nombre de traps peut varier selon la vallée — paramètre à ajouter dans TrialConfig |
| PlayerSpawner | Aucun | Pas de changement |
| CorridorWallsGenerator | Aucun | Pas de changement |
| SeedService | Faible | La seed par trial doit être dérivée de : sessionSeed + blockIndex + trialIndex (déjà possible avec le système FNV-1a existant) |

---

## 5. Structures de données

Nouveaux DTOs nécessaires pour le flow multi-écran.

### 5.1 SessionConfig

```csharp
[System.Serializable]
public class SessionConfig {
    public string sessionId;
    public int totalBlocks;
    public int trialsPerBlock;
    public int seed;
    public string consentText;
    public string endText;
    public BlockConfig[] blocks;  // Pré-configuré par le chercheur
}
```

### 5.2 BlockConfig

```csharp
[System.Serializable]
public class BlockConfig {
    public int blockId;
    public bool metaChoiceForced;
    public AdvisorType forcedAdvisorType;     // Si metaChoiceForced
    public bool mountainChoiceForced;
    public ValleyChoice forcedValleyChoice;   // Si mountainChoiceForced
    public ValleyParams leftValley;
    public ValleyParams rightValley;
    public float distalAdviceReliability;
    public float proximalAdviceReliability;
    public float motorAdviceReliability;
    public float adviceGivenFrequency;
    public int trialsInBlock;
}
```

### 5.3 ValleyParams

```csharp
[System.Serializable]
public class ValleyParams {
    public int totalBugs;
    public int greenBugs;
    public int redBugs;
    public float visibilityNoise;
}
```

### 5.4 TrialConfig

```csharp
[System.Serializable]
public class TrialConfig {
    public int blockId;
    public int trialNb;
    public int seed;          // Dérivé de sessionSeed + blockId + trialNb
    public ValleyParams activeValley;  // Vallée choisie au mountain screen
    public AdvisorType activeAdvisor;
    public bool proximalAdviceGiven;
    public bool motorAdviceGiven;
}
```

### 5.5 ScreenResult (union)

```csharp
public class ScreenResult {
    public bool accepted;              // Consent
    public AdvisorType advisorType;    // MetaChoice
    public ValleyChoice valleyChoice;  // Mountain
    public TrialData trialData;        // Forest
    public float timestamp;
}
```

*Note : On peut aussi utiliser des classes séparées (ConsentResult, MetaChoiceResult, etc.) et un système d'interface IScreenResult. L'approche union est plus simple pour démarrer.*

---

## 6. Plan d'implémentation séquencé

Le chantier est découpé en 7 étapes séquentielles. Chaque étape est testable indépendamment.

### Étape 1 — Infrastructure de base

**Effort estimé :** 1-2 jours

**Tâches :**

1. Créer le FlowController (state machine vide, transitions entre états)
2. Créer le TransitionManager (fade noir avec coroutine)
3. Créer le FadeOverlay (Canvas UI + Image noir, alpha animé)
4. Créer le QuitManager (bouton persistant + overlay de confirmation)
5. Réorganiser la hiérarchie de la scène (GameObjects racines par écran)
6. Vérifier que le forest screen fonctionne toujours après réorganisation

> **CRITÈRE DE VALIDATION** — Le forest screen s'active/désactive proprement. Le fade noir fonctionne entre deux GameObjects vides. Le bouton quitter affiche l'overlay.

### Étape 2 — Écran de consentement + écran de fin

**Effort estimé :** 0.5-1 jour

**Tâches :**

1. Créer Screen_Consent : UI Canvas avec texte scrollable + deux boutons
2. Créer Screen_End : UI Canvas avec message de remerciement
3. Brancher sur le FlowController : Consent → BlockStart ou SessionEnd
4. Le texte de consentement est chargé depuis un fichier texte ou ScriptableObject

> **CRITÈRE DE VALIDATION** — Lancer le jeu → écran de consentement s'affiche. Clic « Accepter » → fade noir → (état BlockStart, pour l'instant vide). Clic « Refuser » → fade → écran de fin.

### Étape 3 — Écran meta-choice

**Effort estimé :** 1-2 jours

**Tâches :**

1. Créer Screen_MetaChoice : UI avec 3 cartes (advisor moteur, proximal, distal)
2. Implémenter le mode free (clic sélectionne) et forced (pré-sélectionné, non-interactif)
3. Le résultat (advisor_type) est transmis au FlowController
4. Brancher dans le flow : BlockStart → MetaChoice → Mountain
5. Créer le BlockConfig et le brancher sur SessionManager

> **CRITÈRE DE VALIDATION** — Après le consentement → meta-choice s'affiche. Clic sur un advisor → fade → (mountain, pour l'instant vide). En mode forced → le choix est affiché mais pas modifiable.

### Étape 4 — Écran mountain (choix distal)

**Effort estimé :** 1-2 jours

**Tâches :**

1. Créer Screen_Mountain : UI avec deux zones côte à côte
2. Générer les paramètres des vallées (ValleyParams) à partir de la seed du bloc
3. Implémenter le mode free/forced
4. Afficher le distal advice si applicable (bandeau conditionnel)
5. Le résultat (valley_choice) est transmis au FlowController
6. Brancher : MetaChoice → Mountain → TrialStart

> **CRITÈRE DE VALIDATION** — Meta-choice → mountain → deux zones affichées. Clic sur une vallée → choix enregistré. Le distal advice s'affiche quand applicable.

### Étape 5 — Intégration du forest screen dans le flow

**Effort estimé :** 2-3 jours

**Tâches :**

1. Modifier GameManager : exposer `StartRound(TrialConfig)` + événement `OnRoundComplete`
2. Supprimer la boucle auto de GameManager (plus de rechargement automatique)
3. Les paramètres de bugs du forest proviennent de la vallée choisie (via TrialConfig)
4. La seed du trial est dérivée correctement (sessionSeed + blockId + trialNb)
5. Créer le nettoyage inter-trial (clear grille, reset fog, destroy clouds/traps)
6. Brancher : TrialStart → Forest → TrialSummary
7. Vérifier que N trials d'affilée fonctionnent sans fuite mémoire ni état résiduel

> **CRITÈRE DE VALIDATION** — Flow complet : consentement → meta-choice → mountain → forest trial 1 → forest trial 2 → ... Chaque trial se configure correctement. Pas de state leaking entre trials.

### Étape 6 — Écrans de résumé et récapitulatif

**Effort estimé :** 1-2 jours

**Tâches :**

1. Créer Screen_TrialSummary : overlay avec score du trial (auto-disparition)
2. Créer Screen_BlockRecap : écran plein avec stats du bloc + bouton continuer
3. Brancher : Forest → TrialSummary → (trial suivant ou BlockRecap) → (bloc suivant ou SessionEnd)
4. Implémenter le comptage de score (bugs verts collectés) — logique dépend de la décision client sur cumul vs indépendant

> **CRITÈRE DE VALIDATION** — Flow complet multi-blocs : consent → bloc 1 (meta + mountain + N trials + recap) → bloc 2 → ... → fin. Le récapitulatif affiche des données cohérentes.

### Étape 7 — DataCollector + pipeline CSV enrichi

**Effort estimé :** 2-3 jours

**Tâches :**

1. Créer le DataCollector qui agrège les données de tous les écrans
2. Enrichir TrialData pour couvrir les colonnes du CSV V1 pertinentes au flow
3. Ajouter les colonnes : screen_type, block_id, screen_id, advisor_type, valley_choice, valley_advisor_choice_match, trial_nb, etc.
4. Vérifier l'envoi à l'API REST avec le format enrichi
5. Implémenter le screen_id (compteur par type d'écran comme montré dans l'onglet screen_id example du template Excel)
6. Sauvegarder les données partielles en cas de quit anticipé

> **CRITÈRE DE VALIDATION** — Exécuter une session complète (2 blocs × 3 trials). Vérifier que les données envoyées à l'API correspondent aux colonnes attendues du CSV V1. Tester le quit en milieu de session.

---

## 7. Estimation globale et risques

| Étape | Effort estimé | Risque |
|-------|---------------|--------|
| 1. Infrastructure de base | 1-2 jours | Faible |
| 2. Consent + Fin | 0.5-1 jour | Faible |
| 3. Meta-choice | 1-2 jours | Faible |
| 4. Mountain screen | 1-2 jours | Moyen — génération des vallées à valider avec le chercheur |
| 5. Intégration forest | 2-3 jours | Élevé — touche au code existant, risque de régression |
| 6. Résumés + récap | 1-2 jours | Faible — dépend de la décision sur le score cumulé |
| 7. DataCollector + CSV | 2-3 jours | Moyen — mapping CSV V1 à valider |
| **TOTAL** | **8-15 jours développeur** | |

### 7.1 Risques identifiés

| Risque | Impact | Mitigation |
|--------|--------|------------|
| Nettoyage inter-trial incomplet (state leaking) | Élevé — données corrompues | Tests systématiques : exécuter 10+ trials d'affilée, vérifier l'absence de données résiduelles |
| Performance de la scène unique si trop d'objets | Moyen — lag possible | Profiler après l'étape 5. Si problème, passer en scènes additives. |
| Paramètres de vallée mal calibrés | Moyen — expérience non valide | Validation avec le chercheur après l'étape 4. Prévoir un mode debug affichant les valeurs. |
| Décision score cumulé en attente | Faible — bloque l'étape 6 | Implémenter les deux modes (cumul bloc / cumul session / indépendant) derrière un enum configurable. |
| Format CSV V1 peut encore évoluer | Moyen — rework | Utiliser un système de sérialisation flexible (dictionnaire clé-valeur) plutôt qu'une struct rigide. |

---

## 8. Point ouvert — à confirmer avec le client

Un point de design reste ouvert et doit être tranché avant ou pendant l'implémentation de l'étape 6 :

| Question | Options | Impact |
|----------|---------|--------|
| Le score (bugs verts collectés) est-il cumulé ou indépendant par trial ? | A) Cumulé par bloc (reset entre blocs) — B) Cumulé sur toute la session — C) Indépendant par trial (affiché puis oublié) | Affecte le TRIAL_SUMMARY (score affiché), le BLOCK_RECAP (total affiché), et potentiellement la motivation du participant. |

> **RECOMMANDATION** — Implémenter un enum `ScoreMode { PerTrial, PerBlock, Session }` configurable par le chercheur, avec `PerBlock` comme défaut. Cela évite de refaire le travail si la décision change.

---

## 9. Checklist pré-développement

Avant de commencer l'implémentation, le développeur doit s'assurer que :

1. ☐ Le TDD v2.5 est à jour et reflète l'état réel du code
2. ☐ Le projet Unity compile sans erreur sur la branche de développement
3. ☐ Les tests existants passent (si applicable)
4. ☐ Le template CSV V1 est la référence validée par le chercheur (pas le CSV V0)
5. ☐ Le texte de consentement est fourni par le client
6. ☐ Le nombre de blocs et trials par bloc est défini (ou des valeurs par défaut sont convenues)
7. ☐ L'API REST accepte le nouveau format de données enrichi (ou un endpoint de test est disponible)
8. ☐ La décision sur le score cumulé est prise (ou le mode configurable est accepté)
