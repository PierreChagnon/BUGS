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
- **Statut :** EN ATTENTE
- **Réponse :** —

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

---

## Questions répondues

_(aucune pour l'instant)_
