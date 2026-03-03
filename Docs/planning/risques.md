# Matrice de risques

> Mis à jour par le Rôle 4 (Planification) et le Rôle 1 (Pilotage).
> Dernière mise à jour : 2026-03-03

---

| # | Risque | Impact | Probabilité | Mitigation | Statut |
|:---|:---|:---|:---|:---|:---|
| R1 | State leaking entre trials dans le flow multi-écran | Élevé — données recherche corrompues | Moyenne | Spec tech doit définir une stratégie de cleanup explicite par composant. Pattern ICleanable envisagé. | À couvrir |
| R2 | WebGL + GetCommandLineArgs non fonctionnel | Élevé — params recherche non injectables | Faible | Valider avec le dashboard. Fallback : URL query params via bridge JS→Unity. | À valider |
| R3 | Perte de trials si fermeture navigateur | Moyen — données de session perdues | Moyenne | Pas de solution en place. Options : localStorage WebGL, envoi trial par trial, heartbeat. | Connu |
| R4 | Complexité du FlowController multi-écran | Élevé — retard planning | Moyenne | Découper en tickets infrastructure d'abord. Prototype minimal avant les écrans complexes. | À planifier |
| R5 | Cohérence inter-specs si produites à des moments différents | Moyen — contradictions entre specs | Faible | Matrice de traçabilité CSV dans chaque spec fonc. Revue croisée avant validation. | Processus en place |
