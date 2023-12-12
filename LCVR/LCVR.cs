using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;

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

            var extractionPath = Paths.CachePath + @"\LCVR";

            if (!Directory.Exists(extractionPath)) {
                Directory.CreateDirectory(extractionPath);
            }

            var assembly = typeof(LCVR).Assembly;

            foreach (var resourceName in assembly.GetManifestResourceNames()) {
                var nameStart = resourceName.IndexOf('.', 15) + 1;
                var resourceType = resourceName.Substring(15, nameStart - 16);
                var subExtractionPath = extractionPath + @"\" + resourceType;
                var resourceFileName = resourceName.Substring(nameStart);
                var fullExtractionPath = subExtractionPath + @"\" + resourceFileName;

                if (!Directory.Exists(subExtractionPath)) {
                    Directory.CreateDirectory(subExtractionPath);
                }

                if (!File.Exists(fullExtractionPath)) {
                    Logger.LogInfo($"Extracting resource {resourceFileName} of type {resourceType} to {fullExtractionPath}");

                    using (var input = assembly.GetManifestResourceStream(resourceName)) {
                        using (var output = File.OpenWrite(fullExtractionPath)) {
                            input?.CopyTo(output);
                        }
                    }
                }
            }

            foreach (var file in Directory.EnumerateFiles(extractionPath + @"\Plugins")) {
                Logger.LogInfo($"Loading plugin {Path.GetFileName(file)}");

                var result = LoadLibraryA(file);

                if (result == IntPtr.Zero) {
                    Logger.LogError($"Failed to load plugin {Path.GetFileName(file)}");
                }
            }

            Assembly xrGeneralSettingsAssembly = null;

            foreach (var file in Directory.EnumerateFiles(extractionPath + @"\Managed")) {
                var fileName = Path.GetFileName(file);
                Logger.LogInfo($"Loading managed library {fileName}");

                var loadedAssembly = Assembly.LoadFile(file);
                var assemblyName = loadedAssembly.GetName();

                if (assemblyName.Name.Equals("Unity.XR.Management")) {
                    xrGeneralSettingsAssembly = loadedAssembly;
                }

                Logger.LogInfo($"Loaded {assemblyName.Name} from {fileName}");
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

            var xrSettingsBundle = AssetBundle.LoadFromFile(extractionPath + @"\Assets\xr_settings");

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

        [DllImport("Kernel32.dll")]
        private static extern IntPtr LoadLibraryA(string lpLibFileName);
    }
}