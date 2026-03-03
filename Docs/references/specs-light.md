# Specs Light — Notes de cadrage client

> Document de référence — ne pas modifier.
> Source : notes brutes du chercheur, consolidées en début de projet.
> Emplacement prévu : `Docs/references/specs-light.md`

---

## 1. Boucle principale (Forest Screen)

### Positionnement et distances

- Le personnage est à **égale distance des deux cibles** (nuages de bugs).
- Il existe un **trajet optimal** pour atteindre chacune des cibles (même nombre de key press par l'optimal).
- La **structure du chemin** est tirée aléatoirement dans une longueur donnée. Autant de changements de direction que souhaité — le trajet sera prolongé et le joueur n'empruntera pas le meilleur chemin.
- La contrainte "même variété dans les mêmes types de keypress" est **retirée**.
- **Variété dans la longueur des trajets** entre les maps. Les cibles sont positionnées aléatoirement à distance comprise dans un intervalle.

### Caméra

- Positionnée **pile au-dessus de la map** (top-down view) ou légèrement inclinée si plus joli.

### Obstacles

Deux types d'obstacles sur la map :

| Type | Pénalité | Visible à travers le fog of war |
|:---|:---|:---|
| **Walls** (murs) | Non | Oui |
| **Traps** (pièges) | Oui | Non |

> ⚠️ *"2 types d'obstacles : no tasks planned for this point — to be checked"*

---

## 2. Nuages d'insectes (Bug Clouds)

### Positionnement

- **Distance minimale** entre les 2 nuages (discrimination purpose) — enforced par la gestion de distance au joueur.
- **Distance au joueur toujours la même.**
- **Toujours spawn sur la même rangée** pour que le participant n'ait pas l'impression qu'un nuage est plus éloigné.

### Mécanique de récompense

- Chaque nuage a une **valeur de récompense différente** : le nombre de bugs verts varie entre nuages.
- **Même nombre total de bugs** (cohérence perceptuelle), seul le **ratio vert/rouge varie** (configurable).
- Les bugs **s'envolent en punition** → la quantité dans les nuages diminue.
- Les **deux cibles perdent** des bugs en fonction de la performance du joueur → la meilleure cible reste la meilleure.
- Quand un piège est touché : **les deux nuages perdent 1 bug vert chacun.** *(Implémentation actuelle : le total de bugs est diminué sur les deux et un ratio est calculé en fin de trial.)*
- Un **feedback visuel** doit montrer ce qui se passe.
- La **quantité de particules ne change pas** (visuel constant).
- Les cibles doivent avoir un **paramètre de visibility noise** pour rendre la discrimination plus difficile (blur, saturation, ou ratio vert/rouge).

---

## 3. Fog of War

- **La tuile entière** doit être visible (pas de révélation partielle).
- **En fin de trial**, révéler toute la map.
- Le fog of war révèle uniquement la **tuile active et les tuiles déjà visitées**. Les tuiles voisines ne sont **pas** visibles.

---

## 4. Déplacements

- Les obstacles **ne bloquent pas** le joueur — il les surmonte, passe dessus, et subit le malus.
- Le joueur **ne peut pas revenir en arrière**. Il ne peut pas aller sur une case déjà visitée.
- Le joueur **démarre toujours à la même position**.
- Pour se déplacer, le joueur utilise le clavier avec **un set de touches actif par trial**, suivant le pattern directionnel ←↑→ :
  - **QZD** ou **FTH** ou **KOM**
- Le set actif est **tiré aléatoirement à chaque trial**.
- Le joueur n'a **aucune indication** du set actif sauf par essai ou via le motor-advice.

---

## 5. Chemins optimaux

- Un chemin optimal = **nombre minimum de pas** pour atteindre la cible, **sans piège** sur le chemin. Il mène à la cible avec la **plus haute récompense**.
- Il faut **positionner au moins un obstacle sur tous les autres chemins optimaux non sélectionnés** à ce tour.

> ⚠️ *"still right?? if yes that means that there is only 2 ways to go the target without hitting something → impossible to win without advice — no tasks planned for this point — à spécifier"*

---

## 6. Objectif du joueur

Parcourir un environnement et faire les bons choix pour collecter le **plus grand nombre de bugs verts** et le **moins de bugs rouges**.

La quantité de bugs verts dans les nuages varie en fonction de trois niveaux de choix :

1. **Distal-choice** (chaque bloc) — La zone choisie a une tendance générale de récompense.
2. **Proximal-choice** (chaque trial) — Dans la forêt, les nuages apparaissent avec plus ou moins de bugs, en respectant la tendance générale de la zone.
3. **Motor-choice** (chaque trial) — La quantité finalement collectée peut diminuer selon la performance (obstacles rencontrés, chemin non-optimal emprunté).

---

## 7. Système de choix

| # | Choix | Type | Description | Environnement |
|:---|:---|:---|:---|:---|
| 1 | **Meta-choice** | Choix d'advisor | Le joueur choisit un advisor pour le prochain bloc | Vaisseau au lancement puis Mountain Screen |
| 2 | **Distal-choice** | Choix de zone | Choisir la zone où ramasser les bugs (parmi 2) | Mountain Screen |
| 3 | **Proximal-choice** | Choix de cible | Choisir l'essaim à collecter dans la forêt | Forest Screen |
| 4 | **Motor-choice** | Choix de touches | Choix des touches pour se déplacer | Forest Screen |

### Mécanique free/forced

- Pour certains blocs et trials, les meta-choices, distal choices, proximal choices et motor choices **seront forcés**.
- Un mécanisme **free/forced** est nécessaire.

### Explanations

- Quand un advice est donné, il est **accompagné d'une explication**.
- L'explication peut être **"short"** ou **"long"**.
- Le **texte de l'explication** doit être contrôlable et éditable par les chercheurs.

> ⚠️ *"Note sur les Explanations : TBC !"*

---

## 8. Écrans

### Mountain Screen (meta & distal choice)

- Le joueur sélectionne la zone à explorer : **choisir le côté avec le plus de bugs overall**.
- Une zone doit être **plus récompensante en moyenne** tout au long de la phase forêt (paramétrable / configurable).

### Forest Screen (proximal & motor choice)

*(Détaillé dans les sections précédentes.)*

---

## 9. Système d'advisors

| Advisor | Fonction | Écran |
|:---|:---|:---|
| **Motor-advice** | Donne les bindings pour se déplacer | Forest Screen |
| **Proximal-advice** | Montre le meilleur chemin vers le meilleur nuage | Forest Screen |
| **Distal-advice** | Montre la meilleure zone | Mountain Screen |

### Proximal Advice — détails

- Pointe **toujours vers la cible avec la plus haute récompense**.
- À **fiabilité maximale** : montre le chemin optimal (pas minimal, sans piège).
- **Advice incorrect** : montre un chemin sub-optimal (avec pièges et/ou longueur non minimisée, ou les deux).
- Peut être **triggered**.
- Doit avoir un **paramètre de fiabilité**.
- Fourni **périodiquement** suivant un pattern donné (aléatoire ?).

### Motor Advice — détails

- 3 sets de touches disponibles ←↑→ : **QZD, FTH, KOM** (fonctionnent comme des groupes).
- Fourni **périodiquement** suivant un pattern donné (aléatoire ?).
- Montre au joueur le **set actif**.
- **Coût quand le joueur se trompe de touche** :
  - Si mauvaise touche et aucun mouvement → les nuages perdent 1 bug chacun.
  - Si la touche fonctionne mais mène à un piège ou un chemin plus long → double coût.
- Peut être **non fiable** (paramètre) : un motor advice non fiable **montre le mauvais set de touches actives**.

### Motor Advice — Q&A avec le client

| Question | Réponse |
|:---|:---|
| Les touches sont les mêmes pour tous les trials ? | **OUI** |
| Les combinaisons touche-direction changent ? Quand ? | **OUI, à chaque trial** le set actif change |
| Quelle info donnée au joueur ? Un seul key/direction ou tous ? | **TOUTES les combinaisons** |
| Seulement les "actives" ? | **NON, un seul set complet** |
| Paramètre de fiabilité comme le proximal advice ? | **OUI** |
| Comment se comporte un motor advice non fiable ? | **Montre le mauvais set de touches actives** |
| L'exhaustivité de l'advice est aussi un paramètre ? | **NON** — l'advice est exhaustif |

---

## 10. Questionnaires en jeu

Le joueur peut évaluer après certains trials :

- **Sense of agency** — niveau de contrôle ressenti
- **Acceptability** — niveau d'acceptation de l'advisor
- **Human-likeness** — ressemblance humaine de l'advisor

---

## 11. Game Flow

- **Transition fluide entre maps (trials)** : camera pan vertical vers le haut, le personnage marche automatiquement de sa position vers la prochaine position de départ, puis le contrôle est rendu au participant.
- Enchaînement de **plusieurs essais dans un bloc**.
- Enchaînement de **plusieurs blocs**.
- Le joueur peut **accepter/refuser le consentement**.
- Le joueur peut **choisir de terminer** le jeu/expérience à tout moment.

---

## 12. Paramètres d'entrée (résumé)

| Paramètre | Accessible au chercheur | Moment |
|:---|:---|:---|
| Nombre de pièges sur la map | Oui (research panel) | Runtime |
| Distance joueur ↔ cibles (min-max) | Oui (research panel) | Runtime |
| Taille de la map | Non (fixée avant build) | Build time |
| Paramètres de génération des récompenses des nuages | Oui (research panel) | Runtime |
| Distance minimale entre cibles | Non (fixée avant build) | Build time |
| Fiabilité du proximal advice | Oui (research panel) | Runtime |
| Fréquence/distribution du proximal advice | Oui (research panel) | Runtime |
| Nombre de blocs | Oui (research panel) | Runtime |
| Nombre de trials par bloc | Oui (research panel) | Runtime |
