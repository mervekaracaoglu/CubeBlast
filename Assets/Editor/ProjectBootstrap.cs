using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CubeBlast.EditorTools
{
    /// <summary>
    /// Self-configuring project setup: makes sure both scenes are registered in
    /// the build settings (so SceneManager.LoadScene works out of the box) and
    /// that the player targets portrait orientation.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectBootstrap
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/MainScene.unity",
            "Assets/Scenes/LevelScene.unity"
        };

        static ProjectBootstrap()
        {
            EditorApplication.delayCall += Configure;
        }

        private static void Configure()
        {
            EnsureScenesInBuildSettings();
            EnsurePortraitOrientation();
        }

        private static void EnsureScenesInBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool changed = false;
            foreach (var path in ScenePaths)
            {
                if (!File.Exists(path))
                {
                    Debug.LogError("Expected scene is missing: " + path);
                    continue;
                }
                bool present = scenes.Exists(s => s.path == path);
                if (!present)
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                    changed = true;
                }
            }
            if (changed)
            {
                EditorBuildSettings.scenes = scenes.ToArray();
                Debug.Log("Registered game scenes in the build settings.");
            }
        }

        private static void EnsurePortraitOrientation()
        {
            if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait)
            {
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            }
        }
    }
}
