# Journal des divergences & anomalies

> **But** : consigner tout écart constaté pendant la campagne (contrat, valeurs, couverture, perte de données), avec sévérité et action.
> **Alimenté par** : Axes 1–6. **Source de vérité** : le code (`TrialResponseRow`).

## Échelle de sévérité
- 🔴 **Bloquant** : donnée fausse, manquante ou perdue silencieusement → l'analyse chercheur est compromise.
- 🟠 **Majeur** : donnée exploitable mais ambiguë / mal nommée / partielle → risque d'erreur d'analyse.
- 🟡 **Mineur** : cosmétique / documentaire → à corriger sans urgence.

## Registre

| # | Date | Axe | Champ / zone | Type (manquant / renommé / type / valeur / perte / couverture) | Constat | Attendu (code) | Sévérité | Action / chantier | Statut |
| :- | :-- | :-- | :-- | :-- | :-- | :-- | :-: | :-- | :-- |
| D-001 | | | | | | | | | |

## Points chauds pré-identifiés (à confirmer / infirmer pendant la campagne)

| Réf | Zone | Hypothèse à tester | Sévérité potentielle |
| :-- | :-- | :-- | :-: |
| PC-1 | Noms de colonnes | Dérive code (`advisor_forced`, `acceptability_question`…) ↔ doc (`meta_choice_is_forced`, `q1_response`…) : que reçoit réellement le chercheur ? | 🔴 |
| PC-2 | Résilience réseau | `ProcessPendingTrialRequests` `break` au 1er échec ⇒ file entière bloquée (`ApiClient.cs:349-353`) | 🔴 |
| PC-3 | Persistance | File d'envoi en mémoire, non persistée ⇒ perte à la fermeture / crash | 🔴 |
| PC-4 | Reproductibilité | `trial_seed` = `session.randomizationSeed` (potentiellement constant) au lieu du seed per-trial (`TrialManager.cs:269`) | 🟠 |
| PC-5 | Précédence config | `SessionManager` prime sur `FlowController.ActiveMapConfig` dans `BuildBaseRow` | 🟠 |
| PC-6 | Sérialisation | Champs null omis du JSON (hors `[IncludeNullInJson]`) ⇒ colonnes absentes sur certaines lignes | 🟠 |
| PC-7 | PATCH questionnaire | `human_likeness_question` patché « par bloc » : atterrit-il sur la bonne ligne ? | 🟠 |
| PC-8 | Complétude données | Positions pièges & bugs verts nuage non-choisi non exportés | 🟡 |
| PC-9 | Sémantique | `followed_advisor_path` = chemin affiché, pas optimal | 🟡 (doc) |
