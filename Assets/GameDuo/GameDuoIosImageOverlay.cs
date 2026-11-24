#if UNITY_IOS && !UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;

namespace CoopBeam {
internal sealed class GameDuoIosImageOverlay {
    readonly Canvas canvas;
    readonly RawImage image;
    readonly AspectRatioFitter fit;
    readonly RawImage fade;

    public GameDuoIosImageOverlay(Transform owner, bool cinematic) {
        var root = new GameObject("iOS image overlay", typeof(RectTransform), typeof(Canvas));
        root.transform.SetParent(owner, false);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        if (cinematic) AddImage(root.transform, Color.black);
        image = AddImage(root.transform, Color.white);
        fit = image.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = cinematic ? AspectRatioFitter.AspectMode.FitInParent
                                   : AspectRatioFitter.AspectMode.EnvelopeParent;
        if (cinematic) fade = AddImage(root.transform, Color.clear);
        root.SetActive(false);
    }

    static RawImage AddImage(Transform parent, Color color) {
        var go = new GameObject("Overlay image", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
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

    public void Refresh(Texture texture, bool visible, float opacity = 0) {
        bool active = visible && texture;
        if (active && image.texture != texture) {
            image.texture = texture;
            fit.aspectRatio = (float)texture.width / texture.height;
        }
        if (active && fade) fade.color = new Color(0, 0, 0, Mathf.Clamp01(opacity));
        if (canvas.gameObject.activeSelf != active) canvas.gameObject.SetActive(active);
    }

}
}
#endif
