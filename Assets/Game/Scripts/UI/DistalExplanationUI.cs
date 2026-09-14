using UnityEngine;

[DefaultExecutionOrder(50)]
public class DistalExplanationUI : AdviceExplanationUIBase
{
    protected override AdviceLevel Level => AdviceLevel.Distal;
    protected override bool ShowAdvisorBadgesFromExplanation => false;

    // Le Communication Report recouvre l'ecran a l'arrivee sur la scene distale : l'explanation
    // forced attend sa fermeture pour s'afficher, sinon le chrono de lecture compterait le temps
    // passe a lire le report. DistalChoiceUI rappelle Refresh() a la fermeture du panneau.
    protected override bool IsForcedExplanationDeferred()
    {
        var choiceUi = FindFirstObjectByType<DistalChoiceUI>();
        return choiceUi != null && choiceUi.IsEnteringExplanationBlockPanelOpen;
    }
}
