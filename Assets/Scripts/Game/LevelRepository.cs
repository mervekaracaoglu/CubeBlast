using System;
using CubeBlast.Core;
using UnityEngine;

namespace CubeBlast.Game
{
    /// <summary>Loads level JSON files from Resources and converts them into core definitions.</summary>
    public static class LevelRepository
    {
        [Serializable]
        private class LevelJson
        {
            public int level_number;
            public int grid_width;
            public int grid_height;
            public int move_count;
            public string[] grid;
        }

        public static LevelDefinition Load(int levelNumber)
        {
            string name = "Levels/level_" + levelNumber.ToString("00");
            var asset = Resources.Load<TextAsset>(name);
            if (asset == null)
            {
                Debug.LogError("Level file not found: Resources/" + name);
                return null;
            }
            var json = JsonUtility.FromJson<LevelJson>(asset.text);
            return new LevelDefinition(json.level_number, json.grid_width, json.grid_height, json.move_count, json.grid);
        }
    }
}
