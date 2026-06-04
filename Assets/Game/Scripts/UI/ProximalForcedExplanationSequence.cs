using UnityEngine;
using UnityEngine.SceneManagement;

public static class ProximalForcedExplanationSequence
{
    static bool _proximalForcedClosed;
    static int _activeSceneHandle = int.MinValue;

    public static void EnsureSceneContext()
    {
        int currentSceneHandle = SceneManager.GetActiveScene().handle;
        if (currentSceneHandle == _activeSceneHandle)
            return;

        _activeSceneHandle = currentSceneHandle;
        _proximalForcedClosed = false;
    }

    public static bool ShouldHideForced(AdviceLevel level, FlowController flow)
    {
        if (level == AdviceLevel.Proximal && _proximalForcedClosed)
            return true;

        if (level != AdviceLevel.Motor)
            return false;

        if (_proximalForcedClosed)
            return false;

        var proximalState = flow != null ? flow.GetExplanationState(AdviceLevel.Proximal) : null;
        return proximalState != null && !proximalState.IsNone && !proximalState.IsOptIn;
    }

    public static void OnForcedClosed(AdviceLevel level)
    {
        if (level != AdviceLevel.Proximal)
            return;

        _proximalForcedClosed = true;
        RefreshMotorExplanationUI();
    }

    static void RefreshMotorExplanationUI()
    {
        var motorUis = Object.FindObjectsByType<MotorExplanationUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < motorUis.Length; i++)
        {
            var motorUi = motorUis[i];
            if (motorUi == null)
                continue;

            motorUi.Refresh();
        }
    }
}
