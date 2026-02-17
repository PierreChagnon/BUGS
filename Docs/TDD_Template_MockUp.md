# **Technical Design Document**

| Nom du projet :    | \[Nom de ton jeu\]   |
| :----------------- | :------------------- |
| **Version :**      | 1.0                  |
| **Dernière MAJ :** | \[Date\]             |
| **Auteur(s) :**    | \[Ton nom / Équipe\] |
| **Moteur :**       | Unity \[Version\]    |
| **Langage :**      | C\#                  |

# **1\. Vue d'ensemble du projet**

## **1.1 Résumé technique**

Brève description du jeu d'un point de vue technique (genre, plateforme cible, échelle du projet).

**_Exemple :_**

Jeu d'action-aventure en 2D, single-player, ciblant PC/Console.

Basé sur Unity 2022.3 LTS avec un système de combat en temps réel

et un système de progression par compétences.

## **1.2 Objectifs techniques prioritaires**

- Performance ciblée : 60 FPS constant sur \[plateforme\]
- Modularity : systèmes découplés et réutilisables
- Maintenabilité : code lisible, patterns reconnus
- Scalabilité : architecture permettant l'ajout de contenu

## **1.3 Contraintes techniques**

- Limite mémoire : \[ex. 2GB RAM\]
- Taille du build : \[ex. \<500MB\]
- Compatibilité : \[PC Windows 10+, Steam Deck, etc.\]

# **2\. Architecture globale du projet**

## **2.1 Structure des dossiers**

Assets/

├── \_Project/

│ ├── Scripts/

│ │ ├── Core/ \# Systèmes fondamentaux (GameManager, etc.)

│ │ ├── Gameplay/ \# Mécaniques de jeu

│ │ ├── UI/ \# Interfaces utilisateur

│ │ ├── AI/ \# Intelligence artificielle

│ │ └── Utilities/ \# Helpers, extensions

│ ├── Prefabs/

│ ├── ScriptableObjects/

│ ├── Scenes/

│ └── Settings/

├── Art/

├── Audio/

└── Plugins/

## **2.2 Diagramme d'architecture système**

**🔗 Lien vers Figma :** \[Insérer URL de ton diagramme\]

**Description textuelle (pour backup) :**

┌─────────────────┐

│ Game Manager │ ← Singleton, gère états du jeu

└────────┬────────┘

         │

    ┌────┴────┬─────────┬──────────┐

    │         │         │          │

┌───▼───┐ ┌──▼──┐ ┌───▼────┐ ┌───▼────┐

│Combat │ │ UI │ │Inventory│ │ Audio │

│System │ │Mgr │ │ System │ │Manager │

└───────┘ └─────┘ └─────────┘ └────────┘

## **2.3 Patterns utilisés**

| Pattern       | Utilisation               | Justification                          |
| :------------ | :------------------------ | :------------------------------------- |
| Singleton     | GameManager, AudioManager | Point d'accès global, existence unique |
| Observer      | Event System              | Découplage entre systèmes              |
| Object Pool   | Projectiles, VFX          | Optimisation mémoire/performance       |
| State Machine | IA ennemis, Player States | Gestion claire des transitions d'état  |
| Command       | Input System              | Undo/Redo, replay, rebinding           |

# **3\. Systèmes de gameplay**

## **3.1 \[Système 1 : Combat\]**

### **3.1.1 Responsabilités**

- Détection des hits (raycast, colliders, hitboxes)
- Calcul des dégâts (formules, résistances, critiques)
- Feedback visuel et audio
- Gestion des invincibilité frames (i-frames)

### **3.1.2 Composants clés (Data Model)**

→ **CombatSytem.cs** : Singleton coordonnant les évènements de combat.

// Exemple de structure

public class CombatSystem : MonoBehaviour

{

    private HitDetector \_hitDetector;

    private DamageCalculator \_damageCalculator;

    private CombatFeedback \_feedback;

}

| Variable / Méthode | Type         | Description                                        |
| :----------------- | :----------- | :------------------------------------------------- |
| inventorySize      | int          | Nombre de slots maximum (Défaut: 20\)              |
| itemList           | List\<Item\> | Conteneur des instances d'objets                   |
| UseItem(int id)    | Méthode      | Logique de consommation \+ suppression de la liste |

→ **AttackCombo.cs** : ScriptableObject définissant les attaques disponible.

### **3.1.3 Dépendances**

- **Nécessite :** Health System, Animation Controller
- **Communique avec :** UI (health bars), Audio Manager
- **Déclenche :** Events (OnDamageDealt, OnEntityDeath)

### **3.1.4 Diagramme de flux (Data Flow)**

**🔗 Lien Figma :** \[Combat Flow Diagram\]

**Alternative texte :**

Player Input → Detect Attack → Check Hit → Calculate Damage

→ Apply to Target → Trigger Feedback → Update UI

### **3.1.5 Formules et règles métier**

Dégâts finaux \= (ATK × Multiplicateur) \- DEF

Chance critique \= BASE_CRIT \+ (LUCK / 10\)

I-Frames duration \= 0.5s après hit

### **3.1.6 Points d'attention**

- **⚠️ Performance :** Limiter les raycasts par frame (max 5\)
- **⚠️ Edge case :** Que se passe-t-il si 2 attaques touchent simultanément ?
- **🔧 À optimiser :** Pooling des hitbox colliders

### **3.1.7 Journal d’Implémentation**

| Date     | Développeur | Note / Décision Technique                                                          |
| :------- | :---------- | :--------------------------------------------------------------------------------- |
| 16/02/26 | @Dev1       | Passage en ScriptableObjects pour faciliter la création d'items par les designers. |
| 20/02/26 | @Dev2       | Correction bug : le poids total ne se mettait pas à jour si on jetait l'objet.     |

## **3.2 \[Système 2 : Inventory\]**

### **3.2.1 Responsabilités**

- Stocker et organiser les objets ramassés par le joueur
- Gérer les stacks d'objets et la limite de slots
- Exposer les actions de manipulation (utiliser, jeter, équiper)
- Persister l'état de l'inventaire via le SaveSystem

### **3.2.2 Composants clés (Data Model)**

→ **InventorySystem.cs** : MonoBehaviour gérant la logique de l'inventaire.

```csharp
public class InventorySystem : MonoBehaviour
{
    [SerializeField] private int _maxSlots = 20;
    private List<InventorySlot> _slots;
}
```

| Variable / Méthode     | Type                  | Description                                      |
| :--------------------- | :-------------------- | :----------------------------------------------- |
| \_maxSlots             | int                   | Nombre de slots maximum (Défaut : 20)            |
| \_slots                | List\<InventorySlot\> | Conteneur des slots actifs                       |
| AddItem(ItemData, int) | Méthode               | Ajoute un item, gère le stacking automatique     |
| RemoveItem(string id)  | Méthode               | Retire un item par son ID                        |
| UseItem(string id)     | Méthode               | Logique de consommation \+ déclenchement d'effet |

→ **ItemData.cs** : ScriptableObject définissant les données statiques d'un item.

```csharp
[CreateAssetMenu(fileName = "Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemID;
    public string itemName;
    public Sprite icon;
    public ItemType type;
    public int maxStack;
}
```

→ **InventorySlot.cs** : Structure représentant un slot avec son item et sa quantité.

```csharp
public class InventorySlot
{
    public ItemData Item { get; private set; }
    public int Quantity { get; private set; }
}
```

### **3.2.3 Dépendances**

- **Nécessite :** ItemData (ScriptableObjects), SaveSystem
- **Communique avec :** UI (InventoryPanel), CombatSystem (équipement actif)
- **Déclenche :** Events (OnItemAdded, OnItemRemoved, OnInventoryFull)

### **3.2.4 Diagramme de flux (Data Flow)**

**🔗 Lien Figma :** \[Inventory Flow Diagram\]

**Alternative texte :**

```
Player Pickup → Check Stack → Slot Available ?
→ [Oui] Add to Slot → Update UI
→ [Non] Inventory Full → Trigger OnInventoryFull → Notify Player
```

### **3.2.5 Formules et règles métier**

```
Slot disponible   = _maxSlots - slots occupés
Stack possible    = Quantity + incoming <= ItemData.maxStack
Poids total       = Σ (Item.weight × Quantity) pour chaque slot
```

### **3.2.6 Points d'attention**

- **⚠️ Edge case :** Item ajouté alors que l'inventaire est plein mais qu'un stack partiel existe
- **⚠️ Edge case :** Suppression d'un item équipé actuellement par le joueur
- **🔧 À optimiser :** Rafraîchissement UI — ne redessiner que le slot modifié, pas tout le panel

### **3.2.7 Journal d'implémentation**

| Date     | Développeur | Note / Décision Technique                                                          |
| :------- | :---------- | :--------------------------------------------------------------------------------- |
| 16/02/26 | @Dev1       | Passage en ScriptableObjects pour faciliter la création d'items par les designers. |
| 20/02/26 | @Dev2       | Correction bug : le poids total ne se mettait pas à jour si on jetait l'objet.     |

# **4\. Systèmes Core**

## **4.1 Game Manager**

### **4.1.1 Responsabilités**

- Gestion des états du jeu (Menu, Gameplay, Pause, GameOver)
- Transitions entre scènes
- Sauvegarde/Chargement (délégué au SaveSystem)

### **4.1.2 Composants clés (Data Model)**

→ **GameManager.cs** : Singleton orchestrant les états globaux du jeu.

```csharp
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private IGameState _currentState;
}
```

| Variable / Méthode      | Type        | Description                           |
| :---------------------- | :---------- | :------------------------------------ |
| Instance                | GameManager | Référence statique unique (Singleton) |
| \_currentState          | IGameState  | État courant du jeu                   |
| ChangeState(IGameState) | Méthode     | Transition vers un nouvel état        |

### **4.1.3 Dépendances**

- **Est utilisé par :** Tous les systèmes (accès global)
- **Délègue à :** SaveSystem, SceneLoader

### **4.1.4 Approche retenue & alternatives évaluées**

**Pattern retenu :** Singleton \+ State Machine

| Approche                         | Avantages                                           | Inconvénients                     |
| :------------------------------- | :-------------------------------------------------- | :-------------------------------- |
| ✅ **Singleton + State Machine** | Accès global simple, transitions d'états explicites | Difficile à tester unitairement   |
| ServiceLocator                   | Plus testable, moins de couplage global             | Complexité injustifiée à ce stade |

### **4.1.5 Journal d'implémentation**

| Date     | Développeur | Note / Décision Technique |
| :------- | :---------- | :------------------------ |
| \[Date\] | \[Dev\]     | \[Décision ou note\]      |

---

## **4.2 Event System**

### **4.2.1 Responsabilités**

- Découpler les systèmes entre eux (l'émetteur ne connaît pas le récepteur)
- Permettre la communication entre objets sans références directes
- Faciliter le debug via l'Inspector Unity

### **4.2.2 Composants clés (Data Model)**

→ **GameEventSO.cs** : ScriptableObject générique représentant un événement.

```csharp
[CreateAssetMenu(menuName = "Events/GameEvent")]
public class GameEventSO : ScriptableObject
{
    private List<GameEventListener> _listeners;
    public void Raise() { ... }
}
```

| Variable / Méthode | Type                      | Description                                   |
| :----------------- | :------------------------ | :-------------------------------------------- |
| \_listeners        | List\<GameEventListener\> | Abonnés à notifier                            |
| Raise()            | Méthode                   | Déclenche l'événement vers tous les listeners |
| RegisterListener() | Méthode                   | Abonnement d'un listener à l'événement        |

### **4.2.3 Dépendances**

- **Est utilisé par :** CombatSystem, InventorySystem, UIManager, AudioManager
- **Ne dépend de :** Rien (fondation sans dépendances entrantes)

### **4.2.4 Approche retenue & alternatives évaluées**

**Approche retenue :** UnityEvents \+ ScriptableObject Events

| Approche                       | Avantages                                 | Inconvénients                                    |
| :----------------------------- | :---------------------------------------- | :----------------------------------------------- |
| ✅ **UnityEvents + SO Events** | Visible dans l'Inspector, découplage fort | Légère overhead mémoire                          |
| C\# Events natifs              | Plus performants, typage fort             | Non visible dans l'Inspector, couplage namespace |
| MessageBus custom              | Flexibilité maximale                      | Sur-ingénierie pour ce projet                    |

### **4.2.5 Journal d'implémentation**

| Date     | Développeur | Note / Décision Technique |
| :------- | :---------- | :------------------------ |
| \[Date\] | \[Dev\]     | \[Décision ou note\]      |

---

## **4.3 Input System**

### **4.3.1 Responsabilités**

- Capturer les inputs joueur sur toutes les plateformes cibles
- Gérer les contextes d'input (Menu, Gameplay, Dialogue, Cutscene)
- Permettre le rebinding des touches par le joueur

### **4.3.2 Composants clés (Data Model)**

→ **InputReader.cs** : ScriptableObject exposant les événements d'input au reste du jeu.

```csharp
[CreateAssetMenu(menuName = "Input/InputReader")]
public class InputReader : ScriptableObject, GameInput.IGameplayActions
{
    public event Action OnJumpEvent;
    public event Action OnAttackEvent;
}
```

| Variable / Méthode    | Type    | Description                          |
| :-------------------- | :------ | :----------------------------------- |
| OnJumpEvent           | Action  | Déclenché à l'appui du bouton Jump   |
| OnAttackEvent         | Action  | Déclenché à l'appui du bouton Attack |
| EnableGameplayInput() | Méthode | Active l'Action Map Gameplay         |
| EnableUIInput()       | Méthode | Active l'Action Map UI               |

### **4.3.3 Dépendances**

- **Est utilisé par :** PlayerController, UIManager, DialogueSystem
- **Ne dépend de :** Rien (système d'entrée de bas niveau)

### **4.3.4 Approche retenue & alternatives évaluées**

**Approche retenue :** New Input System (package Unity com.unity.inputsystem)

| Approche                | Avantages                                              | Inconvénients                                      |
| :---------------------- | :----------------------------------------------------- | :------------------------------------------------- |
| ✅ **New Input System** | Multi-plateforme natif, Action Maps, rebinding intégré | Courbe d'apprentissage initiale                    |
| Legacy Input Manager    | Simple, familier                                       | Pas de rebinding natif, multi-plateforme laborieux |

### **4.3.5 Journal d'implémentation**

| Date     | Développeur | Note / Décision Technique |
| :------- | :---------- | :------------------------ |
| \[Date\] | \[Dev\]     | \[Décision ou note\]      |

# **5\. Gestion des données**

## **5.1 ScriptableObjects utilisés**

| SO Type      | Usage              | Exemple                   |
| :----------- | :----------------- | :------------------------ |
| GameSettings | Paramètres globaux | Difficulté, volumes audio |
| ItemData     | Définition items   | Armes, consommables       |
| EnemyData    | Stats ennemis      | HP, ATK, Patterns IA      |
| DialogueData | Conversations      | Arbres de dialogue        |

## **5.2 Système de sauvegarde**

**Format :** JSON (via JsonUtility)  
**Emplacement :** Application.persistentDataPath

**Structure :**

{

"playerData": {

    "position": {"x": 0, "y": 0, "z": 0},

    "health": 100,

    "inventory": \[...\]

},

"gameProgress": {

    "currentLevel": 3,

    "unlockedAbilities": \["dash", "double\_jump"\]

}

}

**Alternatives évaluées :**

- **Binary :** Plus rapide, moins lisible, évite cheating facile
- **XML :** Plus verbeux, moins performant
- **PlayerPrefs :** Trop limité pour données complexes

# **6\. Optimisations et performance**

## **6.1 Profiling cibles**

- **CPU :** \<16ms par frame (60 FPS)
- **Mémoire :** \<300MB allocated
- **Draw Calls :** \<500 par frame
- **SetPass Calls :** \<100 par frame

## **6.2 Stratégies d'optimisation**

| Domaine   | Technique             | Implémentation                    |
| :-------- | :-------------------- | :-------------------------------- |
| Rendering | Batching              | Static/Dynamic batching activé    |
| Physics   | Layer-based collision | Matrice de collision optimisée    |
| Code      | Object Pooling        | Bullets, VFX, enemies             |
| Assets    | Texture atlases       | Sprites packés par thème          |
| Audio     | Audio pooling         | Limiter sources audio simultanées |

## **6.3 LOD & Culling**

- **Frustum Culling :** Automatique Unity
- **Occlusion Culling :** À baker pour grandes scènes
- **LOD Groups :** Non nécessaire en 2D / style pixel art

# **7\. Pipeline et outils**

## **7.1 Outils de développement**

- **IDE :** Rider / Visual Studio
- **Version control :** Git \+ GitHub/GitLab
- **Diagrammes :** Figma (architecture), Draw.io (flowcharts)
- **Task tracking :** \[Trello / Notion / Jira\]

## **7.2 Conventions de code**

**Nomenclature :**

// Classes : PascalCase

public class PlayerController {}

// Méthodes : PascalCase

public void TakeDamage() {}

// Variables privées : \_camelCase avec underscore

private int \_currentHealth;

// Variables publiques : PascalCase

public int MaxHealth;

// Constantes : UPPER_SNAKE_CASE

private const float GRAVITY_SCALE \= 2.5f;

// Events : On \+ Verbe au passé

public UnityEvent OnHealthChanged;

## **7.3 Tests**

- **Unit tests :** Pour calculs critiques (damage, progression)
- **Play mode tests :** Pour séquences de gameplay
- **Framework :** Unity Test Framework (NUnit)

# **8\. Risques techniques et mitigations**

| Risque                        | Impact | Probabilité | Mitigation                                           |
| :---------------------------- | :----- | :---------- | :--------------------------------------------------- |
| Performance en combat intense | Élevé  | Moyenne     | Object pooling, LOD, profiling régulier              |
| Corruption de save            | Élevé  | Faible      | Backups automatiques, validation JSON                |
| Bugs de collision             | Moyen  | Moyenne     | Tests systématiques, layer-based physics             |
| Spaghetti code                | Élevé  | Élevée      | Code reviews, patterns établis, refactoring régulier |

# **9\. Roadmap technique**

## **Phase 1 : Fondations (Semaines 1-4)**

- GameManager \+ State Machine
- Input System configuré
- Player controller basique
- Event System

## **Phase 2 : Gameplay Core (Semaines 5-10)**

- Combat System complet
- Inventory System
- Enemy AI basique
- UI System

## **Phase 3 : Contenu (Semaines 11-16)**

- Level design pipeline
- Dialogue System
- Progression System
- Save/Load complet

## **Phase 4 : Polish (Semaines 17-20)**

- Optimisations
- Bug fixing
- Juice & feedback
- Playtesting

# **10\. Références et ressources**

## **Documentation Unity**

- Input System \- https://docs.unity3d.com/Packages/com.unity.inputsystem@latest
- ScriptableObjects Best Practices \- https://unity.com/how-to/architect-game-code-scriptable-objects
- Profiler Guide \- https://docs.unity3d.com/Manual/Profiler.html

## **Patterns de design**

- Game Programming Patterns (Nystrom) \- https://gameprogrammingpatterns.com/
- Unite Talks \- Architecture \- https://www.youtube.com/unity

## **Glossaire technique**

- **i-frames :** Invincibility frames, période d'invulnérabilité
- **Pooling :** Réutilisation d'objets au lieu de destroy/instantiate
- **SO :** ScriptableObject

# **Changelog du document**

| Date     | Version | Changements                   |
| :------- | :------ | :---------------------------- |
| \[Date\] | 1.0     | Création initiale du document |
