using CubeBlast.Game;
using UnityEditor;
using UnityEngine;

namespace CubeBlast.EditorTools
{
    /// <summary>
    /// Editor menu item: lets developers set the
    /// persisted last played level number without touching PlayerPrefs by hand.
    /// </summary>
    public sealed class LevelProgressWindow : EditorWindow
    {
        private int _level;
        private bool _initialised;

        [MenuItem("Dream Games/Set Last Played Level...")]
        public static void Open()
        {
            var window = GetWindow<LevelProgressWindow>(true, "Level Progress", true);
            window.minSize = new Vector2(320f, 150f);
            window.Show();
        }

        [MenuItem("Dream Games/Reset Progress (Level 1)")]
        public static void ResetProgress()
        {
            LevelProgress.CurrentLevel = LevelProgress.FirstLevel;
            Debug.Log("Level progress reset. Current level: " + LevelProgress.CurrentLevel);
        }

        [MenuItem("Dream Games/Mark All Levels Finished")]
        public static void MarkAllFinished()
        {
            LevelProgress.CurrentLevel = LevelProgress.MaxLevel + 1;
            Debug.Log("All levels marked as finished.");
        }

        private void OnGUI()
        {
            if (!_initialised)
            {
                _level = LevelProgress.CurrentLevel;
                _initialised = true;
            }

            EditorGUILayout.Space();
            string current = LevelProgress.AllLevelsFinished
                ? "All levels finished"
                : "Level " + LevelProgress.CurrentLevel;
            EditorGUILayout.LabelField("Current persisted level:", current);
            EditorGUILayout.Space();

            _level = EditorGUILayout.IntSlider("Set level", _level, LevelProgress.FirstLevel, LevelProgress.MaxLevel + 1);
            if (_level > LevelProgress.MaxLevel)
            {
                EditorGUILayout.HelpBox(
                    "Level " + (LevelProgress.MaxLevel + 1) + " means all levels are finished; the LevelButton will show 'Finished'.",
                    MessageType.Info);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Apply", GUILayout.Height(28f)))
            {
                LevelProgress.CurrentLevel = _level;
                Debug.Log("Last played level set to " + _level + ".");
            }
        }
    }
}
