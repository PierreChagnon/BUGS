# Spec Fonctionnelle — Composant UI "How To Play"

> Produit par le Rôle 2 (Analyse Fonctionnelle).
> Décrit le QUOI et le POURQUOI. Jamais le COMMENT technique.

**Date :** 2026-05-28
**Statut :** `draft`
**Chantier :** UI — Composant "How To Play" (tutoriel paginé d'onboarding)
**Spec tech associée :** `Docs/specs/how-to-play/spec-tech.md`

---

## 1. Contexte et scope

### Objectif

Livrer un **composant UI réutilisable et autonome** de type tutoriel paginé / carrousel d'onboarding (référence : "How To Play" de Fall Guys, Slay the Spire, Ninjala, Mario & Sonic). Le composant gère son visuel, sa navigation, son cycle de vie et sa logique interne. Il reçoit une liste de pages (taille arbitraire, définie au runtime) et la restitue page par page avec navigation Next/Prev.

Cette spec couvre **uniquement la livraison du composant UI**, en mode autonome avec dummy data Inspector pour le test en isolation. Le branchement réel sur les données éditables par les chercheurs (JSON externe, API) et l'intégration dans le flow de l'application (IntroScene, FlowController, transitions de phase) sont **hors scope** et seront pris en charge par un collègue à partir de l'API publique exposée par le composant.

### Périmètre IN

- [x] Composant UI auto-suffisant, testable en isolation (lancement d'une scène sandbox sans dépendance externe)
- [x] Affichage d'une page comprenant une **image**, un **header** (titre court) et un **body** (texte explicatif)
- [x] Navigation **Next** / **Prev** entre les pages
- [x] Pagination visuelle par **dots** générés dynamiquement selon le nombre de pages
- [x] Bouton **Close** sticky : initialement caché, devient visible une fois la dernière page atteinte, et **reste** disponible si l'utilisateur revient en arrière ensuite
- [x] À la dernière page, le bouton **Next** se transforme en **Close** (libellé/icône)
- [x] **API publique de pilotage** au runtime : `SetPages(...)`, `Open()`, `Close()`
- [x] **Événements lifecycle** émis vers l'extérieur : `Closed`, `Completed` (= closed après vue complète)
- [x] **Auto-affichage en mode dev** : si dummy data Inspector renseignée et qu'aucun pilotage externe n'est fait, le composant s'auto-ouvre au Start (permet le test en sandbox)
- [x] **Nombre de pages variable** au runtime — toute la mécanique (dots, navigation, gating Close) découle mécaniquement de `pages.Count`, jamais d'un nombre codé en dur
- [x] **SFX au clic** sur chaque bouton (pattern projet `UIButtonSound`)
- [x] **Dummy data V1** : ~6 pages représentatives proposées pour le test visuel en isolation

### Périmètre OUT

| Exclu | Raison | Reporté à |
|:---|:---|:---|
| Branchement du composant dans `IntroScene.unity` | À la charge de l'intégrateur | Chantier "intégration flow onboarding" |
| Câblage avec `FlowController` (déclenchement, transition après `Closed`) | À la charge de l'intégrateur | Chantier "intégration flow onboarding" |
| Source de données réelle (JSON / API / endpoint chercheur) | À la charge de l'intégrateur ; D5b non tranché à ce stade | Chantier "back-office config chercheurs" |
| Tracking de la consultation dans `trial_responses` (vue, durée, skip) | Non requis V1 ; le composant n'écrit aucune donnée de recherche | Chantier ultérieur si besoin chercheur émerge |
| Multilingue / clé i18n | Non requis V1 ; les textes sont stockés bruts en `string` dans le modèle Page | Chantier ultérieur si requis |
| Validation de contenu (longueur min/max texte, dimensions image) | Le composant accepte n'importe quelle data passée par l'API | À traiter côté outillage chercheur |
| Animations/transitions de page élaborées (slide, fade, etc.) | Non demandé ; transitions simples par swap de contenu (cohérent patterns projet) | Chantier "polish UI" si pertinent |
| Contenu final des pages | Sera fourni par les chercheurs au moment du branchement | Hors scope du composant |
| Pause `Time.timeScale` et `GameManager.SetInputLocked` | Le composant est neutre. Si un futur intégrateur souhaite pause/lock pendant l'affichage, c'est à lui de l'orchestrer autour de `Open()`/`Closed`. | Intégrateur |
| Accès depuis `MenuBtn` ou autre déclencheur en cours de jeu | Hors scope du composant ; un éventuel accès se fera via `Open()` côté intégrateur | Intégrateur |

### Décisions déjà prises

- **Scope strict composant** — le porteur livre l'artefact UI autonome ; l'intégration data + flow est confiée à un autre développeur. La spec doit refléter cette frontière de manière non ambiguë.
- **D2** — Modèle de page : vue simple (1 image + 1 header + 1 body). Pas de multi-vignettes par page.
- **D4** — Skip autorisé uniquement après que la dernière page ait été atteinte au moins une fois (gate sticky).
- **D5** — Data dummy via sérialisation Inspector. API publique pour réception runtime. **Aucun ScriptableObject**, aucun pattern Provider/Strategy interne au composant — le branchement à une source externe se fera par appel à `SetPages(...)` depuis l'extérieur.
- **D6** — Aucun tracking interne. Le composant émet uniquement les events `Closed` / `Completed`, l'intégrateur en fait ce qu'il veut.

### Dépendances

- **Requiert :** `UIButtonSound` (déjà existant), `AudioManager` (pour le SFX au clic, indirectement via UIButtonSound), TextMeshPro, EventSystem standard Unity.
- **Est requis par :** l'intégration onboarding (par un collègue) qui posera le prefab dans `IntroScene`, branchera la source data via `SetPages(...)` et écoutera `Closed` pour appeler `FlowController.OnPhaseComplete()`.
- **Indépendant de :** `FlowController`, `GameManager`, `LevelRegistry`, `SessionManager`, `TrialManager`, `ApiClient` — le composant n'a aucune référence à ces systèmes.

---

## 2. Catalogue des comportements

### 2.1 Affichage d'une page

**Rôle :** Présenter une étape du tutoriel au participant.
**Quand :** En permanence tant que le panel est ouvert.

#### Ce qui est affiché

- Un **panel** modal centré à l'écran avec un fond opaque (sprite SCI-FI Pack Pro, cohérent avec `SettingsPanelUI`).
- Au sein du panel :
  - Une **image** principale (la `Sprite` de la page courante).
  - Un **header** (titre court de la page, en `TMP_Text`).
  - Un **body** (texte explicatif multi-lignes de la page, en `TMP_Text`).
- En footer du panel :
  - Un bouton **Prev** (à gauche).
  - Une rangée de **dots** de pagination (au centre) — un dot par page, le dot actif visuellement distingué.
  - Un bouton **Next** (à droite). À la dernière page, ce bouton porte le libellé/icône **Close**.
- En haut-droite du panel : un bouton **Close** dédié, initialement **masqué**, qui devient **visible** une fois la dernière page atteinte au moins une fois (et le reste — sticky).

#### Interactions utilisateur

| Action utilisateur | Résultat | Cas limite |
|:---|:---|:---|
| Clic sur `Next` (hors dernière page) | Affiche la page suivante. SFX joué. | — |
| Clic sur `Next` à la dernière page (donc bouton "Close") | Ferme le panel. Émet `Closed` et `Completed`. SFX joué. | — |
| Clic sur `Prev` (index > 0) | Affiche la page précédente. SFX joué. | — |
| Clic sur `Prev` à `index == 0` | Aucun effet (bouton non interactif / grisé). | Pas de feedback "tentative refusée" requis. |
| Clic sur un dot | Aucun effet (dots non-cliquables, indicateurs seulement). | — |
| Clic sur le bouton `Close` (top-right, quand visible) | Ferme le panel. Émet `Closed` (et `Completed` si vue complète, ce qui est garanti par la règle d'apparition du bouton). SFX joué. | — |
| Aucune action — survol du curseur | Aucun effet particulier (pas de tooltip prévu). | — |
| Tentative de fermer via touche `Escape` | Le composant ne gère pas Escape natif. L'EventSystem peut router `Cancel` vers le bouton sélectionné. | À ne pas spec en V1 ; comportement EventSystem standard. |

#### Règles métier

- **R1** : Le bouton `Prev` est non-interactif si `index == 0`.
- **R2** : Le bouton `Close` (top-right) est masqué tant que la dernière page (`index == count - 1`) n'a pas été atteinte. Une fois atteinte, il devient visible et **reste** visible si l'utilisateur revient en arrière (flag sticky `lastReached`).
- **R3** : À la dernière page, le bouton `Next` change visuellement pour signaler la fermeture (libellé "Close" ou icône équivalente). Son comportement devient identique à celui du bouton `Close` top-right (ferme + émet events).
- **R4** : Le dot actif visuellement distingué correspond à `index` courant.
- **R5** : La fermeture émet **toujours** `Closed`. Elle émet **en plus** `Completed` si l'utilisateur a vu au moins une fois la dernière page (`lastReached == true`). Avec R2 + R3, la seule façon de fermer est d'avoir vu la dernière page — donc en pratique `Completed` est émis à chaque fermeture. La distinction reste utile pour un futur cas où l'intégrateur ajouterait une fermeture forcée externe (ex : timeout) qui n'aurait pas vu la dernière page.

#### Modes

- Aucun mode multiple. Le composant a un seul mode de fonctionnement.

#### Données collectées

| Donnée | Colonne CSV V1 | Valeurs possibles | Quand enregistrée |
|:---|:---|:---|:---|
| _Aucune._ Le composant n'écrit dans aucun système de tracking. | — | — | — |

---

### 2.2 Réception de données et pilotage externe

**Rôle :** Permettre à un système externe (futur intégrateur) de fournir au composant la liste de pages à afficher et de piloter son ouverture/fermeture.
**Quand :** À tout moment du cycle de vie du composant, via les méthodes publiques.

#### Ce qui est affiché

Pas de visuel propre à ce comportement — il s'agit d'un comportement d'API. Le résultat visuel est l'affichage de la page 0 de la nouvelle liste après `Open()`.

#### Interactions externes (API)

| Action externe | Résultat | Cas limite |
|:---|:---|:---|
| Appel `SetPages(list)` | Remplace la liste interne, reset `index` à 0, reset `lastReached` à `false`. Si le panel est ouvert, affiche immédiatement la page 0 de la nouvelle liste. | Si `list` est `null` ou vide → cf. R7 |
| Appel `Open()` | Active le panel, affiche la page 0. | Si aucune liste n'a été fournie (pas de dummy data Inspector et pas de `SetPages` préalable) → cf. R8 |
| Appel `Close()` | Désactive le panel, émet `Closed` (et `Completed` si applicable). | Si le panel est déjà fermé → no-op silencieux |
| Souscription `event Closed` | L'intégrateur reçoit la notification à chaque fermeture. | — |
| Souscription `event Completed` | L'intégrateur reçoit la notification uniquement si la fermeture intervient après vue complète. | — |

#### Règles métier

- **R6** : Si `dummyPages` (Inspector) est non vide **et** que **aucun appel externe à `SetPages()` n'a été effectué**, le composant utilise `dummyPages` au Start et s'auto-ouvre. Cela permet de tester le composant en isolation dans une scène sandbox, sans intégration. Si `SetPages()` est appelé (avant ou après Start), la dummy data est ignorée au profit de la data passée en argument.
- **R7** : Appel `SetPages(null)` ou `SetPages(emptyList)` → log warning, l'état interne est remis à zéro, le panel est fermé s'il était ouvert. Aucun affichage de page (rien à afficher).
- **R8** : Appel `Open()` sans data disponible (ni dummy Inspector, ni `SetPages` préalable) → log warning, no-op (le panel ne s'ouvre pas).

#### Modes

- Aucun.

#### Données collectées

| Donnée | Colonne CSV V1 | Valeurs possibles | Quand enregistrée |
|:---|:---|:---|:---|
| _Aucune._ | — | — | — |

---

## 3. Règles transversales

- **TR1 — Composant agnostique** : le composant ne référence aucun singleton du projet (`FlowController`, `GameManager`, `SessionManager`, `LevelRegistry`, `ApiClient`, `TrialManager`). Il ne référence pas non plus de scène ou de prefab spécifique au flow. Toutes les interactions externes passent par les méthodes publiques (`SetPages`, `Open`, `Close`) et les events (`Closed`, `Completed`).
- **TR2 — Nombre de pages dynamique** : aucune valeur de N n'est codée en dur. Tout dérive de `pages.Count` au moment de l'affichage. Cela vaut pour la navigation (bornes, gating Close), les dots (générés/recyclés selon `Count`), et le comportement à la dernière page.
- **TR3 — Pas de pause ni d'input lock** : le composant n'altère pas `Time.timeScale` et n'appelle pas `GameManager.SetInputLocked`. Si un intégrateur souhaite pauser le jeu ou verrouiller l'input pendant que le panel est ouvert, c'est à lui de le faire autour de `Open()`/`Closed`.
- **TR4 — SFX au clic** : chaque bouton (Prev, Next, Close) porte un composant `UIButtonSound` avec un `SoundEffect` UI assigné (typiquement `Sfx_UI_Click`, cohérent avec le reste du projet, cf. spec `audio/spec-tech.md`).
- **TR5 — Input clavier/manette** : le composant ne câble pas d'`InputAction` custom. La navigation est laissée à l'EventSystem standard Unity (focus initial sur `Next`, navigation `Submit`/`Cancel`/`Navigate` héritée de la map UI existante du projet — `InputSystem_Actions.inputactions`). Aucune action particulière n'est exigée du composant pour faire fonctionner clavier/manette ; tout passe par la sélection EventSystem.
- **TR6 — Visuel aligné sur les conventions projet** : sprites issus du SCI-FI UI Pack Pro (cohérent `SettingsPanelUI`), TextMeshPro pour tout texte, layout via Unity UI standard (RectTransform + LayoutGroup pour les dots).
- **TR7 — Aucun nettoyage inter-trial** : le composant est local à sa scène, instancié au Start, détruit à la transition de scène. Pas de singleton, pas de `DontDestroyOnLoad`, pas d'état partagé entre instances.

---

## 4. Matrice de traçabilité

**N/A pour la V1.** Le composant n'écrit aucune donnée dans `trial_responses` ni dans aucun autre système de tracking (D6). Si un futur besoin émerge (logguer la durée de consultation, le nombre de pages vues, le skip), il sera traité par un chantier ultérieur via abonnement aux events `Closed` / `Completed` côté intégrateur — sans modification du composant.

---

## 5. Questions ouvertes

### Dans le scope du composant

Aucune. Toutes les décisions structurantes ont été arbitrées (cf. §1 "Décisions déjà prises").

### Hors scope, listées pour mémoire à l'intention de l'intégrateur

| # | Question | Options | Impact | Urgence |
|:---|:---|:---|:---|:---|
| Q-HTP-1 | Origine de la donnée réelle en production (`SetPages` alimenté depuis quoi ?) | A: endpoint API Supabase / B: fichier `StreamingAssets` / C: URL externe configurable | Pipeline data chercheur | À traiter avec le back-office chercheur |
| Q-HTP-2 | Déclenchement du panel (Open) dans le flow | A: auto à l'entrée de `IntroScene` (remplace `FlowContinueScreenUI(Intro)`) / B: bouton "How To Play" depuis `WelcomeScene` / C: hybride (auto + accessible depuis MenuBtn) | UX onboarding | À traiter à l'intégration |
| Q-HTP-3 | Tracking de la consultation dans `trial_responses` | A: aucun (V1) / B: vue oui/non + durée totale / C: log détaillé par page | Donnée de recherche | À évaluer avec le chercheur |
| Q-HTP-4 | Localisation multilingue | A: FR seul (V1) / B: clés i18n dès maintenant | Internationalisation | À évaluer si une vague de participants non-FR est prévue |
| Q-HTP-5 | Contenu final des pages (textes + visuels) | À fournir par les chercheurs après définition du protocole | Onboarding effectif | À fixer en amont du lancement |
| Q-HTP-6 | Articulation avec questions ouvertes Q-007 (perte bugs), Q-008 (4 vs 2 paths), Q-010 (reliability) | Le contenu des pages "pièges", "paths", "advisor" dépend de la résolution de ces Q | Cohérence didactique | À traiter avec le chercheur avant figeage des textes |
