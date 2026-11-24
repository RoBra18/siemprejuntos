#if UNITY_IOS && !UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;

namespace CoopBeam {
internal sealed class GameDuoIosColorComparison {
    readonly Canvas canvas;
    readonly RawImage image;
    readonly AspectRatioFitter fit;
    public bool UseCanvas { get; private set; }

    public GameDuoIosColorComparison(Transform owner, bool cinematic) {
        var root = new GameObject("iOS color comparison", typeof(RectTransform), typeof(Canvas));
        root.transform.SetParent(owner, false);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        if (cinematic) AddImage(root.transform, Color.black);
        image = AddImage(root.transform, Color.white);
        fit = image.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = cinematic ? AspectRatioFitter.AspectMode.FitInParent
                                   : AspectRatioFitter.AspectMode.EnvelopeParent;
        root.SetActive(false);
    }

    static RawImage AddImage(Transform parent, Color color) {
        var go = new GameObject("Comparison image", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        go.transform.SetParent(parent, false);
        var result = go.GetComponent<RawImage>();
        result.color = color;
        result.raycastTarget = false;
        result.rectTransform.anchorMin = Vector2.zero;
        result.rectTransform.anchorMax = Vector2.one;
        result.rectTransform.offsetMin = Vector2.zero;
        result.rectTransform.offsetMax = Vector2.zero;
        return result;
    }

    public void Refresh(Texture texture, bool visible) {
        bool active = UseCanvas && visible && texture;
        if (active && image.texture != texture) {
            image.texture = texture;
            fit.aspectRatio = (float)texture.width / texture.height;
        }
        if (canvas.gameObject.activeSelf != active) canvas.gameObject.SetActive(active);
    }

    public void Toggle(Rect rect) {
        if (CoopPromptUI.Button(rect, UseCanvas ? "Color: Canvas" : "Color: actual", CoopPromptUI.Mint))
            UseCanvas = !UseCanvas;
    }
}
}
#endif
