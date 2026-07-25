using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityModManagerNet;
using static UnityModManagerNet.UnityModManager;

namespace dvSlugSpawnsMod
{
    public static class Main
    {
        public static Settings Settings { get; private set; } = null!;

        public static UnityModManager.ModEntry ModEntry { get; private set; } = null!;

        private static bool Load(UnityModManager.ModEntry modEntry)
        {
            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            modEntry.OnGUI = OnDrawGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            modEntry.OnToggle = OnToggle;
            ModEntry = modEntry;
            return true;
        }

        static void OnDrawGUI(UnityModManager.ModEntry entry)
        {
            Settings.Draw(entry);
            GUILayout.Space(15);
            TrackConfig.DrawGUI();
        }

        static void OnSaveGUI(UnityModManager.ModEntry entry)
        {
            Settings.Save(entry);
            TrackConfig.Save();
            TrackConfig.Load();
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool active)
        {
            Harmony harmony = new(modEntry.Info.Id);
            if (active)
            {
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                if (!TrackConfig.Load()) modEntry.Logger.Error("Failed to load settings, defaults will be loaded");
                WorldStreamingInit.LoadingFinished += OnLoadingFinished;
                UnloadWatcher.UnloadRequested += TrackConfig.Save;
            }
            else
            {
                UnloadWatcher.UnloadRequested -= TrackConfig.Save;
                WorldStreamingInit.LoadingFinished -= OnLoadingFinished;
                harmony.UnpatchAll(modEntry.Info.Id);
            }
            return true;
        }

        private static void OnLoadingFinished()
        {
            if (!TrackConfig.Load()) ModEntry.Logger.Error("Failed to load settings, defaults will be loaded");
        }
    }
}