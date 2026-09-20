using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Arena.UI
{
    /// <summary>
    /// Mobile UI scaling fix — separate file so it doesn't conflict with Theme.cs.
    /// Self-installs on game start and only activates on touch devices or
    /// phone-sized screens.
    ///
    /// Three fixes:
    /// 1. Smaller CanvasScaler reference resolution → everything ~33% bigger.
    /// 2. Font size multiplier for TextMeshPro labels.
    /// 3. Minimum button height so tap targets are big enough.
    /// </summary>
    public class MobileUIScale : MonoBehaviour
    {
        [Header("Reference resolution for mobile (smaller = bigger UI)")]
        public int MobileReferenceWidth = 1024;
        public int MobileReferenceHeight = 640;

        [Header("Font multiplier (1.0 = no change, 1.15 = +15%)")]
        public float FontScale = 1.0f;

        [Header("Minimum touch-target height in logical pixels")]
        public float MinButtonHeight = 60f;

        private readonly Dictionary<TMP_Text, float> originalFontSizes = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInstall()
        {
            string url = Application.absoluteURL;
            bool isMobile = url != null && url.Contains("mobile=1");

            if (!isMobile) return;

            var go = new GameObject("MobileUIScale");
            DontDestroyOnLoad(go);
            go.AddComponent<MobileUIScale>();
        }

        private void Start()
        {
            // Run once at startup, then re-apply once every second to catch
            // newly created canvases (mode select → skill test → dialogue → feedback).
            InvokeRepeating(nameof(Apply), 0f, 1f);
        }

        private void Apply()
        {
            // Purge destroyed entries so the dictionary doesn't grow forever.
            var toRemove = new List<TMP_Text>();
            foreach (var kvp in originalFontSizes)
                if (kvp.Key == null) toRemove.Add(kvp.Key);
            foreach (var k in toRemove) originalFontSizes.Remove(k);

            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas.name.Contains("Admin")) continue;
                // 1. Override CanvasScaler reference resolution
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null && scaler.referenceResolution.x != MobileReferenceWidth)
                {
                    scaler.referenceResolution = new Vector2(MobileReferenceWidth, MobileReferenceHeight);
                    scaler.matchWidthOrHeight = 0.5f;
                }

                // 2. Bump TMP font sizes (cache originals so we don't double-apply)
                var texts = canvas.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var text in texts)
                {
                    if (!originalFontSizes.TryGetValue(text, out float orig))
                    {
                        orig = text.fontSize;
                        originalFontSizes[text] = orig;
                    }
                    float target = orig * FontScale;
                    if (!Mathf.Approximately(text.fontSize, target))
                        text.fontSize = target;
                }

                // 3. Enforce minimum button height
                var buttons = canvas.GetComponentsInChildren<Button>(true);
                foreach (var button in buttons)
                {
                    var le = button.GetComponent<LayoutElement>();
                    if (le == null) le = button.gameObject.AddComponent<LayoutElement>();
                    if (le.minHeight < MinButtonHeight) le.minHeight = MinButtonHeight;
                }
            }
        }
    }
}