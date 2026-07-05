using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VoidClash
{
    /// <summary>Code-built UGUI helpers (no prefab dependencies).</summary>
    public static class UIFactory
    {
        static Font _font;
        public static Font Font
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        public static readonly Color PanelColor = new Color(0.05f, 0.08f, 0.12f, 0.92f);
        public static readonly Color PanelLight = new Color(0.10f, 0.15f, 0.22f, 0.95f);
        public static readonly Color AccentColor = new Color(0.25f, 0.62f, 1f);
        public static readonly Color TextColor = new Color(0.85f, 0.92f, 1f);

        public static Canvas CreateCanvas(string name, int sortOrder = 0)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        public static RectTransform Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = TextureFactory.UISprite;
            img.type = Image.Type.Sliced;
            img.color = color;
            return (RectTransform)go.transform;
        }

        public static RectTransform Invisible(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return (RectTransform)go.transform;
        }

        public static Text Label(Transform parent, string name, string text, int size,
            TextAnchor anchor = TextAnchor.MiddleLeft, Color? color = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color ?? TextColor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button TextButton(Transform parent, string name, string label, int fontSize,
            System.Action onClick, Color? bg = null)
        {
            var rt = Panel(parent, name, bg ?? PanelLight);
            var btn = rt.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.4f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.9f, 1f);
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            var txt = Label(rt, "label", label, fontSize, TextAnchor.MiddleCenter);
            Stretch(txt.rectTransform);
            return btn;
        }

        public static void Stretch(RectTransform rt, float pad = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad);
            rt.offsetMax = new Vector2(-pad, -pad);
        }

        public static void SetRect(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        public static Slider CreateSlider(Transform parent, string name, float value, System.Action<float> onChange)
        {
            var root = Invisible(parent, name);
            var bg = Panel(root, "bg", new Color(0.03f, 0.05f, 0.08f, 1f));
            bg.anchorMin = new Vector2(0f, 0.5f);
            bg.anchorMax = new Vector2(1f, 0.5f);
            bg.offsetMin = new Vector2(0f, -6f);
            bg.offsetMax = new Vector2(0f, 6f);

            var fillArea = Invisible(root, "fillArea");
            fillArea.anchorMin = new Vector2(0f, 0.5f);
            fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.offsetMin = new Vector2(4f, -4f);
            fillArea.offsetMax = new Vector2(-4f, 4f);
            var fill = Panel(fillArea, "fill", AccentColor);
            Stretch(fill);

            var handleArea = Invisible(root, "handleArea");
            handleArea.anchorMin = new Vector2(0f, 0.5f);
            handleArea.anchorMax = new Vector2(1f, 0.5f);
            handleArea.offsetMin = new Vector2(8f, 0f);
            handleArea.offsetMax = new Vector2(-8f, 0f);
            var handle = Panel(handleArea, "handle", Color.white);
            handle.sizeDelta = new Vector2(18f, 26f);

            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
            if (onChange != null) slider.onValueChanged.AddListener(v => onChange(v));
            return slider;
        }
    }
}
