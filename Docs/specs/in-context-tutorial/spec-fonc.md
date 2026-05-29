# Spec Fonctionnelle — Composant UI "In-Context Tutorial"

> Produit par le Rôle 2 (Analyse Fonctionnelle).
> Décrit le QUOI et le POURQUOI. Jamais le COMMENT technique.

**Date :** 2026-05-29
**Statut :** `draft`
**Chantier :** UI — Composant "In-Context Tutorial" (overlay d'instructions contextuelles sur scène de jeu)
**Spec tech associée :** `Docs/specs/in-context-tutorial/spec-tech.md`

---

## 1. Contexte et scope

### Objectif

Livrer un **composant UI réutilisable et autonome** affichant un **overlay d'instructions
contextuelles** directement sur une scène de jeu. À la différence du tutoriel d'onboarding
« How To Play » (carrousel paginé hors gameplay), l'in-context tutorial illustre **un point
particulier sur la scène concernée** : un titre + un texte fournis par le chercheur, présentés
à l'arrivée sur la scène, que le participant referme d'un clic sur **OK** avant de jouer.

Le composant est destiné aux scènes `AdvisorChoiceScene`, `DistalChoiceScene` et
`ProximalScene` (Forest). Le **contenu** et la **décision d'affichage** (quel bloc, quelle
scène, à quel trial) proviennent du back-end et sont pilotés par l'intégrateur via l'API
publique du composant. Cette spec couvre **uniquement la livraison du composant UI** en mode
autonome, avec dummy data Inspector pour le test en isolation.

### Périmètre IN

- [x] Composant UI auto-suffisant, testable en isolation (scène sandbox sans dépendance externe)
- [x] Overlay **modal** : fond plein écran (bloque la souris), **titre** (`TMP_Text`) et **corps**
      de texte (`TMP_Text`)
- [x] Un unique bouton **OK** qui ferme l'overlay
- [x] **API publique de pilotage** : `Show(title, body)`, `Show()`, `SetContent(title, body)`,
      `Close()`, propriétés `IsOpen` / `HasContent`
- [x] **Événements** émis vers l'extérieur : `Shown`, `Closed`
- [x] **Règle « pas de contenu → pas d'overlay »** : un `Show` avec titre ET corps vides
      n'affiche rien (no-op silencieux, retour `false`)
- [x] **Auto-affichage en mode dev** : si dummy data Inspector renseignée et qu'aucun pilotage
      externe n'a eu lieu, le composant s'auto-ouvre au Start (test en sandbox)
- [x] **SFX au clic** sur le bouton OK (pattern projet `UIButtonSound`)
- [x] **Dummy data V1** : un titre + un corps représentatifs proposés pour le test visuel

### Périmètre OUT

| Exclu | Raison | Reporté à |
|:---|:---|:---|
| Branchement dans les 3 vraies scènes (`AdvisorChoiceScene`, `DistalChoiceScene`, `ProximalScene`) | À la charge de l'intégrateur | Chantier "intégration in-context tutorial" |
| Logique de décision « afficher ou non » (bloc tutorial ? scène ? 1er trial ?) | Décision portée par l'intégrateur à partir de `FlowController.State` — le composant reste agnostique | Intégrateur (cf. `integration-guide.md`) |
| Extension de `BlockConfig` (champs titre/texte par scène) + désérialisation back-end | Pipeline data — périmètre intégrateur | Chantier "back-office config chercheurs" |
| Verrou des inputs gameplay clavier sur la scène Proximal | Le composant n'est pas couplé au gameplay ; le verrou est orchestré par l'intégrateur autour de `Show()`/`Closed` | Intégrateur |
| Tracking de la consultation dans `trial_responses` | Le composant n'écrit aucune donnée de recherche | Chantier ultérieur si besoin chercheur |
| Multilingue / clé i18n | Textes stockés bruts en `string` | Chantier ultérieur si requis |
| Pagination, image d'illustration, animations élaborées | Non demandé V1 (panneau unique titre+texte, transitions par `SetActive`) | Chantier "polish UI" si pertinent |
| Contenu final des instructions | Fourni par les chercheurs au branchement | Hors scope du composant |

### Décisions déjà prises

- **D1 — Scope strict composant.** Le porteur livre l'artefact UI autonome + une sandbox de test
  éditeur ; l'intégration data + flow est confiée à un autre développeur. Frontière non ambiguë.
- **D2 — Contenu : titre + texte uniquement.** Pas d'image, pas de pagination (à la différence de
  How To Play). Le back-end fournit un titre + un texte par scène.
- **D3 — Composant 100 % agnostique.** Aucune référence à `FlowController`, `GameManager`, etc.
  La logique de gating « overlay au 1er trial du bloc seulement » (cf. §3) appartient à
  l'intégrateur. Le composant ne fait qu'afficher / fermer / recevoir du contenu.
- **D4 — Modalité.** Le composant fournit un fond modal qui bloque la **souris**. Le verrou des
  inputs **clavier** gameplay (scène Proximal) est orchestré par l'intégrateur autour des events.
- **D5 — Dummy data via sérialisation Inspector.** API publique pour la réception runtime. Aucun
  ScriptableObject, aucun pattern Provider/Strategy interne.
- **D6 — Aucun tracking interne.** Le composant émet uniquement `Shown` / `Closed`.

### Dépendances

- **Requiert :** `UIButtonSound` (existant), `AudioManager` (SFX clic, indirect), TextMeshPro,
  EventSystem standard Unity.
- **Est requis par :** l'intégration in-context tutorial (par un collègue) qui posera le prefab
  dans les 3 scènes, décidera de l'affichage à partir de la config back-end, et écoutera `Closed`
  pour déverrouiller le gameplay le cas échéant.
- **Indépendant de :** `FlowController`, `GameManager`, `LevelRegistry`, `SessionManager`,
  `TrialManager`, `ApiClient` — aucune référence à ces systèmes.

---

## 2. Catalogue des comportements

### 2.1 Affichage de l'overlay

**Rôle :** Présenter une instruction contextuelle au participant à l'arrivée sur la scène.
**Quand :** Tant que l'overlay est ouvert.

#### Ce qui est affiché

- Un **panel modal** centré, par-dessus le contenu de la scène, avec un **fond plein écran**
  (sprite SCI-FI Pack Pro, cohérent avec `HowToPlayPanel` / `SettingsPanelUI`) qui **bloque les
  clics souris** vers la scène en dessous.
- Au sein du panel :
  - Un **titre** (header court, en `TMP_Text`).
  - Un **corps** de texte explicatif multi-lignes (en `TMP_Text`).
  - Un bouton **OK** (unique) qui ferme l'overlay.

#### Interactions utilisateur

| Action utilisateur | Résultat | Cas limite |
|:---|:---|:---|
| Clic sur `OK` | Ferme l'overlay. Émet `Closed`. SFX joué. | — |
| Clic souris hors du panel (sur le fond modal) | Aucun effet : le fond absorbe le clic mais ne ferme pas l'overlay (fermeture uniquement par OK). | Empêche la fermeture accidentelle et protège le gameplay sous-jacent. |
| Touche `Escape` | Le composant ne gère pas `Escape` natif. Comportement EventSystem standard (non spécifié V1). | À ne pas spec en V1. |

#### Règles métier

- **R1 — Pas de contenu, pas d'overlay.** Si le titre **et** le corps sont vides/`null`, aucun
  overlay n'est affiché (cf. R4 §2.2). C'est ce qui permet à l'intégrateur de toujours appeler
  `Show(...)` sans vérification préalable : une scène sans instructions reste vierge.
- **R2 — Modalité souris.** Tant que l'overlay est ouvert, le fond plein écran bloque les clics
  souris vers la scène. (Le verrou des inputs clavier gameplay est hors composant — cf. TR3.)

#### Modes

- Aucun mode multiple. Un seul mode de fonctionnement.

#### Données collectées

| Donnée | Colonne CSV V1 | Valeurs possibles | Quand enregistrée |
|:---|:---|:---|:---|
| _Aucune._ Le composant n'écrit dans aucun système de tracking. | — | — | — |

---

### 2.2 Réception de données et pilotage externe

**Rôle :** Permettre à l'intégrateur de fournir le contenu (titre + texte) et de piloter
l'ouverture/fermeture de l'overlay.
**Quand :** À tout moment via les méthodes publiques.

#### Interactions externes (API)

| Action externe | Résultat | Cas limite |
|:---|:---|:---|
| `Show(title, body)` | Définit le contenu puis affiche. Retourne `true` si réellement affiché, `false` si contenu vide. | Titre ET corps vides → cf. R4 |
| `Show()` | Affiche le contenu déjà défini (dummy Inspector ou `SetContent` préalable). | Aucun contenu → cf. R4 |
| `SetContent(title, body)` | Définit le contenu sans afficher. Désactive le fallback dummy au prochain `Start`. | — |
| `Close()` | Ferme l'overlay, émet `Closed`. | Déjà fermé → no-op silencieux (cf. R5) |
| Souscription `event Shown` | Notification émise à chaque ouverture effective. | — |
| Souscription `event Closed` | Notification émise à chaque fermeture. | — |

#### Règles métier

- **R3 — Auto-show dev.** Si la dummy data Inspector (titre/corps) est non vide **et** qu'aucun
  appel externe à `Show`/`SetContent` n'a été fait, le composant s'auto-ouvre au `Start`. Dès
  qu'un appel externe a lieu, la dummy est ignorée.
- **R4 — Contenu vide.** `Show(...)` ou `Show()` avec titre ET corps vides → log warning, aucun
  overlay affiché, retour `false`. Aucune erreur.
- **R5 — Fermeture redondante.** `Close()` sur un overlay déjà fermé → no-op silencieux, aucun
  event ré-émis.

#### Données collectées

| Donnée | Colonne CSV V1 | Valeurs possibles | Quand enregistrée |
|:---|:---|:---|:---|
| _Aucune._ | — | — | — |

---

## 3. Règles transversales

- **TR1 — Composant agnostique.** Aucune référence à `FlowController`, `GameManager`,
  `SessionManager`, `LevelRegistry`, `ApiClient`, `TrialManager`, ni à une scène/prefab du flow.
  Toutes les interactions passent par l'API (`Show`, `SetContent`, `Close`) et les events.
- **TR2 — Gating porté par l'intégrateur.** La règle métier *« si un bloc est flaggé tutorial et
  qu'une scène a des instructions, l'overlay s'affiche — pour Proximal, une seule fois, au 1er
  trial du bloc »* n'est **pas** implémentée par le composant. L'intégrateur décide quand appeler
  `Show(...)` à partir de l'état de session (bloc courant, `current_trial_index`). Le composant
  est rejouable autant de fois que demandé.
- **TR3 — Pas de pause ni d'input lock gameplay.** Le composant n'altère pas `Time.timeScale` et
  n'appelle pas `GameManager`. Le fond modal bloque la souris ; le verrou clavier gameplay
  (Proximal) est orchestré par l'intégrateur autour de `Show()`/`Closed`.
- **TR4 — SFX au clic.** Le bouton OK porte un `UIButtonSound` avec un `SoundEffect` UI assigné.
- **TR5 — Input clavier/manette.** Pas d'`InputAction` custom. Navigation/validation laissées à
  l'EventSystem standard (focus initial sur OK, `Submit` hérité de la map UI existante).
- **TR6 — Visuel aligné conventions projet.** Sprites SCI-FI UI Pack Pro (cohérent
  `HowToPlayPanel`), TextMeshPro pour tout texte, layout Unity UI standard.
- **TR7 — Aucun nettoyage inter-trial.** Composant local à sa scène, instancié au chargement,
  détruit à la transition. Pas de singleton, pas de `DontDestroyOnLoad`, pas d'état partagé.

---

## 4. Matrice de traçabilité

**N/A pour la V1.** Le composant n'écrit aucune donnée dans `trial_responses` ni ailleurs (D6).
Un futur besoin (logguer l'affichage, la durée de lecture) serait traité par l'intégrateur via
abonnement aux events `Shown` / `Closed`, sans modification du composant.

---

## 5. Questions ouvertes

### Dans le scope du composant

Aucune. Toutes les décisions structurantes sont arbitrées (cf. §1 "Décisions déjà prises").

### Hors scope, listées pour mémoire à l'intention de l'intégrateur / du chercheur

| # | Question | Options | Impact | Urgence |
|:---|:---|:---|:---|:---|
| Q-ICT-1 | Origine de la donnée réelle (`Show` alimenté depuis quoi ?) | A: champs ajoutés à `BlockConfig` (back-end Supabase) / B: autre source | Pipeline data chercheur | À traiter avec le back-office chercheur |
| Q-ICT-2 | Condition exacte de déclenchement | A: présence de contenu pour la scène (le composant no-op si vide) / B: + gate explicite `is_tutorial` / C: gate `is_tutorial` + contenu | Cohérence protocole | À confirmer avec le chercheur |
| Q-ICT-3 | Tracking de la consultation dans `trial_responses` | A: aucun (V1) / B: vu oui/non + durée | Donnée de recherche | À évaluer avec le chercheur |
| Q-ICT-4 | Localisation multilingue | A: FR seul (V1) / B: clés i18n | Internationalisation | À évaluer si participants non-FR |
| Q-ICT-5 | Contenu final des instructions par scène | À fournir par les chercheurs | Onboarding contextuel effectif | À fixer avant lancement |
| Q-ICT-6 | Périmètre du verrou input sur Proximal pendant l'overlay | A: `GameManager.inputLocked` clavier / B: + autres systèmes (caméra, scan) | UX gameplay | À traiter à l'intégration |
