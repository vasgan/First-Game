using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FirstGame
{
    public sealed class BootstrapLoader : MonoBehaviour
    {
        private static readonly Color PanelColor = new(0.08f, 0.025f, 0.18f, 0.96f);

        private void Start()
        {
            LocalProgress.Load();
            CreateMenu();
        }

        private void CreateMenu()
        {
            if (FindFirstObjectByType<Canvas>()) return;
            if (!FindFirstObjectByType<EventSystem>()) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Canvas canvas = new GameObject("MainMenu", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);

            Image panel = CreateImage("Panel", canvas.transform, PanelColor, new Vector2(.08f, .20f), new Vector2(.92f, .80f));
            CreateText("Title", panel.transform, font, "GLOW PATH DEFENSE", 58, new Vector2(.05f, .76f), new Vector2(.95f, .94f));
            CreateText("Subtitle", panel.transform, font, "SELECT A LEVEL", 28, new Vector2(.05f, .64f), new Vector2(.95f, .74f));

            Button level = CreateButton("Level01", panel.transform, font,
                "LEVEL 1\n" + LocalProgress.GetLevelStatus(1), new Color(.38f, .66f, .93f),
                new Vector2(.08f, .38f), new Vector2(.92f, .59f));
            level.interactable = LocalProgress.IsUnlocked(1);
            level.onClick.AddListener(() => SceneManager.LoadScene("Level01"));
            CreateText("Progress", panel.transform, font,
                $"Local progress: {LocalProgress.CompletedLevel}/1 complete", 23,
                new Vector2(.08f, .20f), new Vector2(.92f, .31f));
        }

        private static Image CreateImage(string name, Transform parent, Color color, Vector2 min, Vector2 max)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            Image image = go.GetComponent<Image>(); image.color = color;
            return image;
        }

        private static Text CreateText(string name, Transform parent, Font font, string value, int size, Vector2 min, Vector2 max)
        {
            Text text = CreateImage(name, parent, Color.clear, min, max).gameObject.AddComponent<Text>();
            text.font = font; text.text = value; text.fontSize = size; text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white; text.resizeTextForBestFit = true; text.resizeTextMinSize = 18; text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, Font font, string label, Color color, Vector2 min, Vector2 max)
        {
            Image image = CreateImage(name, parent, color, min, max);
            Button button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            CreateText("Label", image.transform, font, label, 34, Vector2.zero, Vector2.one);
            return button;
        }
    }
}
