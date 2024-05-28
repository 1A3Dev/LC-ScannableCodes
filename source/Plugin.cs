using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace ScannableCodes
{
    [BepInPlugin(modGUID, "ScannableCodes", modVersion)]
    internal class PluginLoader : BaseUnityPlugin
    {
        internal const string modGUID = "Dev1A3.ScannableCodes";

        private readonly Harmony harmony = new Harmony(modGUID);

        private const string modVersion = "1.0.0";

        private static bool initialized;

        internal static ManualLogSource logSource;

        public static PluginLoader Instance { get; private set; }

        private void Awake()
        {
            if (initialized)
            {
                return;
            }
            initialized = true;
            Instance = this;

            ScanConfig.InitConfig();

            Assembly patches = Assembly.GetExecutingAssembly();
            harmony.PatchAll(patches);

            logSource = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            logSource.LogInfo("Loaded ScannableCodes");
        }

        public void BindConfig<T>(ref ConfigEntry<T> config, string section, string key, T defaultValue, string description = "")
        {
            config = Config.Bind<T>(section, key, defaultValue, description);
        }
    }

    internal class ScanConfig
    {
        internal static ConfigEntry<bool> SpikeTrapScanEnabled;
        internal static void InitConfig()
        {
            PluginLoader.Instance.BindConfig(ref SpikeTrapScanEnabled, "Settings", "Spike Traps", true, "Should spike traps be able to be scanned?");
        }
    }

    [HarmonyPatch]
    internal static class ScanPatch
    {
        [HarmonyPatch(typeof(TerminalAccessibleObject), "SetCodeTo")]
        [HarmonyPostfix]
        private static void SetCodeTo(ref TerminalAccessibleObject __instance, int codeIndex)
        {
            if (__instance.objectCode != null)
            {
                Transform scanTransform = __instance.transform.parent ? __instance.transform.parent : __instance.transform;
                ScanNodeProperties scanNodeObj = scanTransform.GetComponentInChildren<ScanNodeProperties>();

                if (scanNodeObj != null)
                {
                    scanNodeObj.subText = $" {__instance.objectCode}";

                    if (__instance.isBigDoor)
                    {
                        scanNodeObj.maxRange = 8;
                    }
                    else if (__instance.GetComponent<Turret>() != null)
                    {
                        scanNodeObj.maxRange = 10;
                    }

                    PluginLoader.logSource.LogDebug($"Set code of {scanTransform.name}: {__instance.objectCode} (Range: {scanNodeObj.maxRange})");
                }
            }
        }

        [HarmonyPatch(typeof(SpikeRoofTrap), "Start")]
        [HarmonyPostfix]
        private static void SpikeRoofTrap_Start(SpikeRoofTrap __instance)
        {
            if (ScanConfig.SpikeTrapScanEnabled.Value)
            {
                GameObject scanCubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                scanCubeObj.tag = "DoNotSet";
                scanCubeObj.layer = 22;
                scanCubeObj.transform.parent = __instance.stickingPointsContainer;
                scanCubeObj.transform.localPosition = new Vector3(-0.8f, 0, -0.5f);
                Object.Destroy(scanCubeObj.GetComponent<MeshFilter>());
                Object.Destroy(scanCubeObj.GetComponent<MeshRenderer>());

                ScanNodeProperties scanNodeObj = scanCubeObj.AddComponent<ScanNodeProperties>();
                scanNodeObj.headerText = "Spike Trap";
                scanNodeObj.subText = "";
                scanNodeObj.requiresLineOfSight = true;
                scanNodeObj.maxRange = 8;
                scanNodeObj.minRange = 1;
                scanNodeObj.scrapValue = 0;
                scanNodeObj.creatureScanID = -1;
                scanNodeObj.nodeType = 1;

                PluginLoader.logSource.LogDebug($"Added scan node to {scanCubeObj.name} (Range: {scanNodeObj.maxRange})");
            }
        }
    }
}