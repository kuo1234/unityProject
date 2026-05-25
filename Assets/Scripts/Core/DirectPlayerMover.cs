using UnityEngine;
using UnityEngine.XR;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DirectPlayerMover : MonoBehaviour
{
    public float moveSpeed = 4.2f;
    public bool enableKeyboardFallback = true;
    public Vector2 areaCenter = new Vector2(0f, 1.1f);
    public Vector2 areaHalfSize = new Vector2(5.1f, 5.0f);

    private Transform rigRoot;

    private void Update()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        Vector2 input = ReadMoveInput();
        if (input.sqrMagnitude <= 0.01f)
        {
            ClampToPlayArea(camera.transform);
            return;
        }

        input = Vector2.ClampMagnitude(input, 1f);
        Transform root = GetRigRoot(camera.transform);
        Vector3 forward = camera.transform.forward;
        Vector3 right = camera.transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 move = ((forward * input.y) + (right * input.x)) * (moveSpeed * Time.deltaTime);
        root.position += move;
        ClampToPlayArea(camera.transform);
    }

    private void LateUpdate()
    {
        Camera camera = Camera.main;
        if (camera != null)
        {
            ClampToPlayArea(camera.transform);
        }
    }

    private void ClampToPlayArea(Transform cameraTransform)
    {
        Transform root = GetRigRoot(cameraTransform);
        Vector3 cameraPosition = cameraTransform.position;
        float minX = areaCenter.x - areaHalfSize.x;
        float maxX = areaCenter.x + areaHalfSize.x;
        float minZ = areaCenter.y - areaHalfSize.y;
        float maxZ = areaCenter.y + areaHalfSize.y;

        float clampedX = Mathf.Clamp(cameraPosition.x, minX, maxX);
        float clampedZ = Mathf.Clamp(cameraPosition.z, minZ, maxZ);
        Vector3 correction = new Vector3(clampedX - cameraPosition.x, 0f, clampedZ - cameraPosition.z);
        if (correction.sqrMagnitude > 0.0001f)
        {
            root.position += correction;
        }
    }

    private Transform GetRigRoot(Transform cameraTransform)
    {
        if (rigRoot != null)
        {
            return rigRoot;
        }

        Transform current = cameraTransform;
        Transform best = cameraTransform;
        while (current != null)
        {
            if (current.name.Contains("OVRCameraRig") || current.name.Contains("XR") || current.parent == null)
            {
                best = current;
            }

            current = current.parent;
        }

        rigRoot = best;
        return rigRoot;
    }

    private Vector2 ReadMoveInput()
    {
        Vector2 input = ReadXRThumbstick(XRNode.LeftHand);
        if (input.sqrMagnitude <= 0.01f)
        {
            input = ReadXRThumbstick(XRNode.RightHand);
        }

#if ENABLE_INPUT_SYSTEM
        if (input.sqrMagnitude <= 0.01f && Gamepad.current != null)
        {
            input = Gamepad.current.leftStick.ReadValue();
        }

        if (enableKeyboardFallback && Keyboard.current != null)
        {
            Vector2 keyboardInput = Vector2.zero;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                keyboardInput.x -= 1f;
            }

            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                keyboardInput.x += 1f;
            }

            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            {
                keyboardInput.y += 1f;
            }

            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            {
                keyboardInput.y -= 1f;
            }

            if (keyboardInput.sqrMagnitude > 0.01f)
            {
                input = keyboardInput;
            }
        }
#endif

        return input;
    }

    private static Vector2 ReadXRThumbstick(XRNode node)
    {
        UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        Vector2 axis = Vector2.zero;
        if (device.isValid && device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out axis))
        {
            return axis;
        }

        return Vector2.zero;
    }
}
