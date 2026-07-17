using System.Collections.Generic;
using CubeBlast.Core;
using UnityEngine;

namespace CubeBlast.Game
{
    /// <summary>
    /// Central, cached access to every sprite in Resources. All view classes go
    /// through this class so asset paths live in exactly one place.
    /// </summary>
    public static class GameAssets
    {
        private const string Root = "Sprites/";
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string path)
        {
            Sprite sprite;
            if (!Cache.TryGetValue(path, out sprite))
            {
                sprite = Resources.Load<Sprite>(Root + path);
                if (sprite == null)
                {
                    Debug.LogError("Missing sprite at Resources/" + Root + path);
                }
                Cache[path] = sprite;
            }
            return sprite;
        }

        private static string ColorName(CubeColor color)
        {
            switch (color)
            {
                case CubeColor.Red: return "red";
                case CubeColor.Green: return "green";
                case CubeColor.Blue: return "blue";
                default: return "yellow";
            }
        }

        // ------------------------------------------------------------ cubes
        public static Sprite Cube(CubeColor color) { return Get("Cubes/DefaultState/" + ColorName(color)); }
        public static Sprite CubeRocketHint(CubeColor color) { return Get("Cubes/RocketState/" + ColorName(color) + "_rocket"); }
        public static Sprite CubeTntHint(CubeColor color) { return Get("Cubes/TntState/" + ColorName(color) + "_tnt"); }
        public static Sprite CubeParticle(CubeColor color) { return Get("Cubes/Particles/particle_" + ColorName(color)); }

        // ---------------------------------------------------- special items
        public static Sprite Rocket(RocketOrientation orientation)
        {
            return Get("SpecialItems/Rocket/" + (orientation == RocketOrientation.Horizontal ? "horizontal_rocket" : "vertical_rocket"));
        }

        public static Sprite RocketPart(RocketOrientation orientation, bool positiveDirection)
        {
            if (orientation == RocketOrientation.Horizontal)
            {
                return Get("SpecialItems/Rocket/horizontal_rocket_part_" + (positiveDirection ? "right" : "left"));
            }
            return Get("SpecialItems/Rocket/vertical_rocket_part_" + (positiveDirection ? "top" : "bottom"));
        }

        public static Sprite RocketSmoke { get { return Get("SpecialItems/Rocket/Particles/particle_smoke"); } }
        public static Sprite RocketStar { get { return Get("SpecialItems/Rocket/Particles/particle_star"); } }
        public static Sprite Tnt { get { return Get("SpecialItems/TNT/TNT"); } }
        public static Sprite TntParticle(int index) { return Get("SpecialItems/TNT/Particles/particle_tnt_0" + (index + 1)); }

        // ---------------------------------------------------------- obstacles
        public static Sprite Vase(bool damaged) { return Get("Obstacles/Vase/" + (damaged ? "vase_02" : "vase_01")); }
        public static Sprite VaseParticle(int index) { return Get("Obstacles/Vase/Particles/particle_vase_0" + (index + 1)); }
        public static Sprite Stone { get { return Get("Obstacles/Stone/stone"); } }
        public static Sprite StoneParticle(int index) { return Get("Obstacles/Stone/Particles/particle_stone_0" + (index + 1)); }
        public static Sprite Chalice { get { return Get("Obstacles/ChaliceBox/Chalice"); } }
        public static Sprite ChaliceBoxBg { get { return Get("Obstacles/ChaliceBox/ChaliceBoxBg"); } }
        public static Sprite ChaliceBoxDoors { get { return Get("Obstacles/ChaliceBox/ChaliceBoxDoors"); } }

        public static Sprite ChaliceBoxParticle(int index)
        {
            return Get("Obstacles/ChaliceBox/Particles/BaseParticles/chalice_box_particle_0" + (index + 1));
        }

        public static Sprite ChaliceBoxDoorParticle(int index)
        {
            string number = (index + 1) < 10 ? "0" + (index + 1) : "" + (index + 1);
            return Get("Obstacles/ChaliceBox/Particles/DoorParticles/chalice_box_door_particle_" + number);
        }

        // ------------------------------------------------------------- UI/FX
        public static Sprite MenuBackground { get { return Get("Menu/background"); } }
        public static Sprite MenuButton { get { return Get("UI/Menu/button"); } }
        public static Sprite MenuButtonFrame { get { return Get("UI/Menu/button_frame"); } }
        public static Sprite GridBackground { get { return Get("UI/Gameplay/grid_background"); } }
        public static Sprite TopPanel { get { return Get("UI/Gameplay/Top/top_ui"); } }
        public static Sprite GoalCheck { get { return Get("UI/Gameplay/Top/goal_check"); } }
        public static Sprite PopupBase { get { return Get("UI/Gameplay/Popup/popup_base"); } }
        public static Sprite PopupRibbon { get { return Get("UI/Gameplay/Popup/popup_ribbon"); } }
        public static Sprite CloseButton { get { return Get("UI/Gameplay/Popup/close_button"); } }
        public static Sprite CelebrationStar { get { return Get("UI/Gameplay/Celebration/star"); } }
        public static Sprite CelebrationStarParticle { get { return Get("UI/Gameplay/Celebration/Particles/additive_particle_star"); } }

        public static Font DefaultFont
        {
            get { return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
        }

        /// <summary>Sprite used for the small goal icons of each obstacle kind.</summary>
        public static Sprite GoalIcon(string goalKey)
        {
            switch (goalKey)
            {
                case VaseItem.Key: return Vase(false);
                case StoneItem.Key: return Stone;
                case ChaliceBoxItem.Key: return Chalice;
                default: return null;
            }
        }
    }
}
