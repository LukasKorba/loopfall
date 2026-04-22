#if UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

/// <summary>
/// After Unity generates the iOS / tvOS / macOS Xcode project, enable the iCloud Key-Value Storage
/// entitlement so NSUbiquitousKeyValueStore (used by ICloudKVStore.cs) is allowed at runtime.
///
/// macOS requires "Create Xcode Project" in Player Settings — direct .app builds have no
/// project to post-process. The developer still owns the App ID's iCloud capability toggle
/// in Apple Developer portal and the matching provisioning profile; this post-processor
/// only writes the entitlement key that the OS checks when the app asks for the ubiquity KV store.
/// </summary>
public static class ICloudPostProcess
{
    private const string KV_STORE_KEY = "com.apple.developer.ubiquity-kvstore-identifier";
    private const string KV_STORE_VALUE = "$(TeamIdentifierPrefix)$(CFBundleIdentifier)";

    [PostProcessBuild(900)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS && target != BuildTarget.tvOS && target != BuildTarget.StandaloneOSX) return;

        string projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
        PBXProject proj = new PBXProject();
        proj.ReadFromFile(projectPath);

        string mainTargetGuid = proj.GetUnityMainTargetGuid();

        // Reuse an existing entitlements file if one is already wired (by Unity, the user's manual
        // Xcode setup, or another post-processor). AddBuildProperty would APPEND our filename to
        // the existing one, producing "First.entitlements Second.entitlements" — a malformed path
        // that breaks code signing and the Signing & Capabilities UI.
        string existing = proj.GetBuildPropertyForAnyConfig(mainTargetGuid, "CODE_SIGN_ENTITLEMENTS");
        string defaultName = target == BuildTarget.StandaloneOSX
            ? PlayerSettings.productName + ".entitlements"
            : "Unity-iPhone.entitlements";
        string entitlementsFileName = !string.IsNullOrEmpty(existing) ? existing : defaultName;

        string entitlementsPath = Path.Combine(pathToBuiltProject, entitlementsFileName);

        PlistDocument entitlements = new PlistDocument();
        if (File.Exists(entitlementsPath))
            entitlements.ReadFromFile(entitlementsPath);
        else
            entitlements.root = new PlistElementDict();

        entitlements.root.SetString(KV_STORE_KEY, KV_STORE_VALUE);
        entitlements.WriteToFile(entitlementsPath);

        if (string.IsNullOrEmpty(existing))
        {
            proj.AddFile(entitlementsFileName, entitlementsFileName);
            proj.SetBuildProperty(mainTargetGuid, "CODE_SIGN_ENTITLEMENTS", entitlementsFileName);
        }

        proj.WriteToFile(projectPath);
    }
}
#endif
