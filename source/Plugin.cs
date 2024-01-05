using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace ScanCodes
{
    [BepInPlugin(modGUID, "ScanCodes", modVersion)]
    internal class PluginLoader : BaseUnityPlugin
    {
        internal const string modGUID = "Dev1A3.ScanCodes";

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

            Assembly patches = Assembly.GetExecutingAssembly();
            harmony.PatchAll(patches);

            logSource = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            logSource.LogInfo("Loaded ScanCodes");
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
    }
}