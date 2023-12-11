using UnityEditor;
using UnityEngine.Windows;

public static class AssetBundleBuilder {
    [MenuItem("Assets/Build AssetBundles")]
    private static void BuildAssetBundles() {
        if (!Directory.Exists("AssetBundles")) {
            Directory.CreateDirectory("AssetBundles");
        }

        BuildPipeline.BuildAssetBundles(
            "AssetBundles",
            BuildAssetBundleOptions.None,
            EditorUserBuildSettings.activeBuildTarget
        );
    }
}
