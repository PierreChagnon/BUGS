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
- **Bloque :** Traçabilite data et spec fonc finale
- **Question :** Quelles colonnes CSV exactes doivent enregistrer le set actif, l affichage de l advice, sa fiabilite et le set affiche ?
- **Options :**
  - A : Ajouter 4 colonnes dediees → impact : mise a jour schema CSV
  - B : Reutiliser colonnes existantes → impact : mapping a clarifier
- **Statut :** EN ATTENTE
- **Réponse :** —

### Q-003 — Le motor advice doit-il inclure une explanation ?
- **Posée le :** 2026-03-12
- **Origine :** Analyse fonctionnelle, chantier Motor Advice
- **Bloque :** Design UI et contenus
- **Question :** Doit on afficher un texte d explanation (short/long) en plus du set de touches ?
- **Options :**
  - A : Oui, explanation requise → impact : contenus a definir
  - B : Non, set uniquement → impact : UI minimaliste
- **Statut :** EN ATTENTE
- **Réponse :** —

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
- **Statut :** EN ATTENTE
- **Réponse :** —

### Q-006 — Explanations associées aux advice (short/long)
- **Posée le :** 2026-05-12
- **Origine :** Pilotage, revue d'avancement vs GDD 2.0
- **Bloque :** Hypothèse H8 (explanations × abstraction). Conception UI et système d'édition des textes.
- **Question :** Le GDD prévoit que chaque advice peut être accompagné d'une explanation "short" ou "long", éditable par le chercheur. Comment souhaitez-vous gérer ces contenus ?
- **Options :**
  - A : **Catalogue de textes éditables côté chercheur** (BDD ou fichier de config) → impact : système CMS-like, édition sans rebuild, plus de flexibilité
  - B : **Textes hardcodés validés en amont** par le chercheur → impact : pas d'outil d'édition, mais nécessite un cycle de validation à chaque modification
  - C : **Génération paramétrée** (templates avec variables) → impact : compromis flexibilité/simplicité, mais nécessite une grammaire de templates
- **Statut :** EN ATTENTE
- **Réponse :** —

### Q-007 — Modèle de perte de bugs en cas de piège
- **Posée le :** 2026-05-12
- **Origine :** Pilotage, revue d'avancement vs GDD 2.0
- **Bloque :** Cohérence protocole. Résultats comparables au GDD.
- **Question :** Le GDD spécifie "When a trap is hit both clouds loose 1 green bug". L'implémentation actuelle décrémente le total de bugs sur les deux clouds et recalcule un ratio en fin de trial. Quel modèle conserve-t-on ?
- **Options :**
  - A : **Aligner sur le GDD** : -1 green bug par cloud par piège → impact : refonte du modèle de pénalité côté GameManager + BugCloud, alignement avec le protocole publié
  - B : **Conserver l'implémentation actuelle** : décrément du total, recalcul du ratio → impact : mettre à jour le GDD, justifier le choix scientifiquement
  - C : **Autre modèle** à préciser → impact : à définir
- **Statut :** EN ATTENTE
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

---

## Questions répondues

_(aucune pour l'instant)_
