using UnityEditor;
using UnityEngine.Windows;

public static class AssetBundleBuilder {
    [MenuItem("Assets/Build AssetBundles")]
    private static void BuildAssetBundles() {
        Directory.Delete("AssetBundles");
        Directory.CreateDirectory("AssetBundles");

        BuildPipeline.BuildAssetBundles("AssetBundles", BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);
    }
}