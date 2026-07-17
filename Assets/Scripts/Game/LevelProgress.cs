using UnityEngine;

namespace CubeBlast.Game
{
    /// <summary>
    /// Persists the player's current level locally (PlayerPrefs), as required:
    /// after restarting Unity the LevelButton shows the persisted level.
    /// </summary>
    public static class LevelProgress
    {
        public const int FirstLevel = 1;
        public const int MaxLevel = 10;
        public const string PlayerPrefsKey = "CubeBlast.LastPlayedLevel";

        /// <summary>Current (next to play) level. MaxLevel + 1 means everything is finished.</summary>
        public static int CurrentLevel
        {
            get { return Mathf.Clamp(PlayerPrefs.GetInt(PlayerPrefsKey, FirstLevel), FirstLevel, MaxLevel + 1); }
            set
            {
                PlayerPrefs.SetInt(PlayerPrefsKey, Mathf.Clamp(value, FirstLevel, MaxLevel + 1));
                PlayerPrefs.Save();
            }
        }

        public static bool AllLevelsFinished
        {
            get { return CurrentLevel > MaxLevel; }
        }

        public static void AdvanceAfterWin()
        {
            CurrentLevel = CurrentLevel + 1;
        }
    }
}
