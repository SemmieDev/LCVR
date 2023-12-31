using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace LCVR {
    [BepInPlugin("semmiedev.lcvr", "LCVR", "1.0.0")]
    public class LCVR : BaseUnityPlugin {
        public LCVR Instance { get; private set; }

        private void Awake() {
            Instance = this;

            // Preloads some libraries
            typeof(OpenXRSettings).GetHashCode();
            typeof(XRGeneralSettings).GetHashCode();

            var xrAssetsBundle = AssetBundle.LoadFromStream(typeof(LCVR).Assembly.GetManifestResourceStream("LCVR.Resources.Assets.lcvr_assets"));

            foreach (var asset in xrAssetsBundle.LoadAllAssets<ScriptableObject>()) {
                Logger.LogInfo($"Loaded {asset.name} of type {asset.GetType().FullName}");
            }

            var stereoscopicImageShader = xrAssetsBundle.LoadAsset<Shader>("StereoscopicImage.shader");

            xrAssetsBundle.Unload(false);

            SceneManager.sceneLoaded += (scene, mode) => {
                if (scene.name.Equals("MainMenu")) StartXR();

                if (scene.name.Equals("SampleSceneRelay")) {
                    var rootTransform = scene.GetRootGameObjects()[0].transform;
                    var uiCamera = rootTransform.Find("/Systems/UI/UICamera").GetComponent<Camera>();

                    var stereoscopicImageRenderSystem = uiCamera.gameObject.AddComponent<StereoscopicImageRenderSystem>();

                    stereoscopicImageRenderSystem.mainCamera = rootTransform.Find("/PlayersContainer/Player/ScavengerModel/metarig/CameraContainer/MainCamera").GetComponent<Camera>();
                    stereoscopicImageRenderSystem.uiCamera = uiCamera;
                    stereoscopicImageRenderSystem.stereoscopicImageShader = stereoscopicImageShader;
                    stereoscopicImageRenderSystem.playerScreen = rootTransform.Find("/Systems/UI/Canvas/Panel/GameObject/PlayerScreen").GetComponent<RawImage>();
                }
            };
        }

        public void StartXR() {
            Logger.LogInfo("Attempting to start XR");

            XRGeneralSettings.Instance.Manager.InitializeLoaderSync();
            XRGeneralSettings.Instance.Manager.StartSubsystems();
        }
    }
}