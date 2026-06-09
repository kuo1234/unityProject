using UnityEngine;
using UnityEngine.XR;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// 從控制器(或相機)射出雷射,瞄準帶有 VrClickRelay 的物件;扣板機/滑鼠左鍵點擊。
// 不依賴 XR Interaction Toolkit / Meta Interaction SDK,完全自含。
public class VrUiPointer : MonoBehaviour
{
    [Tooltip("雷射發射來源(通常是控制器 anchor);留空則用 Camera.main")]
    public Transform rayOrigin;
    public float maxDistance = 25f;
    public Color rayColor = new Color(0.2f, 0.9f, 1f);

    private LineRenderer line;
    private Transform reticle;
    private VrClickRelay hovered;
    private bool previousPressed;

    private void Start()
    {
        GameObject lineObject = new GameObject("VrPointerLine");
        lineObject.transform.SetParent(transform, false);
        line = lineObject.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.widthMultiplier = 0.012f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = rayColor;
        line.endColor = rayColor;
        line.useWorldSpace = true;

        GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dot.name = "VrPointerReticle";
        Destroy(dot.GetComponent<Collider>());
        dot.transform.localScale = Vector3.one * 0.06f;
        reticle = dot.transform;
    }

    private void Update()
    {
        Transform origin = rayOrigin != null
            ? rayOrigin
            : (Camera.main != null ? Camera.main.transform : transform);

        Vector3 start = origin.position;
        Vector3 dir = origin.forward;
        Vector3 end = start + dir * maxDistance;

        VrClickRelay hit = null;
        if (Physics.Raycast(start, dir, out RaycastHit hitInfo, maxDistance))
        {
            end = hitInfo.point;
            hit = hitInfo.collider.GetComponentInParent<VrClickRelay>();
        }

        if (hovered != hit)
        {
            if (hovered != null) hovered.SetHover(false);
            hovered = hit;
            if (hovered != null) hovered.SetHover(true);
        }

        if (line != null)
        {
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            Color c = hovered != null ? Color.green : rayColor;
            line.startColor = c;
            line.endColor = c;
        }

        if (reticle != null)
        {
            reticle.gameObject.SetActive(hit != null);
            reticle.position = end;
        }

        bool pressed = IsClickPressed();
        if (pressed && !previousPressed && hovered != null)
        {
            hovered.OnVrPointerClick();
        }
        previousPressed = pressed;
    }

    private bool IsClickPressed()
    {
        if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.All) ||
            OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.All))
        {
            return true;
        }

        if (IsXrTriggerPressed())
        {
            return true;
        }

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            return true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButton(0))
        {
            return true;
        }
#endif
        return false;
    }

    private bool IsXrTriggerPressed()
    {
        foreach (XRNode node in new[] { XRNode.RightHand, XRNode.LeftHand })
        {
            UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(node);
            if (!device.isValid)
            {
                continue;
            }

            if ((device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool trigger) && trigger) ||
                (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool primary) && primary))
            {
                return true;
            }
        }

        return false;
    }
}
