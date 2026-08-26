# Matrice de risques

> # ⚠️ DOCUMENT ARCHIVÉ — gelé au 03/03/2026
>
> **Ne reflète plus l'état du projet.** R1 à R4 sont obsolètes :
> R1 (state leaking) est couvert par la spec tech multi-écran · R2 (`GetCommandLineArgs` en WebGL)
> est résolu par DEC-009 · R4 (complexité FlowController) est levé, le composant est livré.
> R3 (perte de trials à la fermeture du navigateur) reste **réel et non corrigé** — il est repris
> en Lot A dans la revue de complétude.
>
> **Source de vérité actuelle :** section « Risques actifs » de `Docs/project-state/avancement.md`,
> détaillée dans `Docs/project-state/revue-completude-2026-07-28.md`.
>
> Conservé pour l'historique. Ne pas mettre à jour.

---

> Mis à jour par le Rôle 4 (Planification) et le Rôle 1 (Pilotage).
> Dernière mise à jour : 2026-03-03 — **ARCHIVÉ le 28/07/2026**

---

| # | Risque | Impact | Probabilité | Mitigation | Statut |
|:---|:---|:---|:---|:---|:---|
| R1 | State leaking entre trials dans le flow multi-écran | Élevé — données recherche corrompues | Moyenne | Spec tech doit définir une stratégie de cleanup explicite par composant. Pattern ICleanable envisagé. | À couvrir |
| R2 | WebGL + GetCommandLineArgs non fonctionnel | Élevé — params recherche non injectables | Faible | Valider avec le dashboard. Fallback : URL query params via bridge JS→Unity. | À valider |
| R3 | Perte de trials si fermeture navigateur | Moyen — données de session perdues | Moyenne | Pas de solution en place. Options : localStorage WebGL, envoi trial par trial, heartbeat. | Connu |
| R4 | Complexité du FlowController multi-écran | Élevé — retard planning | Moyenne | Découper en tickets infrastructure d'abord. Prototype minimal avant les écrans complexes. | À planifier |
| R5 | Cohérence inter-specs si produites à des moments différents | Moyen — contradictions entre specs | Faible | Matrice de traçabilité CSV dans chaque spec fonc. Revue croisée avant validation. | Processus en place |
