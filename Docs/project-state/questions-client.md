# Questions en attente pour le client

> Questions fonctionnelles ou de protocole qui nécessitent une réponse du chercheur.
> Formulées en langage non-technique. Options de réponse proposées quand possible.
> Mis à jour par les rôles 1 et 2.

---

## Format

```
### Q-[NNN] — [Titre court]
- **Posée le :** YYYY-MM-DD
- **Origine :** [Quel rôle / quel chantier]
- **Bloque :** [Ce qui ne peut pas avancer sans réponse] ou "Rien pour l'instant"
- **Question :** [Formulée simplement]
- **Options :**
  - A : [Option] → impact : [conséquence technique/planning]
  - B : [Option] → impact : [conséquence technique/planning]
- **Statut :** EN ATTENTE / RÉPONDU
- **Réponse :** [Quand répondu]
```

---

## Questions ouvertes

### Q-001 — Le score se cumule-t-il entre les trials d'un même bloc ?
- **Posée le :** 2026-03-03
- **Origine :** Pilotage, lors de l'analyse des gaps
- **Bloque :** Spec fonctionnelle du chantier Score & feedback, design de Mountain UI
- **Question :** Quand un participant termine un trial et passe au suivant dans le même bloc, est-ce que son score repart de zéro, ou est-ce qu'il s'accumule ?
- **Options :**
  - A : **Score cumulé** — le participant voit son total grandir au fil du bloc → plus motivant, mais un mauvais trial pèse moins dans le ressenti
  - B : **Score remis à zéro** — chaque trial est indépendant → isole mieux la mesure par trial, mais moins de sens de progression
  - C : **Les deux** — score par trial affiché + score cumulé en fond (ex: dans la Mountain UI) → plus complexe à implémenter, plus riche en données
- **Statut :** RÉPONDU
- **Réponse :** Score cumulé dans le bloc, remis à zéro entre blocs (DEC-010, 2026-03-16)

### Q-002 — Quelles colonnes CSV V1 pour le motor advice ?
- **Posée le :** 2026-03-12
- **Origine :** Analyse fonctionnelle, chantier Motor Advice
- **Bloque :** ~~Traçabilite data et spec fonc finale~~ → 🔴 **requalifiée le 2026-07-28** (revue de couverture, constat N1-B / divergence D-008) : **aucune** des 4 données n'est enregistrée aujourd'hui. Ce n'est pas un sujet de traçabilité mais **une manipulation expérimentale sans mesure** — le niveau moteur est inanalysable. Passe au rang des arbitrages bloquants, aux côtés de Q-010 et Q-011.
- **Question :** Quelles colonnes CSV exactes doivent enregistrer le set actif, l affichage de l advice, sa fiabilite et le set affiche ?
- **Options :**
  - A : Ajouter 4 colonnes dediees → impact : mise a jour schema CSV
  - B : Reutiliser colonnes existantes → impact : mapping a clarifier
- **Statut :** EN ATTENTE — **reprise et détaillée dans [Q-MOTOR-1](#q-motor-1--que-faut-il-enregistrer-du-motor-advice-durcit-q-002)** (2026-07-28). Répondre à Q-MOTOR-1 clôt celle-ci.
- **Réponse :** —

### Q-003 — Le motor advice doit-il inclure une explanation ?
- **Posée le :** 2026-03-12
- **Origine :** Analyse fonctionnelle, chantier Motor Advice
- **Bloque :** Design UI et contenus
- **Question :** Doit on afficher un texte d explanation (short/long) en plus du set de touches ?
- **Options :**
  - A : Oui, explanation requise → impact : contenus a definir
  - B : Non, set uniquement → impact : UI minimaliste
- **Statut :** RÉPONDU
- **Réponse :** Option A — explanation requise sur le motor advice au même titre que distal et proximal. Tranché par DEC-017 et `Docs/specs/explanations-short-long/spec-fonc.md` (§2.3). Date : 2026-05-20.

### Q-004 — Format d affichage du set de touches
- **Posée le :** 2026-03-12
- **Origine :** Analyse fonctionnelle, chantier Motor Advice
- **Bloque :** UI et comprehension participant
- **Question :** Le protocole impose t il un format exact (ordre, labels, separators) pour afficher le set ?
- **Options :**
  - A : Format libre → impact : choix UI local
  - B : Format fixe a fournir → impact : texte exact a valider
- **Statut :** EN ATTENTE
- **Réponse :** —

### Q-005 — Périmètre du mécanisme free/forced sur les 4 choix
- **Posée le :** 2026-05-12
- **Origine :** Pilotage, revue d'avancement vs GDD 2.0
- **Bloque :** Validité expérimentale (H1-H8). Spec fonc du free/forced mechanic. Implémentation par-dessus le flow existant.
- **Question :** Le GDD précise "For some blocks and trials, meta-choices, distal choices, proximal choices, and motor choices will be forced". Quelle granularité de force/free souhaitez-vous, et sur quels niveaux ?
- **Options :**
  - A : **Forced au niveau bloc** uniquement (un bloc = tout forced ou tout free sur un niveau donné) → impact : config simple par bloc, contraste expérimental plus net, moins d'aléatoire
  - B : **Forced au niveau trial** (chaque trial peut indépendamment forcer ou libérer un choix) → impact : config par trial plus lourde, plus de combinaisons testables, randomisation plus fine
  - C : **Mixte** : meta-choice forced/free au bloc, distal/proximal/motor au trial → impact : plus proche du protocole multi-niveaux, complexité moyenne
- **Statut :** RÉPONDU
- **Réponse :** Cadrage finalisé via deux passes :
  - **2026-05-20** — Notes orales chercheur posent le modèle initial (toggles meta/distal/motor block-wise, proxima trial-wise).
  - **2026-05-26** — Consolidation post-arbitrage chercheur (Q-FF-1 à Q-FF-9 + 3 notes complémentaires) :
    - **Meta** : toggle déterministe `advisor_forced` + dropdown explicite `advisor_forced_value ∈ {none, human, robot}`.
    - **Distal** : toggle déterministe `distal_forced` + valeur binaire `distal_forced_optimal_probability ∈ {0, 1}`.
    - **Proximal** : proba trial-wise `proximal_forced_probability ∈ [0, 1]` + valeur binaire `proximal_forced_optimal_probability ∈ {0, 1}`.
    - **Motor** : passage block-wise → trial-wise. Proba `motor_forced_probability ∈ [0, 1]` + set fixé `motor_forced_set` appliqué aux trials forced uniquement.
    - **Note 1** : « Advisor follows forced » — l'advice est subordonné au forced aux 3 niveaux, strict même en unreliable. La reliability ne s'exprime que sur les trials free.
    - **Note 2** : overlay diégétique « Equipment failure » sur trials forced + advisor=none.
  - Spec fonc validée (statut `validé`) : `Docs/specs/free-forced-choices/spec-fonc.md`.
  - Décisions consolidées dans DEC-019 (cf. `decisions.md`).

### Q-FF-1 — Advisor forced : qui choisit l'option imposée ?
- **Posée le :** 2026-05-20
- **Origine :** Analyse fonctionnelle, spec free/forced (§2.1)
- **Bloque :** Finalisation spec free/forced. Structure de `BlockConfig`.
- **Question :** Quand `advisor_forced = true`, qui choisit laquelle des 3 options (none / human / robot) est imposée ?
- **Options :**
  - A : Fixée explicitement par le chercheur dans `BlockConfig` → impact : config bloc plus riche, contrôle expérimental fin
  - B : Tirée aléatoirement par le système au début du bloc (équiprobable ou pondérée) → impact : moins de contrôle, plus d'aléatoire
  - C : Le chercheur choisit un sous-ensemble (ex : "tirer parmi human ou robot") → impact : compromis
- **Statut :** RÉPONDU
- **Réponse :** **Option A** (2026-05-26). Le chercheur sélectionne explicitement l'option imposée via un dropdown `{none, human, robot}` dans le session config panel, par bloc. Tranché par DEC-019 et `Docs/specs/free-forced-choices/spec-fonc.md` (§2.1 R1).

### Q-FF-2 — Distal forced : granularité du toggle (bloc seul vs bloc + trial)
- **Posée le :** 2026-05-20
- **Origine :** Analyse fonctionnelle, spec free/forced (§2.2)
- **Bloque :** Modèle de config et de pilotage du distal forced.
- **Question :** `distal_forced` est-il un flag bloc binaire (tout bloc-wide) ou y a-t-il aussi une proba `distal_forced_probability` au niveau bloc qui décide trial-by-trial ?
- **Options :**
  - A : Flag bloc uniquement (tout bloc-wide) → impact : simple, cohérent avec le distal-choice qui est lui-même block-wise (DEC-001)
  - B : Flag + proba trial-wise → impact : plus de combinaisons, plus complexe
  - C : Proba uniquement (pas de flag binaire) → impact : uniforme avec proximal
- **Statut :** RÉPONDU
- **Réponse :** **Option A** (2026-05-26). Flag binaire au bloc — un bloc n'a qu'un distal-choice, un tirage probabiliste trial-wise n'a pas de sens. Paramètre séparé `distal_forced_optimal_probability ∈ {0, 1}` (valeur binaire, cf. Note 3) pour l'optimalité de la vallée imposée. Tranché par DEC-019 et `Docs/specs/free-forced-choices/spec-fonc.md` (§2.2 R5).

### Q-FF-3 — Proximal forced : neutralisation visuelle du cloud non retenu
- **Posée le :** 2026-05-20
- **Origine :** Analyse fonctionnelle, spec free/forced (§2.3)
- **Bloque :** Logique de génération de map et mécanique de fin de trial.
- **Question :** Quand un trial proximal est forced, comment l'autre cloud est-il neutralisé visuellement ?
- **Options :**
  - A : L'autre cloud n'est pas instancié (1 seul cloud spawné) → impact : plus simple, génération de map différente
  - B : L'autre cloud est instancié mais masqué (fog of war permanent dessus) → impact : génération de map identique, masquage à gérer
  - C : L'autre cloud est visible mais non collectable → impact : ambigu pour le participant, déconseillé
- **Statut :** RÉPONDU
- **Réponse :** **Option B** (2026-05-26). Les deux clouds restent instanciés (spawner inchangé). Le cloud non imposé est masqué par fog of war permanent (non révélable, non collectable). Détails et implémentation : `Docs/specs/free-forced-choices/spec-fonc.md` (§2.3). Mécanique exacte de masquage à préciser côté spec tech.

### Q-FF-4 — Proximal forced : quel cloud est imposé ?
- **Posée le :** 2026-05-20
- **Origine :** Analyse fonctionnelle, spec free/forced (§2.3)
- **Bloque :** Combinatoire expérimentale et complexité de la config bloc.
- **Question :** Quand un trial est proximal forced, quel cloud est imposé au participant ?
- **Options :**
  - A : Toujours l'optimal → impact : participant subit toujours le "bon" choix
  - B : Toujours l'advisé (si advice donné) → impact : forced devient un cas particulier d'advice obligatoire
  - C : Random équiprobable → impact : pas de biais, mais variabilité
  - D : Configurable par bloc via une proba `proximal_forced_optimal_probability` (analogue distal) → impact : symétrie avec distal, plus complexe
- **Statut :** RÉPONDU
- **Réponse :** **Option D** (2026-05-26). Paramètre `proximal_forced_optimal_probability ∈ {0, 1}` (valeur binaire, cf. Note 3) au niveau bloc, en symétrie avec `distal_forced_optimal_probability`. 1 → cloud optimal toujours imposé, 0 → cloud sous-optimal toujours imposé. Tranché par DEC-019 et `Docs/specs/free-forced-choices/spec-fonc.md` (§2.3 R11).

### Q-FF-5 — Distal forced sans advisor : conserver la mécanique ?
- **Posée le :** 2026-05-20
- **Origine :** Analyse fonctionnelle, spec free/forced (§2.2 R9)
- **Bloque :** Validité scientifique de la condition "imposé sans advisor".
- **Question :** Si meta-choice forced = `none` (no advisor imposé) OU si le participant a choisi `no advisor` en mode free, le distal forced (avec sa proba d'optimalité) reste-t-il pertinent ?
- **Options :**
  - A : Oui, distal forced reste actif (la vallée est imposée même sans advisor pour l'expliquer) → impact : condition expérimentale "agency annulée sans information"
  - B : Non, distal forced est ignoré si pas d'advisor → impact : couplage agency/information
  - C : Distal forced reste actif mais l'optimalité forcée joue un rôle différent à clarifier → impact : à préciser
- **Statut :** RÉPONDU
- **Réponse :** **Option A étendue** (2026-05-26). Distal forced **ET** proximal forced restent actifs même sans advisor — condition expérimentale "agency annulée sans information" préservée. Pour contextualiser narrativement la contrainte en l'absence d'advisor, un overlay diégétique « **Equipment failure** » est affiché à l'écran (cf. Note 2). Tranché par DEC-019 et `Docs/specs/free-forced-choices/spec-fonc.md` (§2.2 R9 et TR6 réécrite).

### Q-FF-6 — Motor forced × motor-advice : cohérence
- **Posée le :** 2026-05-20
- **Origine :** Analyse fonctionnelle, spec free/forced (§2.4 R18)
- **Bloque :** Combinatoire et lisibilité expérimentale du niveau motor.
- **Question :** Quand motor forced est ON, le motor-advice doit-il refléter le set imposé ?
- **Options :**
  - A : Oui, advice toujours cohérent avec le set imposé (sauf si advice unreliable) → impact : cohérent, mais redondant si fiabilité=100%
  - B : Oui, advice toujours montré, mais peut être unreliable et montrer un autre set → impact : conserve la combinatoire reliability orthogonale
  - C : Motor-advice est désactivé quand motor forced (redondant) → impact : simplifie l'UI, perd de la combinatoire
- **Statut :** RÉPONDU
- **Réponse :** **Option A renforcée par Note 1** (2026-05-26). Motor-advice obligatoirement cohérent avec le set imposé sur les trials forced, **strict même en unreliable** (« advisor follows forced », TR3 réécrite). La reliability ne s'exprime que sur les trials free du bloc. Tranché par DEC-019 et `Docs/specs/free-forced-choices/spec-fonc.md` (§2.4 R20 et TR3).

### Q-FF-7 — Validation des choix imposés (advisor/distal)
- **Posée le :** 2026-05-20
- **Origine :** Analyse fonctionnelle, spec free/forced (§2.1, §2.2)
- **Bloque :** Sense of agency mesuré, contrôle de l'exposition à l'écran.
- **Question :** Quand un choix advisor/distal est imposé, faut-il un clic explicite du participant sur l'option imposée, ou une auto-validation après délai ?
- **Options :**
  - A : Clic explicite requis → impact : engage le participant, maintient une "pseudo-agency"
  - B : Auto-validation après délai configurable → impact : neutralise complètement l'agency, plus rapide
  - C : Clic explicite + délai minimum d'exposition (lecture forcée) → impact : compromis
- **Statut :** RÉPONDU
- **Réponse :** **Option A** (2026-05-26). Clic explicite requis sur l'option imposée pour valider et passer à la suite. Préserve un acte moteur d'engagement même en mode forced. Tranché par DEC-019 et `Docs/specs/free-forced-choices/spec-fonc.md` (§2.1 et §2.2).

### Q-FF-8 — Feedback explicite "choix imposé" au participant
- **Posée le :** 2026-05-20
- **Origine :** Analyse fonctionnelle, spec free/forced (§3 TR6)
- **Bloque :** Validité de la mesure de sense of agency (peut biaiser la réponse).
- **Question :** Le participant doit-il être averti textuellement que son choix est imposé (ex : bandeau "choix imposé pour ce bloc"), ou seule la sémantique UI (options grisées) doit le signaler ?
- **Options :**
  - A : Pas de bandeau, UI seule (options grisées) → impact : sémantique implicite, moins de biais potentiel
  - B : Bandeau systématique → impact : transparent, mais peut biaiser la réponse "sense of agency"
  - C : Bandeau configurable par le chercheur (par bloc) → impact : permet les deux conditions
- **Statut :** RÉPONDU
- **Réponse :** **Hybride** (2026-05-26). Par défaut, pas de bandeau textuel — seule l'UI signale visuellement la contrainte (options grisées, cloud masqué par fog of war). **Exception unique** : quand un trial est forced ET que l'advisor est `none`, un overlay diégétique « **Equipment failure** » est affiché à l'écran (Note 2). Justifie narrativement la contrainte en l'absence d'advisor pour la contextualiser. Tranché par DEC-019 et `Docs/specs/free-forced-choices/spec-fonc.md` (TR6 réécrite).

### Q-FF-9 — Motor forced : comment est défini le set imposé ?
- **Posée le :** 2026-05-20
- **Origine :** Analyse fonctionnelle, spec free/forced (§2.4)
- **Bloque :** Structure config bloc côté `BlockConfig`.
- **Question :** Quand `motor_forced = true`, comment est défini `motor_forced_set` (le set commun à tous les trials du bloc) ?
- **Options :**
  - A : Fixé explicitement par le chercheur dans `BlockConfig` → impact : contrôle expérimental fin
  - B : Tiré aléatoirement une seule fois au début du bloc → impact : moins de contrôle, plus d'aléatoire
  - C : Le chercheur choisit un sous-ensemble parmi lequel tirer → impact : compromis
- **Statut :** RÉPONDU
- **Réponse :** **Option A étendue par Note 3** (2026-05-26). `motor_forced_set ∈ {QZD, FTH, KOM}` fixé par le chercheur via dropdown dans le session config panel. **Évolution majeure** : motor passe de block-wise à trial-wise. Le set imposé s'applique **uniquement aux trials forced** du bloc (selon `motor_forced_probability ∈ [0, 1]`) ; les trials free du même bloc conservent le tirage aléatoire par trial. Tranché par DEC-019 et `Docs/specs/free-forced-choices/spec-fonc.md` (§2.4 R17–R20).
  > ⚠️ **Errata 2026-07-28** — le domaine cité ci-dessus est celui de mai 2026. Le set `OKLM` a depuis été remplacé par `IJKL` (commit `7b3a9e86`) : le domaine réel est **`{QZD, FTH, JIL}`**. Cf. errata de `free-forced-choices/spec-fonc.md`.

### Q-006 — Explanations associées aux advice (short/long)
- **Posée le :** 2026-05-12
- **Origine :** Pilotage, revue d'avancement vs GDD 2.0
- **Bloque :** Hypothèse H8 (explanations × abstraction). Conception UI et système d'édition des textes.
- **Question :** Le GDD prévoit que chaque advice peut être accompagné d'une explanation "short" ou "long", éditable par le chercheur. Comment souhaitez-vous gérer ces contenus ?
- **Options :**
  - A : **Catalogue de textes éditables côté chercheur** (BDD ou fichier de config) → impact : système CMS-like, édition sans rebuild, plus de flexibilité
  - B : **Textes hardcodés validés en amont** par le chercheur → impact : pas d'outil d'édition, mais nécessite un cycle de validation à chaque modification
  - C : **Génération paramétrée** (templates avec variables) → impact : compromis flexibilité/simplicité, mais nécessite une grammaire de templates
- **Statut :** RÉPONDU
- **Réponse :** Modèle retenu = **édition par les chercheurs dans le session config panel** (variante de l'option A, sans CMS dédié). Pour chaque advice, deux textes alternatifs (`short` et `long`) maintenus indépendamment. Tranché par DEC-017 et `Docs/specs/explanations-short-long/spec-fonc.md` (TR3, D5). Date : 2026-05-20. Note : les sous-questions de cadrage restantes (granularité bloc/trial, mode `none` activable, customisation par bloc, etc.) sont consignées comme Q-EXP-1 à Q-EXP-10 ci-dessous.

### Q-007 — Modèle de perte de bugs en cas de piège
- **Posée le :** 2026-05-12
- **Origine :** Pilotage, revue d'avancement vs GDD 2.0
- **Bloque :** Cohérence protocole. Résultats comparables au GDD.
- **Question :** ⚠️ **Reformulée le 2026-07-28 — ce n'est plus un arbitrage entre deux modèles, mais un sign-off.** La revue de complétude a établi que le code applique déjà le modèle du GDD : `-1 bug vert par nuage par piège`, sans jamais retirer de bug rouge et sans descendre sous zéro (`GameManager.cs:262-275`, `BugCloud.AddBugs`). La même pénalité de -1 s'applique aussi à deux autres événements non prévus au GDD : chaque pas au-delà du budget (distance de Manhattan joueur→nuages) et chaque appui sur une touche hors du set moteur actif. **Confirmez-vous ce modèle tel qu'implémenté, y compris les deux pénalités additionnelles ?**
- **Options :**
  - A : **Valider en l'état** → impact : aucun changement de code ; on ferme Q-007 et on corrige le TDD, qui décrit encore à tort une pénalité de -2 (18 occurrences)
  - B : **Valider la pénalité de piège mais revoir les deux autres** (budget de pas, touche invalide) → impact : à cadrer, ces deux mécaniques ne figurent pas au GDD
  - C : **Autre modèle** à préciser → impact : à définir
- **Statut :** EN ATTENTE — le code tranche déjà côté GDD, seul le sign-off manque
- **Réponse :** —

### Q-008 — Nombre de paths visibles dans la forêt (2 vs 4)
- **Posée le :** 2026-05-12
- **Origine :** Pilotage, revue d'avancement vs GDD 2.0
- **Bloque :** Conception level generation, advisor proximal, choix proximal
- **Question :** Le GDD indique "two labyrinth-like paths lead to each cloud (4 paths in all)". L'implémentation actuelle génère 2 paths totaux (un par cloud). Quel design retient-on ?
- **Options :**
  - A : **Aligner sur le GDD** : 4 paths visibles, 2 par cloud → impact : refonte de PathSpawner + CorridorWallsGenerator, plus de richesse stratégique
  - B : **Conserver 2 paths totaux** (1 par cloud) → impact : mettre à jour le GDD, justifier le choix
  - C : **Autre configuration** (ex : 2 paths par cloud mais 1 advised) → impact : à préciser
- **Statut :** EN ATTENTE
- **Réponse :** —

### Q-009 — Transition entre trials (fade noir vs smooth camera pan)
- **Posée le :** 2026-05-12
- **Origine :** Pilotage, revue d'avancement vs GDD 2.0
- **Bloque :** UX et immersion. DEC-002 à reconfirmer.
- **Question :** Le GDD demande explicitement "Smooth transition between maps : camera pan vertical to the top, the character walks automatically". DEC-002 a tranché un fade noir. Confirme-t-on l'écart ?
- **Options :**
  - A : **Conserver le fade noir** (DEC-002) → impact : statu quo, plus simple, écart GDD assumé
  - B : **Implémenter le smooth pan + auto-walk** comme dans le GDD → impact : revoir FadeTransition + ajouter logique de pan caméra + auto-walk inter-trial, plus immersif
  - C : **Hybride** : fade noir avec animation de transition stylisée → impact : compromis, à concevoir
- **Statut :** EN ATTENTE
- **Réponse :** —

### Q-010 — Reliability pattern (fréquence et distribution de l'advice)
- **Posée le :** 2026-05-12
- **Origine :** Pilotage, revue d'avancement vs GDD 2.0
- **Bloque :** Implémentation de la manipulation de fiabilité (cœur scientifique).
- **Question :** Le GDD mentionne "advice is provided periodically following a given pattern (random ?)" et laisse la section "Reliability Manipulations" vide. Quelle est la spécification finale ?
- **Options :**
  - A : **Random uniforme** : reliability tiré à chaque trial selon une probabilité paramétrable → impact : implémentation simple, paramètre unique par bloc
  - B : **Pattern déterministe** : séquence pré-calculée par bloc (ex : 6 trials fiables / 6 non fiables alternés) → impact : reproductibilité parfaite, contrôle expérimental fin
  - C : **Mixte** : random au sein de quotas (ex : 70% fiable garantis sur 36 trials) → impact : compromis, plus représentatif d'un advisor "globalement fiable"
- **Statut :** EN ATTENTE
- **Réponse :** —

### Q-011 — Mapping QuestionnaireUI aux 3 dimensions GDD
- **Posée le :** 2026-05-12
- **Origine :** Pilotage, revue d'avancement vs GDD 2.0
- **Bloque :** Finalisation QuestionnaireScene. Validité des données collectées.
- **Question :** Le GDD prévoit 3 dimensions à mesurer : sense of agency (control), acceptability (de l'advisor), human-likeness. Les colonnes `q1..q3` sont prêtes (DEC-008). Quel mapping retient-on et qui édite les textes ?
- **Options :**
  - A : **Mapping fixe** q1=control, q2=acceptability, q3=human-likeness, textes hardcodés → impact : simple mais peu flexible
  - B : **Mapping fixe + textes éditables côté chercheur** (config ou BDD) → impact : ajout d'un système d'édition, plus de souplesse
  - C : **Mapping configurable par bloc** (toutes les dimensions ne sont pas demandées à chaque bloc) → impact : config plus riche, nécessite logique conditionnelle dans le flow
- **Statut :** EN ATTENTE
- **Réponse :** —

### Q-012 — Nombre de trials par bloc et nombre de blocs training
- **Posée le :** 2026-05-12
- **Origine :** Pilotage, revue d'avancement vs GDD 2.0
- **Bloque :** Configuration des sessions de test. Durée d'une session.
- **Question :** Le GDD indique "Each block contains 36 trials" et "Training block(s): ##?". Confirmez-vous les valeurs cibles ?
- **Options :**
  - A : **36 trials/bloc + 1 bloc training** (valeur GDD par défaut) → impact : durée ~X min/bloc, à confirmer
  - B : **Autres valeurs** : à préciser → impact : adapter la config par défaut
  - C : **Paramétrable par session, pas de valeur fixe** → impact : laisser la config par session (déjà possible), pas de valeur "par défaut" actée
- **Statut :** EN ATTENTE
- **Réponse :** —

### Q-013 — Visibility/saturation noise sur clouds (distal + proximal)
- **Posée le :** 2026-05-12
- **Origine :** Pilotage, revue d'avancement vs GDD 2.0
- **Bloque :** Implémentation du shader/particules pour le bruit visuel. Manipulation de difficulté de discrimination.
- **Question :** Le GDD demande "Targets should have a visibility noise parameter to make more difficult to discriminate which has the more green bugs (blur, saturation, or green/red ratio)" pour les deux écrans. Quel rendu visuel retient-on ?
- **Options :**
  - A : **Saturation des couleurs** (réduction du contraste rouge/vert) → impact : simple, modification de matériau/shader, ajout d'un paramètre `saturation_noise`
  - B : **Blur / gabor masking** → impact : shader plus complexe, plus proche du GDD
  - C : **Variation du ratio affiché vs réel** (cloud bruité visuellement par particules supplémentaires d'une couleur) → impact : altère la perception sans toucher au rendu, plus simple à équilibrer
- **Statut :** EN ATTENTE
- **Réponse :** —

### Q-EXP-1 — Explanations : comment piloter quelle version (short / long) est donnée ?
- **Posée le :** 2026-05-20
- **Origine :** Analyse fonctionnelle, chantier Explanations short/long
- **Bloque :** Finalisation spec fonc et conception du session config panel.
- **Question :** Le mode actif (short vs long) sur un advice donné est-il piloté par bloc, par trial, ou de manière mixte ?
- **Options :**
  - A : **Par bloc** — un mode unique pour tous les trials du bloc → simple, contraste expérimental net
  - B : **Par trial** — tirage indépendant à chaque trial → combinatoire plus riche pour H8
  - C : **Mixte** — bloc active la feature, trial tire short vs long → compromis lisibilité/richesse
- **Statut :** RÉPONDU
- **Réponse :** Option A — **Blockwise**. `display_mode` et `content_variant` sont définis par bloc. Tranché par DEC-018 (échange Florian / Valerian / Mark, principe « keep it simple »). Date : 2026-05-26. Note : Mark préférait trial-wise (option B), consigné comme alternative évaluée dans la spec fonc §6.

### Q-EXP-2 — Explanations : mode `none` activable même quand un advisor est choisi ?
- **Posée le :** 2026-05-20
- **Origine :** Analyse fonctionnelle, chantier Explanations short/long
- **Bloque :** Schéma de config session, validité expérimentale (variable indépendante `Explanation/No explanations` du GDD).
- **Question :** Veut-on des blocs où l'explanation est simplement non donnée alors même qu'un advisor a été choisi et qu'un advice est donné ?
- **Options :**
  - A : **Oui** — `none` est un mode pilotable au même titre que short/long → permet la condition contrôle "advice sans explanation"
  - B : **Non** — dès qu'il y a un advice il y a forcément une explanation → simplifie, mais on perd la condition contrôle
- **Statut :** RÉPONDU
- **Réponse :** Option A — **`none` activable**. Couvert par la nouvelle dimension `display_mode = none` introduite via Q-EXP-5 (cf. DEC-018). Valerian : « We need the possibility to have advice without explanation ». Date : 2026-05-26.

### Q-EXP-3 — Explanations : corpus par bloc ou par session ?
- **Posée le :** 2026-05-20
- **Origine :** Analyse fonctionnelle, chantier Explanations short/long
- **Bloque :** Taille et structure du corpus, complexité du panneau de config.
- **Question :** Les contenus des explanations sont-ils customizables par bloc, ou identiques pour toute la session ?
- **Options :**
  - A : **Par session uniquement** — un corpus global (3 advice × 2 versions = 6 textes par session) → simple
  - B : **Par bloc** — un corpus par bloc → plus de flexibilité expérimentale, panneau plus dense
  - C : **Hybride** — corpus session par défaut, possibilité d'override par bloc → compromis
- **Statut :** RÉPONDU
- **Réponse :** Option B — **Par bloc**. Valerian a marqué une préférence pour davantage de flexibilité (« If you give me more flexibility, I'll take it ») tout en notant que ce n'est pas une priorité scientifique forte. Arbitrage Florian : donner la flexibilité dès le départ. Combiné avec Q-EXP-8 (option B, 2 corpora par advisor type) → 12 textes par bloc (3 advice × 2 variants × 2 advisor types). Tranché par DEC-018. Date : 2026-05-26.

### Q-EXP-4 — Explanations : cross-level autorisé ?
- **Posée le :** 2026-05-20
- **Origine :** Analyse fonctionnelle, chantier Explanations short/long
- **Bloque :** Combinatoire des conditions H8.
- **Question :** Un advice peut-il porter une explanation d'un autre niveau d'abstraction (ex : motor-advice + explanation distale) ?
- **Options :**
  - A : **Non, 1-to-1** — chaque advice porte uniquement sa propre explanation
  - B : **Oui, n'importe quel level** — toutes les combinaisons sont possibles
  - C : **Matrice contrôlée** — seul un sous-ensemble explicite de combinaisons est autorisé
- **Statut :** RÉPONDU
- **Réponse :** Option A — **1-to-1 strict** (TR8). Valerian : « only a motor explanation can accompany a motor advice, etc. ». Tranché par DEC-018. Date : 2026-05-26. Note : Mark proposait un cross-level one-way (lower-level advice + higher-order explanation) avec une justification scientifique forte (tester l'effet du framing AI sur l'advice-taking). Alternative consignée dans la spec fonc §6 (A1), ré-ouvrable si les premiers résultats expérimentaux le justifient.

### Q-EXP-5 — Explanations : timing d'affichage relatif à l'advice ?
- **Posée le :** 2026-05-20
- **Origine :** Analyse fonctionnelle, chantier Explanations short/long
- **Bloque :** UI, perception participant.
- **Question :** Quand s'affiche l'explanation par rapport à l'advice ?
- **Options :**
  - A : **Avant l'advice**
  - B : **Simultané** avec l'advice
  - C : **Après** l'advice
  - D : **Bouton "voir explication"** activable par le participant
- **Statut :** RÉPONDU
- **Réponse :** **Nouveau modèle** : refonte de la question en une dimension `display_mode` à 3 valeurs : `forced` (affichage d'office, pas de bouton), `opt-in` (bouton « show explanation » activable par le participant), `none` (aucune explanation). Mark et Valerian convergent sur l'intérêt scientifique du bouton opt-in (mesure du besoin d'explication par le participant). Mark a également demandé le tracking du clic et de la durée d'affichage (acté : colonnes `*_explanation_clicked` et `*_explanation_display_duration_ms`). Tranché par DEC-018. Date : 2026-05-26.

### Q-EXP-6 — Explanations : skippable / lecture obligatoire ?
- **Posée le :** 2026-05-20
- **Origine :** Analyse fonctionnelle, chantier Explanations short/long
- **Bloque :** UX, contrôle de l'exposition.
- **Question (reformulée 2026-05-26) :** Modalités précises de hide et positionnement de l'explication, à valider en **UI test**. Contrainte acquise : pas de recouvrement de la map (TR10 / DEC-018). Compromis Valerian (« stays on screen until the trial is complete ») + Mark (« they can hide the explanation after a while ») → l'explication doit pouvoir être cachée par le participant, sans la masquer derrière la map.
- **Options résiduelles :**
  - A : durée minimale d'exposition obligatoire avant possibilité de cacher
  - B : hide libre dès l'affichage
  - C : positionnement fixe (encart latéral) sans hide
- **Statut :** EN ATTENTE — UI test à mener
- **Réponse partielle :** Décision reportée à un UI test ; contrainte TR10 (no-overlap) actée. Date : 2026-05-26.

### Q-EXP-7 — Explanations : nombre max simultanées sur un trial ?
- **Posée le :** 2026-05-20
- **Origine :** Analyse fonctionnelle, chantier Explanations short/long
- **Bloque :** Surcharge UI, logging.
- **Question :** Si plusieurs advice sont présents sur un même trial (D rappelé + P + M), combien d'explanations simultanées max ?
- **Options :**
  - A : **1 par advice** (jusqu'à 3) → fidèle au design distinct par niveau
  - B : **1 globale par trial** → moins de surcharge UI, mais perd la spécificité par niveau
  - C : **Piloté par config** → cas par cas
- **Statut :** RÉPONDU
- **Réponse :** Option A — **1 par advice (jusqu'à 3)**. Valerian : « we also want to test the combined effect of advices ». Layout proposé et acté : motor à gauche, proximal à droite (TR9). Tranché par DEC-018. Date : 2026-05-26.

### Q-EXP-8 — Explanations : corpus différencié par advisor type ?
- **Posée le :** 2026-05-20
- **Origine :** Analyse fonctionnelle, chantier Explanations short/long
- **Bloque :** Taille du corpus, mapping config.
- **Question :** Différencie-t-on le corpus par advisor type (human-bot vs bot-bot) ? Le no-advisor a-t-il des explanations ?
- **Options :**
  - A : **Corpus unique** quel que soit l'advisor type → simple
  - B : **Corpus ×2** (human-bot / bot-bot) → permet de tester l'effet du ton
  - C : **Corpus ×3** (human-bot / bot-bot / no-advisor) → corpus complet, plus volumineux
- **Statut :** RÉPONDU
- **Réponse :** Option B — **2 corpora (human-bot, bot-bot)**. Valerian a marqué un intérêt scientifique : « we have hypotheses about the effect of the "AI syntax/language" on recommendations ». Arbitrage Florian : retenir B (le no-advisor n'a pas d'advice donc pas d'explanation, cohérent avec R1). 12 textes par bloc au total (3 advice × 2 variants × 2 advisor types). Tranché par DEC-018 (TR13). Date : 2026-05-26.

### Q-EXP-9 — Explanations : format des valeurs CSV `*_advice_explanation` ?
- **Posée le :** 2026-05-20
- **Origine :** Analyse fonctionnelle, chantier Explanations short/long
- **Bloque :** Traçabilité analytique.
- **Question :** Comment encoder les explanations dans le CSV V1 ?
- **Options :**
  - A : **Enum `none/short/long`** uniquement → simple, suffit pour H8 si corpus est figé
  - B : **Id du texte affiché** → permet de retrouver le contenu exact
  - C : **Enum + id (deux colonnes)** → maximum de traçabilité
- **Statut :** RÉPONDU
- **Réponse :** Option C — **Enum + id**. Valerian : « Maximum traceability! ». Enrichi du tracking opt-in (clic + durée d'affichage) demandé par Mark. Schéma final : 5 colonnes par advice × 3 advices = 15 colonnes (`*_display_mode`, `*_content_variant`, `*_text_id`, `*_clicked`, `*_display_duration_ms`). Tranché par DEC-018. Date : 2026-05-26.

### Q-EXP-10 — Explanations : localisation du corpus ?
- **Posée le :** 2026-05-20
- **Origine :** Analyse fonctionnelle, chantier Explanations short/long
- **Bloque :** Structure du corpus, taille config.
- **Question :** Le corpus est-il FR uniquement ou multilingue dès le départ ?
- **Options :**
  - A : **FR seul** → cohérent avec la documentation projet en français
  - B : **Multilingue dès le départ** → si l'étude vise une population non-FR
- **Statut :** EN ATTENTE
- **Réponse :** —

### Q-EXP-11 — Communication Report : quel comportement en bloc tutoriel ?
- **Posée le :** 2026-08-27
- **Origine :** Support dev, chantier Explanations short/long (DEC-023)
- **Bloque :** Rien pour l'instant — comportement par défaut implémenté (affichage aussi en tutoriel)
- **Question :** Le Communication Report (annonce de la qualité de la « réception radio ») est désormais affiché à chaque entrée dans l'écran de choix de vallée, sans condition (DEC-023). Or en bloc tutoriel les explanations sont supprimées (TR4 / DEC-014). Faut-il afficher le report pendant le tutoriel, et si oui avec quel texte ?
- **Options :**
  - A : **Afficher comme un bloc normal** (comportement actuel) → simple, mais le texte peut annoncer des explanations qui n'apparaîtront jamais en tutoriel
  - B : **Masquer en tutoriel** → retour partiel à l'ancienne condition ; un cas particulier de plus
  - C : **Texte dédié tutoriel** → contenu à fournir par le chercheur
- **Statut :** EN ATTENTE
- **Réponse :** —

### Q-ICT-1 — In-context tutorial : origine de la donnée réelle (titre/texte par scène) ?
- **Posée le :** 2026-05-29
- **Origine :** Architecture technique, chantier in-context-tutorial (DEC-020)
- **Bloque :** Rien pour la livraison composant (agnostique). Bloque l'intégration back-end (collègue).
- **Question :** D'où vient le contenu (titre + texte) injecté dans l'overlay de chaque scène ?
- **Options :**
  - A : Champs ajoutés à `BlockConfig` (back-end Supabase) → cohérent avec le reste de la config
  - B : Autre source → à définir
- **Statut :** EN ATTENTE
- **Réponse :** —

### Q-ICT-2 — In-context tutorial : condition exacte de déclenchement ?
- **Posée le :** 2026-05-29
- **Origine :** Analyse fonctionnelle, chantier in-context-tutorial
- **Bloque :** Logique de gating côté intégrateur.
- **Question :** L'overlay s'affiche-t-il dès qu'un contenu est présent pour la scène, ou faut-il aussi que le bloc soit explicitement flaggé tutorial ?
- **Options :**
  - A : Présence de contenu suffit (le composant no-op si vide) → plus simple
  - B : Gate `is_tutorial` + contenu présent → garde-fou explicite
- **Statut :** EN ATTENTE
- **Réponse :** —

### Q-ICT-3 — In-context tutorial : tracking de la consultation dans `trial_responses` ?
- **Posée le :** 2026-05-29
- **Origine :** Analyse fonctionnelle, chantier in-context-tutorial
- **Bloque :** Rien (V1 sans tracking). Donnée de recherche éventuelle.
- **Question :** Faut-il enregistrer que l'overlay a été vu (et/ou sa durée de lecture) ?
- **Options :**
  - A : Aucun tracking (V1) → composant n'écrit rien
  - B : Vu oui/non + durée → via abonnement aux events côté intégrateur (sans modifier le composant)
- **Statut :** EN ATTENTE
- **Réponse :** —

### Q-ICT-4 — In-context tutorial : localisation multilingue ?
- **Posée le :** 2026-05-29
- **Origine :** Analyse fonctionnelle, chantier in-context-tutorial
- **Bloque :** Structure du contenu côté config. Non bloquant V1.
- **Question :** Les instructions sont-elles FR seul ou multilingue ? (À aligner avec Q-EXP-10.)
- **Options :**
  - A : FR seul (V1)
  - B : Clés i18n
- **Statut :** EN ATTENTE
- **Réponse :** —

### Q-ICT-5 — In-context tutorial : contenu final des instructions par scène ?
- **Posée le :** 2026-05-29
- **Origine :** Analyse fonctionnelle, chantier in-context-tutorial
- **Bloque :** Onboarding contextuel effectif. La dummy data est représentative, pas validée.
- **Question :** Quels titres + textes exacts pour Advisor / Distal / Proximal, par bloc tutorial ?
- **Options :**
  - A : À fournir par les chercheurs avant lancement
- **Statut :** EN ATTENTE
- **Réponse :** —

### Q-ICT-6 — In-context tutorial : périmètre du verrou input sur Proximal pendant l'overlay ?
- **Posée le :** 2026-05-29
- **Origine :** Architecture technique, chantier in-context-tutorial
- **Bloque :** UX gameplay (intégration Proximal).
- **Question :** Quels inputs geler tant que l'overlay est ouvert sur la scène de jeu ?
- **Options :**
  - A : Clavier gameplay via `GameManager.inputLocked` (le fond modal bloque déjà la souris)
  - B : + autres systèmes (caméra, scan) si nécessaire
- **Statut :** EN ATTENTE
- **Réponse :** —

---

## Questions ouvertes ajoutées par la revue de complétude (2026-07-28)

> Ces questions existaient déjà ailleurs dans le repo ou ont été découvertes en confrontant le code aux specs.
> Détail complet : `Docs/project-state/revue-completude-2026-07-28.md`.

### Q-CONSENT-1 — Quel est le texte de consentement réel, et qui doit pouvoir l'éditer ?
- **Posée le :** 2026-07-28
- **Origine :** Pilotage, revue de complétude (écran E2)
- **Bloque :** 🔴 **La conformité éthique de la passation.** Aujourd'hui `ConsentUI.cs:16-20` écrase l'écran par un placeholder codé en dur (« Veuillez accepter le consentement pour demarrer la session. ») et il n'existe **aucun champ `consent_text`** dans la configuration de session. Le vrai texte ne peut donc être affiché ni par la scène, ni par l'API.
- **Question :** Quel texte doit voir le participant, et doit-il être éditable par le chercheur sans rebuild ?
- **Options :**
  - A : Texte fourni une fois et figé dans la scène Unity → impact : simple, mais toute correction impose un rebuild
  - B : Nouveau champ `consent_text` dans la configuration de session (comme `rules`) → impact : cohérent avec DEC-009, éditable à chaud, ~½ journée de dev
- **Note annexe :** `ConsentUI.cs:34` annonce « La session s'arrete ici » alors que le refus renvoie vers Welcome. À trancher aussi : que doit-il se passer exactement en cas de refus ?
- **Statut :** EN ATTENTE
- **Réponse :** —

### Q-QUEST-1 — QuestionnaireScene : à câbler ou à retirer du flow ?
- **Posée le :** 2026-07-28
- **Origine :** Pilotage, revue de complétude (écran E1)
- **Bloque :** 🔴 Un écran mort dans le flow.
- **Question :** `QuestionnaireUI.cs:38` force la liste de questions à vide : la scène ne collecte **rien**. Les 3 dimensions sont en réalité collectées par `TrialQuestionsUI` dans ProximalScene, qui fonctionne. Faut-il un questionnaire de fin de bloc **en plus** de celui par trial ?
- ⚠️ **Portée corrigée le 2026-07-28** (revue de couverture, constat N1-D / divergence D-010). Trois précisions changent l'arbitrage :
  1. **La scène n'est même jamais chargée** — `AdvanceToPhase(GamePhase.Questionnaire)` n'existe nulle part (12 sites d'appel vérifiés). Elle est activée dans le build mais inatteignable. L'option A ne « économise » donc rien au runtime : elle nettoie du code mort.
  2. **L'option B n'est pas un branchement.** `BlockConfig.questions` **n'existe pas** : `QuestionConfig` est un type orphelin, référencé par aucun champ de configuration. Vous n'avez aujourd'hui aucun moyen de saisir des questions.
  3. **La sortie est amputée** : `ApiClient.QueueQuestionnairePatchForTrial:136-147` ne conserve **que** la réponse « ressemblance humaine » et jette les autres. Câbler suppose donc aussi d'étendre le contrat d'envoi.
- **Options :**
  - A : Retirer QuestionnaireScene du flow et du build → impact : ~110 lignes de code mort et une scène supprimées ; **le plus économique**
  - B : La câbler → impact : **extension du modèle de données** (`BlockConfig.questions`) **+ du contrat de PATCH** + saisie côté panneau de configuration. Dépend de Q-011 (quelles questions, quels textes) et de **Q-TRUST-1** (est-ce là que doit passer le questionnaire Trust in Technology ?)
- **Statut :** EN ATTENTE — à trancher **avec [Q-TRUST-1](#q-trust-1--le-questionnaire-trust-in-technology-est-il-dans-le-jeu-ou-en-dehors)**
- **Réponse :** —

### Q-FF-12 — Articulation `distal_forced_optimal_probability` vs `distal_advice_reliability`
- **Posée le :** 2026-05-26 (reportée ici le 2026-07-28 — elle n'existait que dans `specs/free-forced-choices/spec-fonc.md`)
- **Origine :** Analyse fonctionnelle, chantier free-forced-choices
- **Bloque :** Rien pour l'instant — résiduelle, non bloquante.
- **Question :** Comment s'articulent le paramètre d'optimalité du choix distal imposé et la fiabilité de l'advice distal ?
- **Statut :** EN ATTENTE
- **Réponse :** —

### Q-FF-13 — Le bloc tutorial doit-il présenter le mode forced ?
- **Posée le :** 2026-05-26 (reportée ici le 2026-07-28)
- **Origine :** Analyse fonctionnelle, chantier free-forced-choices
- **Bloque :** Rien pour l'instant — résiduelle, non bloquante.
- **Question :** Le tutorial doit-il démontrer le mode forced au participant, ou rester 100 % free (TR4 actuelle) ?
- **Statut :** EN ATTENTE
- **Réponse :** —

### Q-BACK-1 — Le retour en arrière doit-il être interdit ?
- **Posée le :** 2026-07-28
- **Origine :** Pilotage, revue de complétude (écart F2)
- **Bloque :** Cohérence protocole.
- **Question :** `specs-light.md` §4 indique que « le joueur ne peut pas aller sur une case déjà visitée ». Le code ne l'applique pas (`LevelRegistry.cs:266`) : seul le budget de pas (`overtime_steps`) pénalise les trajets non optimaux. Faut-il implémenter l'interdiction, ou acter que le budget de pas la remplace ?
- **Options :**
  - A : Acter que le budget de pas remplace l'interdiction → impact : aucun dev, une décision à enregistrer
  - B : Implémenter l'interdiction → impact : dev court, mais fait potentiellement doublon avec `overtime_steps`
- **Statut :** EN ATTENTE
- **Réponse :** —

### Q-DATA-1 — Que doit valoir l'acceptabilité pour un trial joué sans advisor ?
- **Posée le :** 2026-07-28 · **Reformulée le 2026-07-28** après vérification du code
- **Origine :** Pilotage, revue de complétude (défaut D1 requalifié, Lot A1)
- **Bloque :** 🔴 **Lot A1 — priorité absolue.** La formulation initiale (« que faire d'un questionnaire incomplet ») décrivait un aléa de comportement participant. Le vrai problème est systématique : la question d'acceptabilité **n'est délibérément pas posée** quand aucun advisor n'a été choisi (`TrialQuestionsUI.cs:91` — « elle n'a pas de sens si aucun advisor n'a été choisi »). Le champ reste donc vide (`FlowSerializationUtility.cs:45`), et `TrialManager.cs:119-124` **jette la ligne entière**. Résultat : **aucune trial de la condition `advisor = none` — la condition contrôle — n'arrive en base.**
- **Question :** Pour un trial où le participant n'a pas d'advisor, la question d'acceptabilité n'a pas de sens et n'est pas posée. Que doit contenir la colonne `acceptability_question` dans ce cas ?
- **Options :**
  - A : **Vide / `null` assumé** → impact : la ligne part avec le gameplay complet ; dans l'analyse, `null` sur cette colonne signifie « sans advisor » et se déduit de `advisor_choice`
  - B : **Une valeur « non applicable » explicite** (ex. `"n/a"`) → impact : distingue sans ambiguïté « non applicable » d'une non-réponse d'un participant qui a abandonné en cours de questionnaire
  - C : **Poser quand même la question sans advisor** (reformulée) → impact : change le protocole, à cadrer côté chercheur
- **Note :** quelle que soit l'option, le correctif A1 doit aussi couvrir le cas d'un participant qui abandonne réellement en cours de questionnaire — la ligne de gameplay ne doit jamais être perdue.
- **Statut :** EN ATTENTE
- **Réponse :** —

### Q-TIE-1 — Quand les deux nuages finissent à égalité, quel choix est « correct » ?
- **Posée le :** 2026-07-28
- **Origine :** Pilotage, revue de complétude — vérification du TDD §4.2.5 contre le code
- **Bloque :** 🟠 Validité de la variable dépendante `choice_correct` (et de `true_cloud`) sur une partie des trials.
- **Contexte :** Le nuage « correct » est celui qui a le plus de bugs verts au moment de la collecte. Le code écrit `_leftCloud.greenBugs > _rightCloud.greenBugs ? _leftCloud : _rightCloud` (`GameManager.cs:404`) : la comparaison est **stricte**, donc **en cas d'égalité c'est toujours le nuage de droite qui est déclaré correct**, sans tirage aléatoire.
- **Le cas est-il atteignable ?** **Oui**, et pas par un réglage exotique :
  - Au spawn, non : les deux nuages partagent le même `totalBugs` et leurs ratios verts sont séparés d'un écart `gap ≥ gap_min` (défaut 0.1), soit au moins 2 bugs verts d'écart. *Sauf si `gap_min` est mis à 0 côté configuration de session — auquel cas l'égalité est immédiate et systématique.*
  - **En cours de trial, oui.** Chaque pénalité retire 1 bug vert à **chaque** nuage, mais s'arrête à zéro (`BugCloud.AddBugs`). Une fois les deux nuages vidés, ils sont à égalité (0 = 0) et le nuage de droite gagne automatiquement. Avec les valeurs par défaut, le plus riche des deux nuages peut n'avoir que 10 bugs verts (`total = 20`, ratio 0.5) — or `trap_count` vaut 10 par défaut, auxquels s'ajoutent 1 pénalité par pas hors budget et 1 par touche invalide. **Un participant en difficulté atteint donc l'égalité à zéro.**
  - Conséquence : le biais se concentre précisément sur les trials où le participant a le plus mal performé.
- **Question :** Dans ce cas d'égalité, que doit valoir `choice_correct` ?
- **Options :**
  - A : **Tirage aléatoire du nuage correct** → impact : correctif dans `GetBestCloud()` ; conserve une chance sur deux, mais introduit du bruit non reproductible (à seeder)
  - B : **`choice_correct` = `null` / trial exclu de l'analyse** → impact : `GetBestCloud()` renvoie `null` (ce que le TDD décrivait à tort déjà), et `true_cloud` vaut `"none"` ; l'analyse doit filtrer ces trials
  - C : **`choice_correct` = vrai quel que soit le nuage collecté** (les deux valent autant, donc aucun mauvais choix) → impact : sémantiquement défendable, à acter explicitement
  - D : **Conserver le comportement actuel** (droite gagne) → impact : biais systématique assumé et documenté, à mentionner dans la publication
- **Note technique :** si l'option retenue n'est pas D, le correctif est de quelques lignes dans `GameManager.GetBestCloud()`. Voir aussi le point d'attention « Départage à égalité » dans `Docs/TDD.md` §4.2.6.
  ⚠️ **Complément 2026-07-28 (revue de couverture)** : une **seconde convention, contradictoire**, existe dans le codebase — `BugCloudGenerationUtility.BestCloudIndex` (`:27`) désigne le **premier** nuage à égalité, là où `GameManager` désigne celui de droite. Quelle que soit l'option retenue, les deux emplacements doivent être unifiés.
- **Statut :** EN ATTENTE
- **Réponse :** —

---

## Questions ouvertes par la revue de couverture bidirectionnelle (2026-07-28)

> Source : `Docs/project-state/revue-couverture-2026-07-28.md` — confrontation du code au **CSV de référence client** (`Docs/references/CSV_BUGS_Output_V1.xlsx`), confrontation jamais faite jusqu'ici.
> Ces sept questions portent toutes sur **la donnée de sortie** : le jeu fonctionne, mais une partie de ce que le protocole doit mesurer n'arrive pas en base.

### Q-MOTOR-1 — Que faut-il enregistrer du motor advice ? *(durcit Q-002)*
- **Posée le :** 2026-07-28
- **Origine :** Pilotage, revue de couverture (constats N1-A et N1-B, divergences D-007 et D-008)
- **Bloque :** 🔴 **Toute analyse impliquant le niveau moteur.** Q-002 était classée « traçabilité » depuis le 12/03/26. Après vérification du code, ce n'est pas un sujet de traçabilité : **c'est une manipulation expérimentale sans mesure.**
- **Contexte :** aujourd'hui, la ligne envoyée ne contient, pour le niveau moteur, que les **probabilités de configuration** (`motor_advice_visible_probability`, `motor_advice_reliable_probability`) et les paramètres de forçage. Ce qui s'est réellement passé sur le trial n'est nulle part : ni le set de touches **actif**, ni le set **affiché** au participant, ni le fait que le conseil ait été affiché, ni qu'il ait été fiable. Ces quatre valeurs existent bien en mémoire (`MotorAdviceController.ActiveSet` / `DisplayedSet` / `AdviceVisible` / `AdviceReliable`) — elles ne sont simplement jamais recopiées dans la ligne.
- **Conséquences concrètes :** on ne peut pas mesurer si le participant a suivi le conseil moteur ; on ne peut pas distinguer un trial où le conseil était absent d'un trial où il était présent mais trompeur ; la colonne `motor_choice_match_advice` du CSV de référence est donc **impossible à recalculer post-hoc**.
- **Question :** quelles colonnes le motor advice doit-il produire dans `trial_responses` ?
- **Options :**
  - A : **Les 4 valeurs réalisées** — set actif, set affiché, conseil affiché (oui/non), conseil fiable (oui/non) → impact : symétrie complète avec le distal (qui logge déjà `distal_advice_visible` et `distal_advice_reliable`) ; permet de recalculer l'adhérence moteur post-hoc
  - B : **Les 4 valeurs + une colonne `motor_choice_match_advice` pré-calculée** → impact : le CSV client est servi tel quel, aucun retraitement côté analyse
  - C : **Le minimum** — seulement « conseil affiché » et « conseil fiable » → impact : on sait quand le conseil est intervenu, mais pas ce que le participant a vu ni ce qu'il a fait ; l'adhérence reste non mesurable
- **Note :** l'option retenue conditionne aussi Q-002 (qui peut alors être close) et le lot E1/E3 de la revue de couverture.
- **Statut :** EN ATTENTE
- **Réponse :** —

### Q-DISTAL-1 — Faut-il enregistrer les valeurs réellement affichées sur l'écran distal ?
- **Posée le :** 2026-07-28
- **Origine :** Pilotage, revue de couverture (constat N1-C, divergence D-009)
- **Bloque :** 🔴 Analyse du choix distal — donc H6 (« higher levels of abstraction ») côté distal.
- **Contexte :** sur l'écran de scan des deux vallées, le jeu tire et affiche des quantités concrètes de bugs verts et rouges de chaque côté. La ligne enregistrée ne contient que **les bornes de configuration** (min/max total, min/max ratio, écart) et l'indication de la vallée objectivement la meilleure. **Les valeurs effectivement vues par le participant ne sont enregistrées nulle part.** On connaît donc la distribution dans laquelle le stimulus a été tiré, mais pas le stimulus lui-même.
- **Asymétrie :** côté forêt, les valeurs réelles des deux nuages **sont** bien enregistrées (colonne `map_config`). Le distal semble simplement avoir été oublié.
- **Question :** faut-il enregistrer, par bloc, les valeurs réellement affichées pour chaque vallée ?
- **Options :**
  - A : **Oui, comme pour les nuages** — total, verts et rouges de chaque vallée → impact : aligne le distal sur le proximal, sert les colonnes `left/right_valley_*_bugs_nb` du CSV de référence
  - B : **Oui, mais sous forme condensée** (un objet JSON par bloc, comme `map_config`) → impact : moins de colonnes, nécessite un parsing côté analyse
  - C : **Non** — les bornes de configuration suffisent → impact : à acter explicitement, avec la limite correspondante ajoutée au codebook
- **Statut :** EN ATTENTE
- **Réponse :** —

### Q-ROW-1 — Une ligne par écran ou une ligne par essai ?
- **Posée le :** 2026-07-28
- **Origine :** Pilotage, revue de couverture (§3)
- **Bloque :** 🟠 Interprétation de tout fichier exporté. Point jamais explicité au chercheur.
- **Contexte :** le tableau de référence fourni en début de projet (`CSV_BUGS_Output_V1.xlsx`) prévoit **une ligne par écran** — l'écran de choix de vallée a sa propre ligne, distincte de celles des essais en forêt (colonnes `screen_type`, `screen_id`). Le jeu produit **une ligne par essai** : c'est la décision **DEC-011** (table plate, export sans jointure). Les informations du choix de vallée sont donc **recopiées à l'identique sur chaque essai du bloc**.
- **Conséquence d'analyse :** toute moyenne ou tout comptage effectué naïvement sur les lignes surpondère les variables de niveau bloc (choix de vallée, conseil distal, questionnaire) proportionnellement au nombre d'essais du bloc.
- **Question :** confirmez-vous le format « une ligne par essai », avec recopie des informations de bloc ?
- **Options :**
  - A : **Oui, statu quo** → impact : aucun développement ; on ajoute au codebook un avertissement explicite et la liste des colonnes concernées
  - B : **Ajouter deux colonnes de repérage** (`screen_type`, `screen_id`) sans changer la granularité → impact : petit développement, facilite le filtrage côté analyse
  - C : **Revenir à une ligne par écran** → impact : refonte du modèle de données et de l'export ; remet en cause DEC-011
- **Statut :** EN ATTENTE
- **Réponse :** —

### Q-TRUST-1 — Le questionnaire « Trust in Technology » est-il dans le jeu ou en dehors ?
- **Posée le :** 2026-07-28
- **Origine :** Pilotage, revue de couverture (constat N1-D, divergence D-010)
- **Bloque :** 🔴 H7 (« Individual traits, such as trust in technology »). Conditionne aussi Q-QUEST-1.
- **Contexte :** le document de référence liste le *Trust in Technology Questionnaire* parmi les instruments de mesure. **DEC-005** avait tranché que les questionnaires pré/post sont hors périmètre du jeu (passation via Qualtrics, redirection en fin de session). Or il existe dans le jeu une scène « Questionnaire » à moitié construite, qui n'est **jamais atteinte** et dont le mécanisme de configuration n'existe pas. Les trois questions par essai (acceptabilité, contrôle, ressemblance humaine) sont, elles, bien collectées ailleurs et fonctionnent.
- **Question :** ce questionnaire doit-il être passé dans le jeu, ou reste-t-il hors périmètre ?
- **Options :**
  - A : **Hors périmètre, confirmé (DEC-005)** → impact : on retire la scène inutilisée du jeu ; le lien de redirection en fin de session sert déjà à cela. Le plus économique
  - B : **Dans le jeu, entre les blocs** → impact : développement à chiffrer — il faut créer le moyen pour vous de saisir les questions, l'écran de passation, et l'enregistrement des réponses (rien de tout cela n'existe aujourd'hui)
  - C : **Dans le jeu, une seule fois en fin de session** → impact : même développement, mais un seul point de passation
- **Statut :** EN ATTENTE
- **Réponse :** —

### Q-FREQ-1 — À quelle fréquence poser les trois questions, et sur quelle échelle ?
- **Posée le :** 2026-07-28
- **Origine :** Pilotage, revue de couverture (§4)
- **Bloque :** 🟠 Charge cognitive du participant, durée de passation, et comparabilité avec la littérature.
- **Contexte :** le document de référence dit « après **certains** essais ». Le jeu pose aujourd'hui **deux questions après chaque essai** (acceptabilité + contrôle) et une troisième au dernier essai du bloc (ressemblance humaine). **Il n'existe aucun réglage de fréquence.** Sur un bloc de 36 essais, cela représente **72 interruptions** par bloc.
  Deux détails d'implémentation jamais actés : l'échelle comporte **5 niveaux** (« pas du tout d'accord » → « tout à fait d'accord »), et **l'ordre des questions est mélangé** à chaque essai (de façon reproductible).
- **Question :** à quelle fréquence ces questions doivent-elles être posées, et sur quelle échelle ?
- **Options (fréquence) :**
  - A : **Après chaque essai, statu quo** → impact : aucun développement ; charge participant élevée
  - B : **Sur un sous-ensemble d'essais, réglable par bloc** → impact : développement modéré (un paramètre + un tirage) ; réduit la charge et rapproche du document de référence
  - C : **Une fois par bloc uniquement** → impact : beaucoup moins de données par participant
- **Options (échelle) :** 5 niveaux (statu quo) · 7 niveaux · autre — merci de préciser aussi les **libellés** attendus (cf. Q-011).
- **Statut :** EN ATTENTE
- **Réponse :** —

### Q-RANDOM-1 — L'ordre des advisors et l'apparence de l'advisor humain doivent-ils être contrôlés ?
- **Posée le :** 2026-07-28
- **Origine :** Pilotage, revue de couverture (constat N2-A, divergence D-012)
- **Bloque :** 🔴 Contrôle d'un confondant sur le méta-choix (H1), et cohérence de la présentation de l'advisor.
- **Contexte :** deux tirages aléatoires échappent aujourd'hui au mécanisme de reproductibilité du jeu (tout le reste du gameplay est reproductible à partir d'une graine enregistrée) :
  1. **L'ordre des trois options d'advisor** (aucun / humain / robot) est **remélangé à chaque affichage**. L'ordre présenté n'est ni reproductible, ni enregistré, ni spécifié dans aucun document. Or un effet de position (gauche / milieu / droite) est un biais classique, et il porte ici sur **le choix le plus en amont du protocole**. En l'état, il est **impossible de le vérifier ou de le corriger après coup**.
  2. **L'apparence de l'advisor humain** (homme ou femme) est tirée à pile ou face **indépendamment par chaque élément d'interface**. Conséquence : sur un même essai, l'indicateur de conseil peut montrer une femme pendant que l'encart d'explication montre un homme. Le document de référence s'interroge explicitement sur les biais liés à l'apparence des advisors.
- **Question :** que doit-il se passer pour chacun de ces deux points ?
- **Options (ordre des advisors) :**
  - A : **Randomisé, mais reproductible et enregistré** → impact : correctif simple ; permet de tester l'effet de position dans l'analyse. **Recommandé**
  - B : **Ordre fixe pour tous les participants** → impact : supprime la variance, mais installe un effet de position constant
  - C : **Contrebalancé entre participants** (ordre déterminé par l'identifiant participant) → impact : le plus propre méthodologiquement, développement légèrement supérieur
- **Options (apparence de l'advisor humain) :**
  - A : **Un seul genre pour tout le projet** → impact : le plus simple, supprime la variable
  - B : **Tiré une fois par participant, cohérent partout** → impact : permet de contrôler la variable dans l'analyse ; suppose de l'enregistrer
  - C : **Tiré une fois par bloc, cohérent partout** → impact : idem, plus proche du comportement actuel
- **Statut :** EN ATTENTE
- **Réponse :** —

### Q-HL-1 — La réponse « ressemblance humaine » doit-elle figurer sur toutes les lignes du bloc ?
- **Posée le :** 2026-07-28
- **Origine :** Pilotage, revue de couverture (constat N2-B, divergence D-011)
- **Bloque :** 🟠 Interprétation de la colonne `human_likeness_question`. **Ferme le point de vigilance PC-7.**
- **Contexte :** cette question n'est posée **qu'une fois par bloc** (au dernier essai). Le code recopie ensuite la réponse **sur toutes les lignes du bloc**. **DEC-013** et le dictionnaire de données annoncent au contraire qu'elle n'est renseignée que sur **la dernière ligne**. Le comportement du code est peut-être le bon — il évite d'avoir à faire une jointure pour analyser — mais il n'est écrit nulle part, et une moyenne calculée naïvement sur les lignes compte la réponse autant de fois qu'il y a d'essais dans le bloc.
- **Question :** quel comportement retenir ?
- **Options :**
  - A : **Recopie sur toutes les lignes, statu quo** → impact : aucun développement ; on corrige DEC-013 et le codebook, et on ajoute un avertissement d'analyse
  - B : **Uniquement sur la dernière ligne du bloc** (conforme à DEC-013) → impact : correctif simple ; l'analyse doit rattacher la réponse au bloc
- **Statut :** EN ATTENTE
- **Réponse :** —

---

## Questions répondues

- **Q-001, Q-003, Q-006** : RÉPONDU (cf. ci-dessus, 2026-03-16 à 2026-05-20)
- **Q-005, Q-FF-1 à Q-FF-9** : RÉPONDU le 2026-05-26 via DEC-019 (consolidation post-arbitrage chercheur sur free/forced choices, intégration des Notes 1/2/3). Spec fonc passée en statut `validé`.
- **Q-EXP-1, 2, 3, 4, 5, 7, 8, 9** : RÉPONDU le 2026-05-26 via DEC-018 (échange Florian / Valerian / Mark)
- **Q-EXP-6** : reformulée — UI test à mener (cf. ci-dessus, 2026-05-26)
- **Q-EXP-10** : EN ATTENTE — jamais abordée dans l'échange mail (localisation FR/multilingue)
