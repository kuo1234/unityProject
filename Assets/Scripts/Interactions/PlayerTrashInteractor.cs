using UnityEngine;
using UnityEngine.XR;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerTrashInteractor : MonoBehaviour
{
    public Camera playerCamera;
    public Transform holdPoint;
    public float pickupRange = 12f;
    public float holdForce = 18f;
    public float throwForce = 5f;
    public float pointerRadius = 0.35f;
    public float aimAssistViewportRadius = 0.18f;
    public bool autoPickupOnHover = false;
    public float hoverPickupSeconds = 1.2f;

    private Rigidbody heldRigidbody;
    private LineRenderer pointerLine;
    private Transform handVisual;
    private Transform targetMarker;
    private TrashItem targetedTrashItem;
    private TrashItem previousTargetedTrashItem;
    private Vector3 targetPoint;
    private float hoverTimer;
    private bool previousPickupButtonPressed;
    private bool previousThrowButtonPressed;
    private string lastInputSource = "none";
    private int detectedInputSystemControllers;

    private void Reset()
    {
        playerCamera = Camera.main;
    }

    private void Update()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        EnsureVisuals();
        UpdateTargeting();
        if (autoPickupOnHover)
        {
            UpdateHoverPickup();
        }

        if (WasPickupPressed())
        {
            if (heldRigidbody == null)
            {
                TryPickup();
            }
            else
            {
                DropHeldItem(false);
            }
        }

        if (WasThrowPressed() && heldRigidbody != null)
        {
            DropHeldItem(true);
        }

        UpdateVisuals();
    }

    private void FixedUpdate()
    {
        if (heldRigidbody == null || holdPoint == null)
        {
            return;
        }

        Vector3 toHoldPoint = holdPoint.position - heldRigidbody.position;
        heldRigidbody.linearVelocity = toHoldPoint * holdForce;
    }

    private void TryPickup()
    {
        if (targetedTrashItem == null)
        {
            return;
        }

        heldRigidbody = targetedTrashItem.GetComponent<Rigidbody>();
        if (heldRigidbody == null)
        {
            return;
        }

        heldRigidbody.useGravity = false;
        heldRigidbody.angularVelocity = Vector3.zero;
    }

    private void DropHeldItem(bool shouldThrow)
    {
        Rigidbody droppedRigidbody = heldRigidbody;
        heldRigidbody = null;

        droppedRigidbody.useGravity = true;

        if (shouldThrow && playerCamera != null)
        {
            droppedRigidbody.AddForce(playerCamera.transform.forward * throwForce, ForceMode.Impulse);
        }
    }

    private void UpdateTargeting()
    {
        targetedTrashItem = null;
        targetPoint = GetPointerOrigin() + GetPointerDirection() * pickupRange;

        if (playerCamera == null)
        {
            return;
        }

        Ray ray = new Ray(GetPointerOrigin(), GetPointerDirection());
        if (Physics.SphereCast(ray, pointerRadius, out RaycastHit hit, pickupRange))
        {
            TrashItem trashItem = hit.collider.GetComponentInParent<TrashItem>();
            if (trashItem != null)
            {
                targetedTrashItem = trashItem;
                targetPoint = hit.point;
                return;
            }
        }

        TrashItem bestTrashItem = null;
        float bestScore = float.MaxValue;

        foreach (TrashItem trashItem in FindObjectsByType<TrashItem>(FindObjectsInactive.Exclude))
        {
            if (heldRigidbody != null && trashItem.gameObject == heldRigidbody.gameObject)
            {
                continue;
            }

            Vector3 viewportPosition = playerCamera.WorldToViewportPoint(trashItem.transform.position);
            if (viewportPosition.z <= 0f || viewportPosition.z > pickupRange)
            {
                continue;
            }

            Vector2 viewportOffset = new Vector2(viewportPosition.x - 0.5f, viewportPosition.y - 0.5f);
            if (viewportOffset.magnitude > aimAssistViewportRadius)
            {
                continue;
            }

            float score = viewportOffset.sqrMagnitude + (viewportPosition.z * 0.002f);
            if (score < bestScore)
            {
                bestScore = score;
                bestTrashItem = trashItem;
            }
        }

        if (bestTrashItem != null)
        {
            targetedTrashItem = bestTrashItem;
            targetPoint = bestTrashItem.transform.position;
        }
    }

    private void UpdateHoverPickup()
    {
        if (heldRigidbody != null)
        {
            hoverTimer = 0f;
            previousTargetedTrashItem = null;
            return;
        }

        if (targetedTrashItem == null)
        {
            hoverTimer = 0f;
            previousTargetedTrashItem = null;
            return;
        }

        if (targetedTrashItem != previousTargetedTrashItem)
        {
            hoverTimer = 0f;
            previousTargetedTrashItem = targetedTrashItem;
        }

        hoverTimer += Time.deltaTime;
        if (hoverTimer >= hoverPickupSeconds)
        {
            TryPickup();
            hoverTimer = 0f;
        }
    }

    private void EnsureVisuals()
    {
        if (handVisual == null)
        {
            GameObject hand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hand.name = "SortingHand";
            hand.transform.localScale = Vector3.one * 0.18f;
            Destroy(hand.GetComponent<Collider>());
            Renderer renderer = hand.GetComponent<Renderer>();
            renderer.material.color = new Color(0.2f, 0.9f, 1f);
            handVisual = hand.transform;
        }

        if (targetMarker == null)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "SortingTargetMarker";
            marker.transform.localScale = Vector3.one * 0.3f;
            Destroy(marker.GetComponent<Collider>());
            Renderer renderer = marker.GetComponent<Renderer>();
            renderer.material.color = Color.white;
            targetMarker = marker.transform;
        }

        if (pointerLine == null)
        {
            GameObject pointer = new GameObject("SortingPointer");
            pointerLine = pointer.AddComponent<LineRenderer>();
            pointerLine.positionCount = 2;
            pointerLine.startWidth = 0.025f;
            pointerLine.endWidth = 0.01f;
            pointerLine.material = new Material(Shader.Find("Sprites/Default"));
            pointerLine.startColor = Color.cyan;
            pointerLine.endColor = Color.cyan;
        }
    }

    private void UpdateVisuals()
    {
        if (handVisual != null && holdPoint != null)
        {
            handVisual.position = holdPoint.position;
            handVisual.rotation = holdPoint.rotation;
        }

        if (pointerLine != null)
        {
            pointerLine.SetPosition(0, GetPointerOrigin());
            pointerLine.SetPosition(1, heldRigidbody != null ? heldRigidbody.position : targetPoint);
            pointerLine.startColor = targetedTrashItem != null || heldRigidbody != null ? Color.green : Color.cyan;
            pointerLine.endColor = targetedTrashItem != null || heldRigidbody != null ? Color.green : Color.cyan;
        }

        if (targetMarker != null)
        {
            bool hasTarget = targetedTrashItem != null && heldRigidbody == null;
            targetMarker.gameObject.SetActive(hasTarget);
            if (hasTarget)
            {
                targetMarker.position = targetedTrashItem.transform.position + Vector3.up * 0.45f;
            }
        }
    }

    private Vector3 GetPointerOrigin()
    {
        if (holdPoint != null)
        {
            return holdPoint.position;
        }

        return playerCamera != null ? playerCamera.transform.position : transform.position;
    }

    private Vector3 GetPointerDirection()
    {
        return playerCamera != null ? playerCamera.transform.forward : transform.forward;
    }

    private bool WasPickupPressed()
    {
        if (WasOVRPickupPressed())
        {
            lastInputSource = "OVR pickup";
            return true;
        }

        if (WasInputSystemPickupPressed())
        {
            lastInputSource = "InputSystem XR pickup";
            return true;
        }

        bool pickupButtonPressed = IsXRButtonPressed(UnityEngine.XR.CommonUsages.triggerButton) ||
                                   IsXRButtonPressed(UnityEngine.XR.CommonUsages.gripButton) ||
                                   IsXRButtonPressed(UnityEngine.XR.CommonUsages.primaryButton);
        bool pickupButtonDown = pickupButtonPressed && !previousPickupButtonPressed;
        previousPickupButtonPressed = pickupButtonPressed;
        if (pickupButtonDown)
        {
            lastInputSource = "Unity XR pickup";
            return true;
        }

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null &&
            (Keyboard.current.pKey.wasPressedThisFrame ||
             Keyboard.current.tKey.wasPressedThisFrame ||
             Keyboard.current.uKey.wasPressedThisFrame ||
             Keyboard.current.bKey.wasPressedThisFrame ||
             Keyboard.current.digit1Key.wasPressedThisFrame ||
             Keyboard.current.numpad4Key.wasPressedThisFrame ||
             Keyboard.current.numpad1Key.wasPressedThisFrame))
        {
            lastInputSource = "keyboard pickup";
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            return true;
        }
#endif

        return false;
    }

    private bool WasThrowPressed()
    {
        if (WasOVRThrowPressed())
        {
            lastInputSource = "OVR throw";
            return true;
        }

        if (WasInputSystemThrowPressed())
        {
            lastInputSource = "InputSystem XR throw";
            return true;
        }

        bool throwButtonPressed = IsXRButtonPressed(UnityEngine.XR.CommonUsages.secondaryButton) ||
                                  IsXRButtonPressed(UnityEngine.XR.CommonUsages.primary2DAxisClick);
        bool throwButtonDown = throwButtonPressed && !previousThrowButtonPressed;
        previousThrowButtonPressed = throwButtonPressed;
        if (throwButtonDown)
        {
            lastInputSource = "Unity XR throw";
            return true;
        }

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null &&
            (Keyboard.current.oKey.wasPressedThisFrame ||
             Keyboard.current.nKey.wasPressedThisFrame ||
             Keyboard.current.iKey.wasPressedThisFrame ||
             Keyboard.current.digit2Key.wasPressedThisFrame ||
             Keyboard.current.numpad2Key.wasPressedThisFrame))
        {
            lastInputSource = "keyboard throw";
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.O) || Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            return true;
        }
#endif

        return false;
    }

    private bool WasOVRPickupPressed()
    {
        return OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.All) ||
               OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.All) ||
               OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.All);
    }

    private bool WasOVRThrowPressed()
    {
        return OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.All) ||
               OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.All);
    }

    private bool WasInputSystemPickupPressed()
    {
#if ENABLE_INPUT_SYSTEM
        detectedInputSystemControllers = 0;

        foreach (UnityEngine.InputSystem.InputDevice device in UnityEngine.InputSystem.InputSystem.devices)
        {
            if (device is not UnityEngine.InputSystem.XR.XRController controller)
            {
                continue;
            }

            detectedInputSystemControllers++;

            if (IsPressedThisFrame(controller.triggerPressed) ||
                IsPressedThisFrame(controller.gripPressed) ||
                IsPressedThisFrame(controller.primaryButton))
            {
                return true;
            }
        }
#endif

        return false;
    }

    private bool WasInputSystemThrowPressed()
    {
#if ENABLE_INPUT_SYSTEM
        foreach (UnityEngine.InputSystem.InputDevice device in UnityEngine.InputSystem.InputSystem.devices)
        {
            if (device is not UnityEngine.InputSystem.XR.XRController controller)
            {
                continue;
            }

            if (IsPressedThisFrame(controller.secondaryButton) ||
                IsPressedThisFrame(controller.thumbstickClicked))
            {
                return true;
            }
        }
#endif

        return false;
    }

#if ENABLE_INPUT_SYSTEM
    private static bool IsPressedThisFrame(UnityEngine.InputSystem.Controls.ButtonControl button)
    {
        return button != null && button.wasPressedThisFrame;
    }
#endif

    private bool IsXRButtonPressed(InputFeatureUsage<bool> button)
    {
        UnityEngine.XR.InputDevice leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (leftController.isValid && leftController.TryGetFeatureValue(button, out bool leftPressed) && leftPressed)
        {
            return true;
        }

        UnityEngine.XR.InputDevice rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rightController.isValid && rightController.TryGetFeatureValue(button, out bool rightPressed) && rightPressed)
        {
            return true;
        }

        return false;
    }

    public string GetDebugStatus()
    {
        string targetName = targetedTrashItem != null ? targetedTrashItem.name : "none";
        string heldName = heldRigidbody != null ? heldRigidbody.name : "none";
        return $"Target: {targetName} | Held: {heldName} | Last input: {lastInputSource} | InputSystem XR controllers: {detectedInputSystemControllers}";
    }
}
