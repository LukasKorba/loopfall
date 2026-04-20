using UnityEditor;
using UnityEngine;

public static class TutorialTools
{
    [MenuItem("Loopfall/Reset Tutorial Flag")]
    public static void ResetTutorialFlag()
    {
        PlayerPrefs.DeleteKey("HasSeenTutorial");
        PlayerPrefs.DeleteKey("HasPlayed");
        PlayerPrefs.Save();
        Debug.Log("[Loopfall] Tutorial flag cleared — tutorial will play on next run.");
    }
}
