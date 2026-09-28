using UnityEngine;

// -----------------------------
// Slot de badges advisor (AdvisorDisplayHumanMale / HumanFemale / Robot).
//
// Par defaut, le slot masque ses badges : un panneau qui reutilise une boite
// contenant ce slot (ex. QuestionPanel sur la base d'ExplanationBox) n'affiche
// donc aucun badge, sans reglage dans l'editeur. Seul un script qui le pilote
// (AdviceExplanationUIBase) le marque IsDriven, et decide alors seul du badge
// affiche.
// -----------------------------

public class AdvisorBadgesSlot : MonoBehaviour
{
    public bool IsDriven { get; set; }

    void OnEnable()
    {
        if (!IsDriven)
            AdvisorBadgeUtility.ApplyBadgesByName(transform, false, AdvisorType.None);
    }
}
