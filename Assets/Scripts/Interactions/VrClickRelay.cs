using UnityEngine;

// 掛在可被 VR 指標點擊的物件上(需有 Collider)。
// VrUiPointer 命中並點擊時呼叫 OnVrPointerClick();滑入/滑出時呼叫 SetHover()。
public class VrClickRelay : MonoBehaviour
{
    public System.Action onClick;

    public Renderer highlightRenderer;
    public Color normalColor = new Color(0.20f, 0.75f, 0.35f);
    public Color hoverColor = new Color(0.35f, 1f, 0.55f);

    private void Awake()
    {
        ApplyColor(false);
    }

    public void SetHover(bool hovering)
    {
        ApplyColor(hovering);
    }

    private void ApplyColor(bool hovering)
    {
        if (highlightRenderer == null)
        {
            return;
        }

        Color c = hovering ? hoverColor : normalColor;
        Material m = highlightRenderer.material;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        m.color = c;
    }

    public void OnVrPointerClick()
    {
        onClick?.Invoke();
    }
}
