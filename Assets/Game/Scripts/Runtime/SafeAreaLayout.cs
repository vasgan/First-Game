using UnityEngine;

namespace FirstGame
{
    /// <summary>
    /// Keeps the HUD inside the device safe area while preserving the canvas scale.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaLayout : MonoBehaviour
    {
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private void Awake() => Apply();

        private void Update()
        {
            if (lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height ||
                lastSafeArea != Screen.safeArea)
                Apply();
        }

        public static void GetAnchors(Rect safeArea, Vector2 screenSize,
            out Vector2 anchorMin, out Vector2 anchorMax)
        {
            anchorMin = new Vector2(safeArea.xMin / screenSize.x, safeArea.yMin / screenSize.y);
            anchorMax = new Vector2(safeArea.xMax / screenSize.x, safeArea.yMax / screenSize.y);
        }

        private void Apply()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;

            Rect safeArea = Screen.safeArea;
            GetAnchors(safeArea, new Vector2(Screen.width, Screen.height), out Vector2 min, out Vector2 max);
            RectTransform rect = (RectTransform)transform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        }
    }
}
