# Guide d'intégration — Composant UI "How To Play"

> Document de référence pour l'intégrateur qui branchera le composant `HowToPlayUI` dans le flow de production.
> Produit après la livraison du composant en sandbox isolée, en amont du chantier d'intégration.

**Date :** 2026-05-28
**Statut :** `draft`
**Chantier :** UI — Composant "How To Play" (étape d'intégration)
**Spec fonc associée :** `Docs/specs/how-to-play/spec-fonc.md`
**Spec tech associée :** `Docs/specs/how-to-play/spec-tech.md`

---

## 1. Contexte

Le composant `HowToPlayUI` a été livré et validé dans une sandbox isolée (voir `Assets/Game/Scenes/Sandboxes/Florian/HowToPlaySandbox.unity`). Il expose une API publique propre — `SetPages(IList<HowToPlayPage>)`, `Open()`, `Close()`, events `Closed`/`Completed` — et fonctionne en autonomie avec dummy data Inspector.

Deux étapes restent pour le brancher en production, **explicitement hors scope du composant lui-même** :

1. **Intégration dans `IntroScene`** à la place de l'écran "Continue" actuel
2. **Branchement runtime sur les données du back-end** (les chercheurs éditent le contenu depuis leur back-office sans rebuild WebGL)

Ce document décrit pas à pas la marche à suivre, en s'appuyant sur les patterns déjà en place dans le projet.

---

## 2. Intégration dans IntroScene

### 2.1 État actuel de la scène

`Assets/Game/Scenes/GameScenes/IntroScene.unity` contient aujourd'hui :

- `Main Camera`, `Directional Light`, `EventSystem`, `SceneMusic` (composants standard)
- `Canvas` (Screen Space Overlay) avec le script `FlowContinueScreenUI` posé dessus, en mode `ScreenKind.Intro`
- Un bouton enfant "Continue" sous le Canvas

Le flux actuel : clic "Continue" → `FlowContinueScreenUI.OnContinueClicked()` → `FlowController.Instance.OnPhaseComplete()` → transition vers `GamePhase.AdvisorChoice`.

C'est précisément ce flux qu'il faut **remplacer** par `HowToPlayUI`, en gardant le même point de sortie (`OnPhaseComplete`).

### 2.2 Étapes d'intégration

**Étape 1 — Retirer `FlowContinueScreenUI` du Canvas d'IntroScene**

Deux approches équivalentes :
- Supprimer le composant `FlowContinueScreenUI` et son bouton "Continue" enfant directement
- Ou désactiver le GameObject Canvas existant et en créer un nouveau dédié à `HowToPlayUI`

Le script `FlowContinueScreenUI` lui-même est conservé dans le projet (il sert encore aux modes `Welcome` et `EndSession`). Le mode `Intro` de l'enum peut être marqué obsolète ou laissé tel quel sans impact.

**Étape 2 — Instancier le prefab `HowToPlayPanel`**

Glisser `Assets/Game/Prefabs/UI/HowToPlay/HowToPlayPanel.prefab` dans la scène, comme enfant d'un Canvas (Screen Space Overlay).

`_dummyPages` doit rester vide sur l'instance — c'est volontairement vide sur le prefab livré, le contenu réel viendra du back-end (§3).

**Étape 3 — Créer un script controller pour IntroScene**

C'est la **glue** qui fait le pont entre le composant agnostique et le flow. Nouveau fichier `Assets/Game/Scripts/UI/IntroSceneController.cs` (ou un nom équivalent), posé sur un GameObject de la scène :

```csharp
public class IntroSceneController : MonoBehaviour
{
    [SerializeField] private HowToPlayUI _howToPlay;

    void Start()
    {
        if (_howToPlay == null) return;

        // 1. Charger les pages depuis le back (cf. §3)
        var pages = LoadPagesFromBackend();

        if (pages == null || pages.Count == 0)
        {
            // Fallback : pas de pages → skip silencieux pour ne pas bloquer le flow
            Debug.LogWarning("[IntroSceneController] Pas de pages tutorial. Skip phase Intro.");
            FlowController.Instance?.OnPhaseComplete();
            return;
        }

        // 2. Pousser dans le composant + ouvrir
        _howToPlay.SetPages(pages);
        _howToPlay.Closed += OnTutorialClosed;
        _howToPlay.Open();
    }

    void OnDestroy()
    {
        if (_howToPlay != null) _howToPlay.Closed -= OnTutorialClosed;
    }

    private void OnTutorialClosed()
    {
        FlowController.Instance?.OnPhaseComplete();
    }

    private IList<HowToPlayPage> LoadPagesFromBackend()
    {
        // Implémentation §3
        return null;
    }
}
```

Trois points importants :

- **Aucune modification de `FlowController` ni de `HowToPlayUI`** — le controller s'insère entre les deux sans toucher au composant ni au singleton orchestrateur. C'est le respect strict de la frontière qu'on a tracée dans la spec.
- **Désabonnement obligatoire en `OnDestroy`** (cohérent avec les patterns du projet — cf. `RoundUI`).
- **Pattern identique à `FlowContinueScreenUI.OnContinueClicked`** : la sortie de phase n'a pas changé, on remplace juste le déclencheur (avant : un clic ; maintenant : la fermeture du tutoriel).

**Étape 4 — Tester en deux temps**

- D'abord lancer **IntroScene directement** depuis l'Editor avec des pages câblées via dummy Inspector (au lieu de l'appel back) — le composant doit s'ouvrir, naviguer, se fermer ; le log "FlowController.Instance is null" est attendu et géré par le `?.` (FlowController vient de BootScene).
- Puis tester via le flow complet **BootScene → ... → IntroScene → AdvisorChoice** : à la fermeture du tutoriel, la scène doit transitionner vers AdvisorChoice via le fade noir existant.

### 2.3 Ce qui reste inchangé dans le projet

- `HowToPlayUI.cs`, `HowToPlayPanel.prefab`, `HowToPlayDot.prefab` — aucun changement
- `FlowController.cs`, `FlowDataModels.cs` — aucun changement (sauf si choix Option A en §3.3)
- `FlowContinueScreenUI.cs` — aucun changement (reste utile pour Welcome/EndSession)
- `BootScene`, `WelcomeScene`, `ConsentScene`, etc. — aucun changement

Seul ajout au code projet : `IntroSceneController.cs`. Seule scène modifiée : `IntroScene.unity`.

### 2.4 Pré-requis sur la scène hôte — `CanvasScaler` calibré 1920×1080

Le prefab `HowToPlayPanel` a été calibré dans la sandbox avec un Canvas en mode **`ScaleWithScreenSize / 1920×1080 / match 0.5`**. Tous ses `sizeDelta` absolus (`Content` 1300×820, `Footer` 1300×140, boutons 240×90 et 200×80, etc.) supposent ce référentiel. Si la scène hôte n'a pas un Canvas configuré de la même façon, Unity tente automatiquement de **conserver la rect visuelle du prefab** lors de l'instanciation et fabrique des **overrides anormaux** sur la `RectTransform` de l'instance (`sizeDelta` qui devient `-1920, -1080`, `anchoredPosition` qui devient `-960, -540`). Le résultat est une rect calculée négative sur tout Canvas plus petit que 1920×1080 → boutons projetés, contenu invisible ou mal placé en Game View.

**Conclusion** : avant de poser le prefab dans une scène hôte, vérifier que son `Canvas` + `CanvasScaler` racine sont configurés ainsi :

| Composant | Champ | Valeur attendue |
|:---|:---|:---|
| `Canvas` | `m_RenderMode` | `0` (Screen Space - Overlay) |
| `CanvasScaler` | `m_UiScaleMode` | `1` (ScaleWithScreenSize) |
| `CanvasScaler` | `m_ReferenceResolution` | `{x: 1920, y: 1080}` |
| `CanvasScaler` | `m_MatchWidthOrHeight` | `0.5` |

C'est exactement le pattern de **`BootScene`** et de la **sandbox**. La plupart des autres scènes du projet sont encore en `ConstantPixelSize / 800×600` (héritage du template Unity par défaut) — c'est une dette de cohérence visuelle non bloquante, mais qui implique que **chaque scène où on pose le prefab doit d'abord être alignée**, sinon le rendu se casse comme décrit ci-dessus.

Si la modification est faite a posteriori (après instanciation du prefab), il faut aussi **reset les overrides anormaux** de la `RectTransform` de l'instance : `anchoredPosition` à `(0, 0)`, `sizeDelta` à `(0, 0)`. C'est équivalent à un "Revert" sur ces deux propriétés depuis le menu des prefab overrides Unity.

---

## 3. Branchement runtime aux données back-end

### 3.1 Comment les données arrivent dans le projet aujourd'hui

Le bootstrap actuel suit ce pipeline (vérifié dans `FlowController.cs` + `ApiClient.cs` + `FlowDataModels.cs`) :

```
BootScene.Start
  └─ FlowController.BootstrapFlow (coroutine)
     ├─ extrait sessionId des args CLI (ou _editorSessionId pour le dev)
     ├─ ApiClient.FetchSessionConfig(sessionId)
     │   └─ GET /api/sessions/{sessionId}
     │   └─ JsonUtility.FromJson<SessionConfig>(response)
     └─ FlowController.Initialize(config)
         ├─ stocke FlowController.Instance.Config (SessionConfig)
         └─ crée FlowController.Instance.State (PlayerSessionState)

Welcome → Consent → Intro → ...   (Config est déjà en mémoire à ce stade)
```

**Conséquence clé** : quand IntroScene se charge, `FlowController.Instance.Config` est **déjà disponible globalement**. C'est le point d'extension naturel pour les pages du tutoriel.

### 3.2 Trois options de plumbing — décision à prendre avec le back

| Option | Principe | Quand l'utiliser |
|:---|:---|:---|
| **A — Étendre `SessionConfig`** | Ajouter un champ `intro_pages` au payload de `/api/sessions/{sessionId}` | Le contenu peut varier par session (ex: pilote vs final) |
| **B — Endpoint dédié** | Nouveau `GET /api/intro-pages` séparé | Le contenu est global et stable |
| **C — `StreamingAssets/intro-pages.json`** | Fichier embarqué dans le build | **Déconseillée** : ne répond pas au besoin "édition sans rebuild" |

**Recommandation** : Option A si tu veux pouvoir adapter le contenu selon les conditions expérimentales, Option B sinon. La décision est plus une question de modèle de données chercheur que technique.

### 3.3 Implémentation — Option A (recommandée par défaut)

**Étape 1 — Ajouter le DTO et le champ dans `FlowDataModels.cs`**

```csharp
[Serializable]
public class HowToPlayPageDto
{
    public string image_key;   // clé qui résout en Sprite (cf. §3.5)
    public string header;
    public string body;
}
```

Puis dans `SessionConfig` :

```csharp
[Serializable]
public class SessionConfig
{
    public string session_template_id;
    public List<BlockConfig> blocks;
    public List<HowToPlayPageDto> intro_pages;  // <-- nouveau
}
```

`JsonUtility.FromJson` désérialisera automatiquement le nouveau champ si présent, et le laissera null/vide si absent (compatibilité descendante avec les sessions sans tutoriel configuré).

**Étape 2 — Lire dans `IntroSceneController.LoadPagesFromBackend`**

```csharp
private IList<HowToPlayPage> LoadPagesFromBackend()
{
    var config = FlowController.Instance?.Config;
    if (config?.intro_pages == null || config.intro_pages.Count == 0)
        return null;

    var pages = new List<HowToPlayPage>(config.intro_pages.Count);
    foreach (var dto in config.intro_pages)
    {
        pages.Add(new HowToPlayPage
        {
            image = ResolveSprite(dto.image_key),
            header = dto.header,
            body = dto.body
        });
    }
    return pages;
}
```

Le retour null déclenche le skip silencieux décrit en §2.2, donc le flow continue même si le back ne fournit pas de pages.

### 3.4 Implémentation — Option B (si endpoint dédié)

- Nouvelle méthode `ApiClient.FetchIntroPagesCoroutine(callback, errorCallback)` calquée sur `FetchSessionConfigCoroutine` (lignes 219-242 de `ApiClient.cs`)
- `IntroSceneController.Start()` devient une coroutine qui appelle `ApiClient.Instance.FetchIntroPages(...)`, attend la réponse, puis fait `SetPages` + `Open`
- Tant que la réponse n'est pas arrivée, garder le panel fermé ou afficher un loader simple

Le reste (DTO, ResolveSprite, OnTutorialClosed → OnPhaseComplete) est identique à l'Option A.

### 3.5 Résolution des sprites — `ResolveSprite(string key)`

Le DTO porte une `string` (un nom logique, ex: `"intro_page_movement"`). Côté Unity il faut convertir en `Sprite`. Trois approches possibles, par ordre de simplicité :

| Approche | Mise en œuvre | Quand l'utiliser |
|:---|:---|:---|
| **`Resources.Load`** | Sprites placés dans `Assets/Game/Resources/HowToPlay/{key}.png`. `Resources.Load<Sprite>("HowToPlay/" + key)` | V1 simple, sprites versionnés dans le repo avec le client. Le back ne fournit que la clé. |
| **AssetBundle distant** | Le back uploade un AssetBundle, le client le télécharge au boot, résolu via `bundle.LoadAsset<Sprite>(key)` | Si les chercheurs veulent **ajouter des images sans rebuild client**. |
| **Téléchargement runtime de PNG** | `UnityWebRequestTexture.GetTexture(url)` puis `Sprite.Create(...)` | Si le DTO porte directement une `image_url`. Le plus flexible mais latence visible au chargement. |

**Recommandation V1 : `Resources.Load`.** Suffit pour démarrer, zéro infrastructure supplémentaire. Si plus tard les chercheurs veulent uploader leurs propres images depuis leur dashboard sans rebuild, basculer sur AssetBundle ou URL directe — `HowToPlayUI` ne changera pas, seul `ResolveSprite` évolue.

---

## 4. Ordre d'exécution recommandé

1. **Aligner avec le back** sur Option A vs B (déclencheur : qui édite, à quelle granularité)
2. **Côté Unity, faire d'abord la partie sans données réelles** : créer `IntroSceneController`, poser le prefab dans IntroScene, retirer `FlowContinueScreenUI`, brancher l'event `Closed` → `OnPhaseComplete`. Tester avec un `LoadPagesFromBackend` qui retourne une **liste hardcodée** de 2-3 pages. Vérifier que le flow complet Boot → ... → AdvisorChoice fonctionne.
3. **Ajouter la couche données** : DTO + lecture depuis Config (A) ou nouvel endpoint (B) + `ResolveSprite`.
4. **Côté back** : ajouter le champ (A) ou l'endpoint (B), tester le payload en local.
5. **Test end-to-end** : modifier le JSON côté back, vérifier que le contenu visible dans Unity change sans rebuild.

---

## 5. Fichiers à toucher / créer (récap)

| Fichier | Action |
|:---|:---|
| `Assets/Game/Scenes/GameScenes/IntroScene.unity` | **Modifier** : retirer `FlowContinueScreenUI`/Continue, poser le prefab `HowToPlayPanel`, ajouter le GameObject controller |
| `Assets/Game/Scripts/UI/IntroSceneController.cs` | **Créer** — la glue entre `HowToPlayUI` et `FlowController` |
| `Assets/Game/Scripts/FlowDataModels.cs` | **Modifier** (Option A) : ajouter `HowToPlayPageDto` + champ `intro_pages` dans `SessionConfig` |
| `Assets/Game/Scripts/ApiClient.cs` | **Modifier** (Option B uniquement) : ajouter `FetchIntroPages` |
| `Assets/Game/Resources/HowToPlay/*.png` | **Créer** (si `Resources.Load`) — les sprites du tutoriel |

`HowToPlayUI.cs` et les prefabs livrés (`HowToPlayPanel.prefab`, `HowToPlayDot.prefab`) restent **inchangés**.

---

## 6. Vérification end-to-end

À tester une fois branché :

- [ ] Lancer depuis BootScene avec un `sessionId` valide → IntroScene s'ouvre et affiche les pages du back
- [ ] À la fermeture du tutoriel (Close button ou Next sur dernière page) → transition fade noir vers AdvisorChoice
- [ ] Modifier le JSON / la config back (ajouter/retirer une page) → vérifier que Unity reflète sans rebuild
- [ ] Lancer IntroScene seule dans l'Editor sans Boot → fallback (skip silencieux) ne bloque pas
- [ ] `FlowController.Instance.State.is_tutorial` cohérent au moment où le HowToPlay est affiché
- [ ] WebGL build : pas de régression
- [ ] L'event `Completed` est émis quand le joueur a vu toutes les pages (utile si on veut tracker plus tard)

---

## 7. Questions ouvertes pour l'intégration

| # | Question | Options | Urgence |
|:---|:---|:---|:---|
| Q-INT-1 | Option A vs B vs autre pour la source des données | A: étendre SessionConfig / B: endpoint dédié / C: autre | À décider avec le back-end dev avant Étape 3 du §4 |
| Q-INT-2 | Mode de résolution des sprites | Resources.Load (V1) / AssetBundle / URL directe | À décider selon le besoin chercheur d'uploader des images sans rebuild |
| Q-INT-3 | Fallback si pas de pages configurées | Skip silencieux (proposé) / écran d'erreur / page par défaut | À valider avec le chercheur |
| Q-INT-4 | Tracking de la consultation tutoriel | Aucun (V1, cf. spec fonc) / vue oui/non / log détaillé | À évaluer si le chercheur exprime un besoin de mesure |
| Q-INT-5 | Localisation multilingue | FR seul (V1) / clés i18n dès maintenant | À évaluer selon la population cible |

---

## 8. Journal d'avancement de l'intégration

Entrées append-only, du plus récent au plus ancien. Sert à l'intégrateur back-end pour savoir exactement où le porteur s'est arrêté et ce qui reste à faire.

### 2026-05-28 — Étape 1 terminée : composant posé dans IntroScene avec dummy data, en attente du branchement back

**Par :** @fsorco

**Ce qui est fait :**
- `IntroSceneController.cs` créé dans `Assets/Game/Scripts/UI/` — glue entre `HowToPlayUI` et `FlowController`. Méthode `LoadPages()` retourne actuellement `_testPages` (champ `[SerializeField] List<HowToPlayPage>` Inspector) — le TODO de remplacement par lecture `FlowController.Config.intro_pages` est explicite dans le commentaire de la méthode.
- `IntroScene.unity` modifiée :
  - `FlowContinueScreenUI` retiré du Canvas
  - Bouton "Continue" enfant supprimé
  - Prefab `HowToPlayPanel.prefab` instancié sous le Canvas
  - GameObject `IntroSceneController` créé avec ref `_howToPlay` → instance du prefab + 3 pages dummy hardcodées dans `_testPages` ("Bienvenue", "Te deplacer", "C'est parti !")
  - **CanvasScaler aligné** sur 1920×1080 ScaleWithScreenSize match 0.5 (cf. §2.4) — c'était `ConstantPixelSize 800×600` avant
  - **Overrides RectTransform** de l'instance prefab remis à `(0, 0) / (0, 0)` (cf. §2.4) — Unity les avait fabriqués à `(-960, -540) / (-1920, -1080)` à l'instanciation

**Ce qui reste pour l'étape 2 (branchement back-end)** :
- Décider Option A vs B (cf. §3.2) avec le back-end dev
- Implémenter le DTO `HowToPlayPageDto` dans `FlowDataModels.cs` (Option A) **ou** la méthode `ApiClient.FetchIntroPages` (Option B)
- Implémenter `ResolveSprite(string key)` selon le choix retenu (Resources.Load V1 conseillé, cf. §3.5)
- Remplacer le corps de `LoadPages()` dans `IntroSceneController.cs` : aujourd'hui `return _testPages;`, demain → lecture depuis Config + mapping DTO → `HowToPlayPage` + `ResolveSprite`
- Une fois le branchement back fonctionnel, on peut retirer le champ `_testPages` du controller (ou le garder comme fallback dev silencieux, à arbitrer)
- Trancher les Q-INT-1 à Q-INT-5 du §7

**Tests passés à cette étape :**
- IntroScene lancée seule depuis l'Editor → panel s'auto-affiche avec les 3 dummy pages, navigation OK, fermeture OK (FlowController.Instance null = no-op silencieux grâce au `?.`)
- Aucune exception console en Play Mode
- Layout visuel conforme à la sandbox après correction du CanvasScaler

**Tests qui restent à faire avant clôture du chantier :**
- Flow complet BootScene → Welcome → Consent → IntroScene → AdvisorChoice avec un `sessionId` valide
- WebGL build
- Une fois étape 2 faite : édition du JSON back-end → vérifier reflet en Unity sans rebuild

**Commits associés :**
- `a6ddbc59` (livraison composant + sandbox, branche `ui/how-to-play-component`) — déjà pushé
- L'étape 1 d'intégration n'est **pas encore commitée** au moment de l'écriture de cette entrée (changements en `git status` : `IntroScene.unity` modifié, `IntroSceneController.cs` + .meta créés)

**Décisions techniques actées à cette étape :**
- Le composant `HowToPlayUI` reste strictement inchangé (objectif "agnostique" tenu)
- `FlowController`, `ApiClient`, `FlowDataModels` restent strictement inchangés à cette étape (changements prévus uniquement à l'étape 2)
- `FlowContinueScreenUI` n'est pas supprimé — il reste utilisé par WelcomeScene et EndSessionScene
- `_testPages` Inspector dans le controller est un choix transitoire : il permet à l'étape 1 d'être testable visuellement de bout en bout sans dépendre du back, et facilite la bascule vers les vraies données à l'étape 2 (changement isolé dans `LoadPages()`)
