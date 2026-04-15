// SteamManager — Steamworks initialization, callback loop, and shutdown.
// Requires Steamworks.NET: https://github.com/rlabrecque/Steamworks.NET
//
// SETUP: Import Steamworks.NET into Assets/Plugins/Steamworks.NET/ and add
//        STEAMWORKS to Player Settings > Scripting Define Symbols for
//        Standalone (Windows/Mac/Linux) build targets.

using UnityEngine;

#if STEAMWORKS
using Steamworks;
#endif

public class SteamManager : MonoBehaviour
{
    public static SteamManager Instance { get; private set; }
    public static bool Initialized { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

#if STEAMWORKS
        if (!Packsize.Test())
        {
            Debug.LogError("[Steam] Packsize test failed — wrong Steamworks.NET version");
            return;
        }
        if (!DllCheck.Test())
        {
            Debug.LogError("[Steam] DllCheck test failed — steam_api DLL mismatch");
            return;
        }

        try
        {
            if (SteamAPI.RestartAppIfNecessary(SteamAppId.Value))
            {
                Debug.Log("[Steam] Restarting via Steam client...");
                Application.Quit();
                return;
            }
        }
        catch (System.DllNotFoundException e)
        {
            Debug.LogError("[Steam] steam_api DLL not found: " + e.Message);
            return;
        }

        Initialized = SteamAPI.Init();
        if (!Initialized)
        {
            Debug.LogError("[Steam] SteamAPI.Init() failed — is Steam running?");
            return;
        }

        Debug.Log("[Steam] Initialized — user: " + SteamFriends.GetPersonaName());
#else
        Debug.Log("[Steam] STEAMWORKS not defined — skipping init");
#endif
    }

    void Update()
    {
#if STEAMWORKS
        if (Initialized)
            SteamAPI.RunCallbacks();
#endif
    }

    void OnApplicationQuit()
    {
#if STEAMWORKS
        if (Initialized)
        {
            SteamAPI.Shutdown();
            Debug.Log("[Steam] Shutdown complete");
        }
#endif
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}

#if STEAMWORKS
/// <summary>
/// Your Steam App ID. Replace with your actual ID from Steamworks partner site.
/// During development, steam_appid.txt in the project root overrides this.
/// </summary>
public static class SteamAppId
{
    // 480 = Spacewar (Valve's test app) — replace with your real App ID
    public static readonly AppId_t Value = new AppId_t(480);
}
#endif
