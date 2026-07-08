# Protocole de sessions de test — scénarios & sorties attendues

> **But** : dérouler des runs reproductibles et confronter la sortie au CSV. À exécuter par Florian/l'équipe (build + play — hors capacité Unity-blind).
> **Pré-requis** : sessions A/B/C prêtes dans Supabase (`config-session-test.md`), accès export CSV.

---

## 0. Journal de run (à remplir pour chaque exécution)

| Run | Date | Session | `sessionId` | `trial_seed` | Build/Éditeur | Opérateur | CSV exporté (lien) |
| :-- | :-- | :-- | :-- | :-- | :-- | :-- | :-- |
| | | | | | | | |

---

## 1. Scénario NOMINAL (Axes 1, 2, 3, 5)
1. Lancer la **Session A** (couverture déterministe), seed fixe.
2. Jouer **tous** les blocs jusqu'à EndSession, en variant les comportements pour couvrir : choix left/right, suivre / ne pas suivre le chemin affiché, cliquer / ne pas cliquer une explication opt-in, toucher / éviter les pièges.
3. Répondre au questionnaire à chaque bloc.
4. Exporter le CSV.
5. **Vérifs** :
   - [ ] 1 ligne par trial non-tutorial ; aucune ligne pour les blocs tutorial.
   - [ ] `matrice-tracabilite.md` : renseigner `CSV ?` + `Statut` pour les 80 champs.
   - [ ] Règles de cohérence inter-champs (Axe 2) OK sur chaque ligne.
   - [ ] Matrice de couverture (`config-session-test.md` §1) complétée à 100 %.

### Attendus déterministes (seed fixe) à pré-calculer
> À remplir une fois le seed choisi ; sert d'oracle.

| Trial | `map_config` attendu | `cloud_distance` | `optimal_path_length` | `true_cloud` | Autres |
| :-- | :-- | :-- | :-- | :-- | :-- |
| | | | | | |

---

## 2. Scénario BOUT-EN-BOUT réaliste (Axe 5)
1. Lancer la **Session B**, ≥ 2 participants (relancer avec un nouveau `participant_id`).
2. Exporter le CSV.
3. **Vérifs** :
   - [ ] `participant_id` uniques, aucune contamination croisée.
   - [ ] Timestamps ISO parsables ; `started_at ≤ ended_at`.
   - [ ] **Échappement CSV** des colonnes JSON : ouvrir `map_config` et `player_path_log` dans un parseur ; vérifier virgules/guillemets/retours-ligne non cassés, UTF-8 OK, pas de troncature.
   - [ ] Comptes de lignes = Σ `trial_count` des blocs non-tutorial.

---

## 3. Scénario RÉSILIENCE / PERTE (Axe 4)
Pour chaque cas : provoquer la panne, puis vérifier dans le CSV ce qui manque, et **acter** le comportement avec le chercheur.

| Cas | Manip | Attendu code | À vérifier dans le CSV | Acté ? |
| :-- | :-- | :-- | :-- | :-: |
| R1 Réseau coupé pendant envoi | Couper le réseau à la fin d'un trial | 3 retries (1 s) puis échec ; ⚠️ file **bloquée** (`ApiClient.cs:349-353`) | Trial(s) manquant(s) ? les suivants aussi ? | |
| R2 Réseau rétabli après coup | Rétablir le réseau | Reprise de la file au prochain déclenchement | Le(s) trial(s) en attente arrivent-ils ? | |
| R3 Questionnaire incomplet | Laisser une réponse vide | Envoi **annulé** (`TrialManager.cs:119-124`) | Ligne absente | |
| R4 Fermeture navigateur | Fermer en cours de session | File en mémoire perdue | Trials non envoyés perdus ? | |
| R5 Reload de scène | Recharger la scène en plein trial | Row courante perdue | Trial en cours perdu ? | |
| R6 PATCH human-likeness | Terminer un bloc | PATCH sur la **bonne** ligne | `human_likeness_question` renseignée sur la ligne attendue du bloc | |

> Consigner chaque perte mesurée dans `journal-divergences.md` (sévérité + mitigation proposée).

---

## 4. Scénario REPRODUCTIBILITÉ (Axe 6)
1. Lancer la **Session C** avec un seed S. Exporter → CSV#1.
2. Relancer à l'identique avec le **même** seed S. Exporter → CSV#2.
3. **Vérifs** :
   - [ ] Diff CSV#1/CSV#2 **hors** timestamps et input humain ⇒ `map_config`, `cloud_distance`, `optimal_path_length`, tirages de condition **identiques**.
   - [ ] ⚠️ `trial_seed` : vérifier qu'il est cohérent trial par trial et qu'il **régénère** le trial (pas un seed de session constant). Tester la précédence `SessionManager` vs `FlowController`.

---

## 5. Sortie
- Une fois tous les scénarios passés : reporter le bilan dans `campagne-test-validation.md` §4 (critères de sortie) et mettre à jour `gates-et-questions.md`.
