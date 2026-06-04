using UnityEngine;

[DefaultExecutionOrder(50)]
public class DistalExplanationUI : AdviceExplanationUIBase
{
    protected override AdviceLevel Level => AdviceLevel.Distal;
    protected override bool ShowAdvisorBadgesFromExplanation => false;
}
