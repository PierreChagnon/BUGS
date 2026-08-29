using UnityEngine;

// -----------------------------
// Affichage unique des badges advisor (humain homme/femme, robot), partagé
// par AdviceExplanationUIBase, DistalChoiceUI et MotorAdviceUI.
//
// Le genre du conseiller humain est tiré une seule fois par bloc, de façon
// seedée, dans FlowController (salt 6 → PlayerSessionState.advisor_display_is_male) :
// les trois UI affichent donc le même personnage sur tout le bloc. Le repli
// sandbox (FlowController absent) tire une seule valeur par lancement.
// -----------------------------

public static class AdvisorBadgeUtility
{
    public const string HumanMaleName = "AdvisorDisplayHumanMale";
    public const string HumanFemaleName = "AdvisorDisplayHumanFemale";
    public const string RobotName = "AdvisorDisplayRobot";

    static bool? _sandboxIsMale;

    public static bool ShowHumanMale
    {
        get
        {
            var flow = FlowController.Instance;
            if (flow != null && flow.State != null)
                return flow.State.advisor_display_is_male;

            _sandboxIsMale ??= Random.value < 0.5f;
            return _sandboxIsMale.Value;
        }
    }

    // Applique la visibilité aux trois badges (références directes).
    public static void ApplyBadges(
        GameObject humanMaleBadge,
        GameObject humanFemaleBadge,
        GameObject robotBadge,
        bool visible,
        AdvisorType advisorType)
    {
        bool showHuman = visible && advisorType == AdvisorType.Human;
        bool showRobot = visible && advisorType == AdvisorType.Robot;

        if (humanMaleBadge != null)
            humanMaleBadge.SetActive(showHuman && ShowHumanMale);
        if (humanFemaleBadge != null)
            humanFemaleBadge.SetActive(showHuman && !ShowHumanMale);
        if (robotBadge != null)
            robotBadge.SetActive(showRobot);
    }

    // Variante pour les racines dont les badges ne sont pas câblés dans
    // l'inspecteur : retrouve les trois enfants par leurs noms standard.
    public static void ApplyBadgesByName(Transform root, bool visible, AdvisorType advisorType)
    {
        if (root == null)
            return;

        ApplyBadges(
            FindDescendant(root, HumanMaleName),
            FindDescendant(root, HumanFemaleName),
            FindDescendant(root, RobotName),
            visible,
            advisorType);
    }

    public static GameObject FindDescendant(Transform root, string childName)
    {
        if (root == null)
            return null;

        foreach (Transform child in root)
        {
            if (child.name == childName)
                return child.gameObject;

            GameObject match = FindDescendant(child, childName);
            if (match != null)
                return match;
        }

        return null;
    }
}
