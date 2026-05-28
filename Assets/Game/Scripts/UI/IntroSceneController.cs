using System.Collections.Generic;
using UnityEngine;

// -----------------------------
// IntroSceneController : glue entre le composant agnostique HowToPlayUI
// et le FlowController du projet, pour la scene IntroScene.
//
// Etape 1 d'integration (Question 1 du integration-guide.md) :
// - Utilise une liste hardcoded de pages serialisees Inspector (_testPages)
//   au lieu d'aller chercher dans FlowController.Instance.Config.
// - L'integration complete avec le back-end (Question 2 du guide) viendra
//   dans un prochain commit : remplacement de _testPages par lecture depuis
//   FlowController.Instance.Config.intro_pages + DTO HowToPlayPageDto.
//
// Comportement :
// - Start : recupere les pages, les pousse dans HowToPlayUI, l'abonne a Closed
// - Closed : appelle FlowController.OnPhaseComplete() (-> AdvisorChoice)
// - Pas de pages : skip silencieux + OnPhaseComplete direct pour ne pas bloquer
// -----------------------------
public class IntroSceneController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private HowToPlayUI _howToPlay;

    [Header("Pages de test (provisoire — sera remplace par lecture FlowController.Config)")]
    [SerializeField] private List<HowToPlayPage> _testPages = new List<HowToPlayPage>();

    void Start()
    {
        if (_howToPlay == null)
        {
            Debug.LogWarning("[IntroSceneController] _howToPlay non assigne. Skip phase Intro.");
            FlowController.Instance?.OnPhaseComplete();
            return;
        }

        var pages = LoadPages();

        if (pages == null || pages.Count == 0)
        {
            Debug.LogWarning("[IntroSceneController] Pas de pages tutorial. Skip phase Intro.");
            FlowController.Instance?.OnPhaseComplete();
            return;
        }

        _howToPlay.SetPages(pages);
        _howToPlay.Closed += OnTutorialClosed;
        _howToPlay.Open();
    }

    void OnDestroy()
    {
        if (_howToPlay != null) _howToPlay.Closed -= OnTutorialClosed;
    }

    private void OnTutorialClosed()
    {
        FlowController.Instance?.OnPhaseComplete();
    }

    private IList<HowToPlayPage> LoadPages()
    {
        // TODO etape 2 : remplacer par
        //   var cfg = FlowController.Instance?.Config;
        //   return cfg?.intro_pages?.Select(dto => new HowToPlayPage { ... }).ToList();
        return _testPages;
    }
}
