#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// -----------------------------
// Utilitaire dev : efface les PlayerPrefs de volume audio.
// Au prochain lancement, AudioManager ré-applique les _default* du prefab
// (puisque audio.defaults_version est aussi supprimé → reset forcé).
// -----------------------------

public static class AudioPrefsMenu
{
    [MenuItem("Tools/Audio/Reset Volume Prefs")]
    static void ResetVolumePrefs()
    {
        string[] keys =
        {
            "audio.master", "audio.music", "audio.ambience",
            "audio.sfx_gameplay", "audio.sfx_ui", "audio.defaults_version"
        };
        foreach (var k in keys)
            PlayerPrefs.DeleteKey(k);
        PlayerPrefs.Save();
        Debug.Log("[Audio] PlayerPrefs de volume réinitialisés. Le prochain lancement repart des défauts du prefab AudioManager.");
    }
}
#endif
