namespace CubeBlast.Core
{
    public enum CubeColor
    {
        Red,
        Green,
        Blue,
        Yellow
    }

    public enum RocketOrientation
    {
        Horizontal,
        Vertical
    }

    /// <summary>Type of special item a cube group will create, used for group hints.</summary>
    public enum SpecialHint
    {
        None,
        Rocket,
        Tnt
    }

    /// <summary>How a combo of adjacent special items explodes.</summary>
    public enum ComboKind
    {
        None,          // single special item, no combo
        RocketRocket,  // only rockets: 2 perpendicular rockets in a + shape
        TntTnt,        // at least 2 TNTs: 7x7 area
        TntRocket      // 1 TNT and 1+ rockets: 3x3 array of rockets in a + shape
    }

    public enum LevelState
    {
        Playing,
        Won,
        Failed
    }

    /// <summary>A single primitive explosion, as understood by the view layer.</summary>
    public enum ExplosionKind
    {
        Rocket,   // a rocket line (2 parts moving in opposite directions)
        Tnt,      // 5x5 area
        TntTnt    // 7x7 area (TNT-TNT combo)
    }
}
