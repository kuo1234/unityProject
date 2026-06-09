using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// 掛在 OVRCameraRig 上:用 WASD / 方向鍵 / VR 搖桿水平移動整個 rig。
// 不依賴 Meta XR Simulator 的內建移動,確保任何環境都能走動。
public class SimplePlayerLocomotion : MonoBehaviour
{
    [Tooltip("用來決定前進方向的相機(通常是 CenterEyeAnchor);留空則用 Camera.main")]
    public Transform head;
    public float moveSpeed = 3f;
    public float verticalSpeed = 2f;

    private void Update()
    {
        Transform reference = head != null ? head : (Camera.main != null ? Camera.main.transform : transform);

        Vector2 move = GetMoveInput();
        float vertical = GetVerticalInput();

        Vector3 forward = reference.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = reference.right;
        right.y = 0f;
        right.Normalize();

        Vector3 direction = (forward * move.y) + (right * move.x);
        if (direction.sqrMagnitude > 1f)
        {
            direction.Normalize();
        }

        transform.position += direction * moveSpeed * Time.deltaTime;
        transform.position += Vector3.up * (vertical * verticalSpeed * Time.deltaTime);
    }

    private Vector2 GetMoveInput()
    {
        // VR 搖桿優先
        Vector2 stick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.All);
        if (stick.sqrMagnitude > 0.02f)
        {
            return stick;
        }

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            float x = 0f;
            float y = 0f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y -= 1f;
            if (x != 0f || y != 0f)
            {
                return new Vector2(x, y);
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        {
            float x = 0f;
            float y = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y -= 1f;
            if (x != 0f || y != 0f)
            {
                return new Vector2(x, y);
            }
        }
#endif

        return Vector2.zero;
    }

    private float GetVerticalInput()
    {
        float value = 0f;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.eKey.isPressed) value += 1f;
            if (Keyboard.current.qKey.isPressed) value -= 1f;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKey(KeyCode.E)) value += 1f;
        if (Input.GetKey(KeyCode.Q)) value -= 1f;
#endif
        return value;
    }
}
