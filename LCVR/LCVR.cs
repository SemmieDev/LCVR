using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using BepInEx;
using UnityEngine;

namespace LCVR {
    [BepInPlugin("semmiedev.lcvr", "LCVR", "1.0.0")]
    public class LCVR : BaseUnityPlugin {
        private void Awake() {
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

            var xrSettingsBundle = AssetBundle.LoadFromFile(extractionPath + @"\Assets\xr_settings");

            var openXRLoader = xrSettingsBundle.LoadAsset("assets/xr/loaders/openxrloader.asset");
            var openXRPackageSettings = xrSettingsBundle.LoadAsset("assets/xr/settings/openxr package settings.asset");
            var xrGeneralSettingsPerBuildTarget = xrSettingsBundle.LoadAsset("assets/xr/xrgeneralsettingsperbuildtarget.asset");

            var xrGeneralSettingsType = xrGeneralSettingsAssembly.GetType("UnityEngine.XR.Management.XRGeneralSettings");

            var xrSDKInitializerMethod = xrGeneralSettingsType.GetMethod("AttemptInitializeXRSDKOnLoad", BindingFlags.NonPublic | BindingFlags.Static);
            var xrSDKStarterMethod = xrGeneralSettingsType.GetMethod("AttemptStartXRSDKOnBeforeSplashScreen", BindingFlags.NonPublic | BindingFlags.Static);

            if (xrSDKInitializerMethod == null) {
                throw new Exception("Can't find XR SDK initializer method");
            }

            if (xrSDKStarterMethod == null) {
                throw new Exception("Can't find XR SDK starter method");
            }

            Logger.LogInfo("Initializing XR SDK");
            xrSDKInitializerMethod.Invoke(null, null);

            Logger.LogInfo("Starting XR SDK");
            xrSDKStarterMethod.Invoke(null, null);
        }

        [DllImport("Kernel32.dll")]
        private static extern IntPtr LoadLibraryA(string lpLibFileName);
    }
}