using UnityEngine;

namespace FirstGame
{
    [CreateAssetMenu(menuName = "First Game/Level Definition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [Min(1)] public int levelIndex = 1;
        public string sceneName = "Level01";
        public string displayName = "LEVEL 1";
        public WaveConfig waveConfig;
        [Tooltip("Replaceable route surfaces and borders. The centre sprites may be transparent.")]
        public RouteTilePalette routePalette;
        public Vector2[] route = System.Array.Empty<Vector2>();
        public Vector2[] buildSpots = System.Array.Empty<Vector2>();
    }

    [System.Serializable]
    public sealed class RouteTilePalette
    {
        public Sprite roadCenterHorizontal;
        public Sprite roadCenterVertical;
        public Sprite roadBorderTopHorizontal;
        public Sprite roadBorderBottomHorizontal;
        public Sprite roadBorderLeftVertical;
        public Sprite roadBorderRightVertical;
        public Sprite roadBorderInnerCornerTopLeft;
        public Sprite roadBorderInnerCornerTopRight;
        public Sprite roadBorderInnerCornerBottomLeft;
        public Sprite roadBorderInnerCornerBottomRight;
        public Sprite roadBorderOuterCornerTopLeft;
        public Sprite roadBorderOuterCornerTopRight;
        public Sprite roadBorderOuterCornerBottomLeft;
        public Sprite roadBorderOuterCornerBottomRight;
    }
}
