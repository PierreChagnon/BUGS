using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// -----------------------------
// IntroSceneController : glue entre le composant agnostique HowToPlayUI
// et le FlowController du projet, pour la scene IntroScene.
//
// Affiche les ecrans de regles de la session (Config.rules), recus depuis
// GET /api/sessions/[id]. Chaque rule = une image + un texte = une page de la modal.
//
// Comportement :
// - Start : telecharge les images des rules, construit les pages, ouvre la modal
// - Closed : appelle FlowController.OnPhaseComplete() (-> AdvisorChoice)
// - Pas de rules : skip silencieux + OnPhaseComplete direct pour ne pas bloquer
//
// Note : le header de la modal n'est pas alimente par les donnees (statique cote scene).
// -----------------------------
public class IntroSceneController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private HowToPlayUI _howToPlay;

    void Awake()
    {
        // Neutralise l'auto-show des dummy pages de HowToPlayUI avant son Start().
        // Tous les Awake() s'executent avant tous les Start() : la suppression est garantie,
        // ce qui evite le flash des placeholders pendant le download des images de session.
        if (_howToPlay != null) _howToPlay.MarkExternallyControlled();
    }

    void Start()
    {
        if (_howToPlay == null)
        {
            Debug.LogWarning("[IntroSceneController] _howToPlay non assigne. Skip phase Intro.");
            FlowController.Instance?.OnPhaseComplete();
            return;
        }

        var rules = FlowController.Instance?.Config?.rules;
        if (rules == null || rules.Count == 0)
        {
            Debug.Log("[IntroSceneController] Aucune regle pour cette session. Skip phase Intro.");
            FlowController.Instance?.OnPhaseComplete();
            return;
        }

        StartCoroutine(BuildAndShowRules(rules));
    }

    void OnDestroy()
    {
        if (_howToPlay != null) _howToPlay.Closed -= OnRulesClosed;
    }

    private IEnumerator BuildAndShowRules(List<RuleScreen> rules)
    {
        var pages = new List<HowToPlayPage>();

        foreach (var rule in rules)
        {
            Sprite sprite = null;
            bool done = false;
            ApiClient.Instance.FetchImage(
                rule.image_url,
                result => { sprite = result; done = true; },
                error =>
                {
                    Debug.LogWarning($"[IntroSceneController] Image non chargee ({rule.image_url}): {error}");
                    done = true;
                });

            while (!done)
                yield return null;

            pages.Add(new HowToPlayPage { image = sprite, header = rule.title, body = rule.text });
        }

        _howToPlay.SetPages(pages);
        _howToPlay.Closed += OnRulesClosed;
        _howToPlay.Open();
    }

    private void OnRulesClosed()
    {
        FlowController.Instance?.OnPhaseComplete();
    }
}
