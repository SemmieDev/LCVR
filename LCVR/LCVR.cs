using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SubsystemsImplementation;

namespace LCVR {
    [BepInPlugin("semmiedev.lcvr", "LCVR", "1.0.0")]
    public class LCVR : BaseUnityPlugin {
        public LCVR Instance { get; private set; }

        private PropertyInfo xrGeneralSettingsInstanceProperty;
        private PropertyInfo xrGeneralSettingsManagerProperty;
        private MethodInfo xrManagerSettingsInitializeLoaderSyncMethod;
        private MethodInfo xrManagerSettingsStartSubsystemsMethod;

        private void Awake() {
            Instance = this;

            var assembly = typeof(LCVR).Assembly;

            Assembly xrGeneralSettingsAssembly = null;

            foreach (var resourceName in assembly.GetManifestResourceNames()) {
                var nameStart = resourceName.IndexOf('.', 15) + 1;
                var resourceType = resourceName.Substring(15, nameStart - 16);
                var resourceFileName = resourceName.Substring(nameStart);

                if (resourceType.Equals("Managed")) {
                    Logger.LogInfo($"Loading managed library {resourceFileName}");

                    using (var input = assembly.GetManifestResourceStream(resourceName)) {
                        var rawAssembly = new byte[input.Length];
                        input.Read(rawAssembly, 0, rawAssembly.Length);
                        var loadedAssembly = Assembly.Load(rawAssembly);

                        var assemblyName = loadedAssembly.GetName();

                        if (assemblyName.Name.Equals("Unity.XR.Management")) {
                            xrGeneralSettingsAssembly = loadedAssembly;
                        }

                        Logger.LogInfo($"Loaded managed library {assemblyName.Name}");
                    }
                }
            }

            if (xrGeneralSettingsAssembly == null) {
                throw new Exception("Didn't load assembly named Unity.XR.Management");
            }

            var xrGeneralSettingsType = xrGeneralSettingsAssembly.GetType("UnityEngine.XR.Management.XRGeneralSettings", true);
            xrGeneralSettingsInstanceProperty = xrGeneralSettingsType.GetProperty("Instance");
            xrGeneralSettingsManagerProperty = xrGeneralSettingsType.GetProperty("Manager");

            var xrManagerSettingsType = xrGeneralSettingsAssembly.GetType("UnityEngine.XR.Management.XRManagerSettings", true);
            xrManagerSettingsInitializeLoaderSyncMethod = xrManagerSettingsType.GetMethod("InitializeLoaderSync");
            xrManagerSettingsStartSubsystemsMethod = xrManagerSettingsType.GetMethod("StartSubsystems");

            if (xrGeneralSettingsInstanceProperty == null) {
                throw new Exception("UnityEngine.XR.Management.XRGeneralSettings Instance");
            }

            if (xrGeneralSettingsManagerProperty == null) {
                throw new Exception("UnityEngine.XR.Management.XRGeneralSettings Manager");
            }

            if (xrManagerSettingsInitializeLoaderSyncMethod == null) {
                throw new Exception("UnityEngine.XR.Management.XRManagerSettings InitializeLoaderSync");
            }

            if (xrManagerSettingsStartSubsystemsMethod == null) {
                throw new Exception("UnityEngine.XR.Management.XRManagerSettings StartSubsystems");
            }

            var xrSettingsBundle = AssetBundle.LoadFromStream(assembly.GetManifestResourceStream("LCVR.Resources.Assets.xr_settings"));

            foreach (var asset in xrSettingsBundle.LoadAllAssets()) {
                Logger.LogInfo($"Loaded {asset.name} of type {asset.GetType().FullName}");
            }

            SceneManager.sceneLoaded += (scene, mode) => {
                if (!scene.name.Equals("MainMenu")) return;

                StartXR();
            };
        }

        public void StartXR() {
            Logger.LogInfo("Attempting to start XR");

            var xrGeneralSettingsInstance = xrGeneralSettingsInstanceProperty.GetValue(null);
            var xrManagerSettingsInstance = xrGeneralSettingsManagerProperty.GetValue(xrGeneralSettingsInstance);

            Logger.LogInfo("Initializing XR");
            xrManagerSettingsInitializeLoaderSyncMethod.Invoke(xrManagerSettingsInstance, null);

            Logger.LogInfo("Starting XR");
            xrManagerSettingsStartSubsystemsMethod.Invoke(xrManagerSettingsInstance, null);
        }
    }
}