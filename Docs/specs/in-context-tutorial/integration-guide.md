# Guide d'intégration — Composant UI "In-Context Tutorial"

> Document à l'intention de **l'intégrateur** (branchement back-end + flow).
> Le composant `InContextTutorialUI` est livré agnostique et testé en isolation.
> Ce guide décrit comment le brancher dans le vrai jeu.
> **Mise à jour 2026-05-29 :** le scaffold d'intégration (étape 1) est désormais **EN PLACE**
> dans les 3 scènes — voir §0. Il reste à brancher la source de données back-end (§2) et à
> affiner le verrou input Proximal (§4).

**Date :** 2026-05-29
**Statut :** `draft`
**Lié à :** `spec-fonc.md`, `spec-tech.md`

---

## 0. État actuel — déjà en place dans les 3 scènes (étape 1)

Posé et testé le 2026-05-29 dans `AdvisorChoiceScene`, `DistalChoiceScene`, `ProximalScene` :
- Un Canvas dédié **`InContextTutorialCanvas`** (Screen Space-Overlay, `sortingOrder = 50`,
  CanvasScaler 1920×1080) — découplé des canvases de design existants, garantit le z-order.
- L'instance du prefab **`InContextTutorialPanel`** dessous.
- Un GameObject **`InContextTutorialBinder`** portant `InContextTutorialSceneBinder`
  (`Assets/Game/Scripts/UI/InContextTutorialSceneBinder.cs`), `_scene` réglé par scène,
  `_overlay` câblé, et un **contenu de test** placeholder (`_testTitle` / `_testBody`).

Le binder fait déjà, **pour de vrai** :
- **gating** : si `FlowController.Instance.CurrentBlock.is_tutorial == false` → aucun overlay ;
  sur Proximal, overlay seulement si `current_trial_index == 0` ;
- **verrou input** Proximal : `GameManager.SetInputLocked(true)` à l'ouverture, `false` à la fermeture ;
- **self-test éditeur** : sans `FlowController` (scène lancée seule), il affiche le contenu de test
  (désactivable via `_showTestContentWhenNoFlow`).

**Ce qu'il te reste à faire (branchement dynamique) :**
1. §2 — étendre `BlockConfig` avec les champs titre/texte par scène.
2. §4 — remplacer les ~2 lignes de `InContextTutorialSceneBinder.LoadContent()` (le `// TODO étape 2`)
   par la lecture de `BlockConfig.in_context_tutorial`, puis vider les `_testTitle` / `_testBody`.
3. Affiner la coordination du verrou input Proximal avec `GameManager.GetReadySequence` (cf. §4).

---

## 1. Rappel de l'API exposée

```csharp
public class InContextTutorialUI : MonoBehaviour
{
    public event Action Shown;
    public event Action Closed;

    public bool IsOpen     { get; }
    public bool HasContent { get; }

    public bool Show(string title, string body);  // définit + affiche ; false si vide
    public bool Show();                            // affiche le contenu déjà défini
    public void SetContent(string title, string body);
    public void Close();
}
```

Le composant ne décide **jamais** seul de s'afficher (sauf auto-show dummy en éditeur). C'est
l'intégrateur qui appelle `Show(...)` au bon moment. La règle « pas de contenu → pas d'overlay »
est gérée par le composant : tu peux appeler `Show(titre, texte)` sans condition, il ne fera rien
si les deux sont vides.

---

## 2. Étape A — Étendre la config back-end

Ajouter à `BlockConfig` (`Assets/Game/Scripts/Data/FlowDataModels.cs`) un sous-objet par bloc
décrivant les instructions in-context **par scène** :

```csharp
[Serializable]
public class InContextTutorialConfig
{
    public string advisor_title;
    public string advisor_text;
    public string distal_title;
    public string distal_text;
    public string proximal_title;
    public string proximal_text;

    public InContextTutorialConfig DeepClone() => new InContextTutorialConfig
    {
        advisor_title = advisor_title, advisor_text = advisor_text,
        distal_title  = distal_title,  distal_text  = distal_text,
        proximal_title = proximal_title, proximal_text = proximal_text
    };
}
```

Puis dans `BlockConfig` :

```csharp
public InContextTutorialConfig in_context_tutorial = new();
// ... et dans BlockConfig.DeepClone() :
in_context_tutorial = in_context_tutorial != null
    ? in_context_tutorial.DeepClone()
    : new InContextTutorialConfig(),
```

> Les champs étant des `string`, une scène sans instruction reçoit une chaîne vide → le composant
> n'affiche rien (R1/R4). Aucun flag supplémentaire n'est requis côté composant. Si le protocole
> veut un garde-fou explicite, gater en plus sur `BlockConfig.is_tutorial` (cf. Q-ICT-2).

---

## 3. Étape B — Poser le prefab dans chaque scène

> ✅ **Fait (2026-05-29) — cf. §0.** Posé sous un Canvas dédié `InContextTutorialCanvas`
> (`sortingOrder = 50`) au lieu du Canvas existant, pour découpler des variantes de design et
> fixer le z-order. Les `_dummyTitle` / `_dummyBody` de l'instance sont vidés. La procédure
> manuelle ci-dessous reste pour référence / nouvelle scène.

Dans `AdvisorChoiceScene`, `DistalChoiceScene`, `ProximalScene` :
1. Glisser `InContextTutorialPanel.prefab` sous le `Canvas` de la scène (en dernier enfant pour
   passer au-dessus du reste).
2. Vider les champs `_dummyTitle` / `_dummyBody` de l'instance (la dummy ne sert qu'en sandbox ;
   en prod le contenu vient de `Show(...)`).
3. Référencer l'instance dans le contrôleur de scène (ci-dessous).

---

## 4. Étape C — Contrôleur de scène (déclenchement)

> ✅ **Réalisé par `InContextTutorialSceneBinder`** (`Assets/Game/Scripts/UI/InContextTutorialSceneBinder.cs`),
> déjà posé dans les 3 scènes (cf. §0). Il fait le gating réel (`is_tutorial`, 1er trial Proximal)
> et le verrou input. **Ta seule action** : remplacer les ~2 lignes de sa méthode `LoadContent()`
> (marquées `// TODO étape 2`) par la lecture de `BlockConfig.in_context_tutorial` (code ci-dessous),
> puis vider les `_testTitle` / `_testBody`. Les exemples ci-dessous montrent la cible.

Modèle (calqué sur `IntroSceneController`). **Advisor** et **Distal** : afficher à l'entrée
(de fait 1×/bloc, car ces scènes ne se rejouent pas au sein d'un bloc).

```csharp
public class AdvisorTutorialController : MonoBehaviour
{
    [SerializeField] private InContextTutorialUI _overlay;

    void Start()
    {
        var block = FlowController.Instance?.CurrentBlock;
        if (_overlay == null || block?.in_context_tutorial == null) return;

        var cfg = block.in_context_tutorial;
        _overlay.Show(cfg.advisor_title, cfg.advisor_text);  // no-op si vide
    }
}
```

> Pour Distal : idem avec `distal_title` / `distal_text`.

**Proximal** : la scène se recharge à **chaque trial** du bloc. Gater sur le 1er trial pour ne
l'afficher qu'une fois par bloc, et verrouiller le clavier gameplay le temps de la lecture :

```csharp
public class ProximalTutorialController : MonoBehaviour
{
    [SerializeField] private InContextTutorialUI _overlay;

    void Start()
    {
        var fc = FlowController.Instance;
        var block = fc?.CurrentBlock;
        if (_overlay == null || block?.in_context_tutorial == null) return;

        // 1er trial du bloc seulement (réf. PlayerSessionState.current_trial_index)
        if (fc.State.current_trial_index != 0) return;

        var cfg = block.in_context_tutorial;
        bool shown = _overlay.Show(cfg.proximal_title, cfg.proximal_text);
        if (!shown) return;

        // Verrou clavier gameplay tant que l'overlay est ouvert (le fond modal ne bloque que la souris)
        if (GameManager.Instance != null) GameManager.Instance.SetInputLocked(true);
        _overlay.Closed += OnOverlayClosed;
    }

    void OnOverlayClosed()
    {
        _overlay.Closed -= OnOverlayClosed;
        if (GameManager.Instance != null) GameManager.Instance.SetInputLocked(false);
    }
}
```

> ⚠️ **`inputLocked` a un setter privé** → utiliser `GameManager.SetInputLocked(bool)` (comme
> `SettingsPanelUI`). Le binder le fait déjà correctement.
>
> ⚠️ **Indexation** : `current_trial_index` est 0-based, remis à 0 en début de bloc
> (`FlowController.AdvanceToNextBlockOrEnd` / init), incrémenté dans `OnTrialComplete`. Donc
> `== 0` = 1er trial du bloc. ✔️ confirmé.
>
> ⚠️ **Coordination du verrou input vs `GetReadySequence`** : à l'entrée de Proximal,
> `GameManager.BeginFirstRound()` lock l'input puis lance `GetReadySequence` qui le **déverrouille
> après `getReadyDuration`**, indépendamment de l'overlay. Le binder verrouille à l'ouverture et
> déverrouille à la fermeture, mais ces deux mécanismes peuvent se chevaucher (le joueur pourrait
> bouger au clavier si l'overlay reste ouvert au-delà du « Get Ready »). Coordination propre à
> décider : p.ex. retarder `BeginFirstRound()` jusqu'à la fermeture de l'overlay, ou laisser
> l'overlay piloter le démarrage du round. **Non résolu dans le scaffold — à ton appréciation.**

---

## 5. Recette de test offline du vrai flux (sans back-end)

**Constat.** Le boot exige aujourd'hui une config back-end : `FlowController` appelle
`ApiClient.FetchSessionConfig(...)` et, si le fetch échoue, fait `LogError` + `yield break` (le
flow ne démarre pas). Il n'existe aucun chemin offline pour parcourir les scènes.

Pour parcourir les **vraies scènes** avec un bloc tutorial simulé **sans back-end** :

1. **Hook dev dans le boot.** Dans la séquence de boot du `FlowController`, avant le fetch :
   ```csharp
   #if UNITY_EDITOR
   if (_useLocalConfig && _localConfig != null)
   {
       Initialize(_localConfig);          // _localConfig : SessionConfig sérialisé / asset JSON
       AdvanceToPhase(GamePhase.Welcome);
       yield break;                       // court-circuite FetchSessionConfig
   }
   #endif
   ```
   (`_useLocalConfig` : bool Inspector ; `_localConfig` : un `SessionConfig` éditable en éditeur,
   ou chargé depuis un `TextAsset` JSON via `JsonUtility.FromJson`.)

2. **Config locale de test.** Fournir un `SessionConfig` minimal :
   - 1 bloc `is_tutorial = true`, `trial_count >= 2` (pour vérifier le « 1er trial only »),
   - `in_context_tutorial` renseigné pour les 3 scènes (advisor/distal/proximal).

3. **Validation attendue :**
   - Advisor → overlay affiché ; Distal → overlay affiché ;
   - Proximal trial 0 → overlay affiché + clavier verrouillé jusqu'au clic OK ;
   - Proximal trial 1 (rechargement) → **pas** d'overlay.

> Ce hook est utile au-delà du tutorial (tester tout le jeu offline). Le garder `#if UNITY_EDITOR`
> ou derrière un flag pour ne jamais l'embarquer en build prod.

---

## 6. Comment le porteur teste sa partie (rappel)

Sans rien intégrer, le porteur valide le composant via
`Assets/Game/Scenes/Sandboxes/Florian/InContextTutorialSandbox.unity` :
- **Contenu/visuel** : `_dummyTitle` / `_dummyBody` + `InContextTutorialTestRunner` (ContextMenu).
- **Logique de gating** : `InContextTutorialSceneArrivalDemo` reproduit la décision de l'étape C
  (`tutorial && firstTrial && hasContent`) avec des toggles Inspector, sans `FlowController`.

---

## 7. Questions ouvertes pour l'intégrateur / le chercheur

Voir `spec-fonc.md` §5 (Q-ICT-1 à Q-ICT-6) : source data, condition exacte de déclenchement,
tracking, localisation, contenu final, périmètre du verrou input.
