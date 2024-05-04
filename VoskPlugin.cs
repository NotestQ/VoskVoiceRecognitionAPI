using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Vosk;

namespace VoskVoiceRecognitionAPI
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class VoskPlugin : BaseUnityPlugin
    {
        public static VoskPlugin Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger { get; private set; } = null!;
        internal static Harmony? Harmony { get; set; }
        internal static ConfigEntry<string>? modelName;
        const uint LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000;

        internal ConfigEntry<string>? modelPath;
        internal static Model voskModel;
        public static bool CanStart;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool SetDefaultDllDirectories(uint DirectoryFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int AddDllDirectory(string NewDirectory);

        private void Awake()
        {
            Logger = base.Logger;
            Instance = this;

            SetDefaultDllDirectories(LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);
            AddDllDirectory(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "VoskResources"));

            modelPath = Config.Bind(
                "General",
                "Path",
                "",
                "The path to where you stored your Vosk speech recognition model. \n Make sure to not include spaces or special characters. \n Example path: \"C:\\vosk-model-small-en-us-0.15\""
            );
            
            Patch();

            ModalOption[] buttons = [
                new("Ok."),
            ];


            if (modelPath.Value == "")
            {
                Logger.LogError("Model path was not defined. Speech handler will not start");
                CanStart = false;
                return;
            } 
            
            if (InvalidPath(modelPath.Value))
            {
                Logger.LogError($"Model path {modelPath.Value} contains spaces or non-ASCII characters. Speech handler will not start");
                CanStart = false;
                return;
            }

            CanStart = true;
            voskModel = new Model(modelPath.Value);

            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }

        private static bool InvalidPath(string path)
        {
            var regex = new Regex("[^\\x00-\\x7F]|\\s");
            return regex.IsMatch(path);
        }

        internal static void Patch()
        {
            Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

            Logger.LogDebug("Patching...");

            Harmony.PatchAll();

            Logger.LogDebug("Finished patching!");
        }

        internal static void Unpatch()
        {
            Logger.LogDebug("Unpatching...");

            Harmony?.UnpatchSelf();

            Logger.LogDebug("Finished unpatching!");
        }
    }
}
