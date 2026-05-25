using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class TrashClampGrabber : MonoBehaviour
{
    [Header("References")]
    public Transform gripPoint;

    [Header("Input")]
    public OVRInput.Controller controller = OVRInput.Controller.RTouch;
    public OVRInput.Axis1D closeAxis = OVRInput.Axis1D.PrimaryHandTrigger;
    public KeyCode keyboardCloseKey = KeyCode.G;
    public bool useMouseLeftButton = true;

    [Header("Clamp")]
    public float closeThreshold = 0.7f;
    public float reopenThreshold = 0.35f;
    public float grabRadius = 0.18f;
    public float grabReach = 0.45f;
    public LayerMask grabbableLayers = Physics.DefaultRaycastLayers;

    [Header("Jaw Visuals")]
    public bool autoCreateJawVisuals = true;
    public bool autoCreateBodyVisual = true;
    public float jawOpenOffset = 0.065f;
    public float jawClosedOffset = 0.018f;
    public float jawMoveSpeed = 14f;

    [Header("Feedback")]
    public float grabHapticFrequency = 0.9f;
    public float grabHapticAmplitude = 0.9f;
    public float grabHapticDuration = 0.05f;
    public float releaseHapticFrequency = 0.25f;
    public float releaseHapticAmplitude = 0.35f;
    public float releaseHapticDuration = 0.03f;

    private Rigidbody heldRigidbody;
    private Transform originalParent;
    private bool originalUseGravity;
    private bool originalIsKinematic;
    private Collider[] originalColliders;
    private bool[] originalColliderIsTriggerStates;
    private Transform leftJaw;
    private Transform rightJaw;
    private float jawCloseAmount;
    private Coroutine hapticRoutine;
    private float lastCloseAmount;
    private int lastCandidateCount;
    private string lastCandidateName = "none";

    private void Awake()
    {
        EnsureGripPoint();
        EnsureVisuals();
    }

    private void Update()
    {
        float closeAmount = GetCloseAmount();
        lastCloseAmount = closeAmount;
        UpdateJawAnimation(closeAmount);

        if (heldRigidbody == null && closeAmount >= closeThreshold)
        {
            TryGrab();
        }
        else if (heldRigidbody != null && closeAmount <= reopenThreshold)
        {
            ReleaseHeldItem();
        }
    }

    private void LateUpdate()
    {
        if (heldRigidbody == null || gripPoint == null)
        {
            return;
        }

        heldRigidbody.transform.SetPositionAndRotation(gripPoint.position, gripPoint.rotation);
    }

    private void OnDisable()
    {
        ReleaseHeldItem();
        StopHaptics();
    }

    private float GetCloseAmount()
    {
        float ovrValue = OVRInput.Get(closeAxis, controller);
        if (IsInputSystemClosePressed() || IsLegacyClosePressed())
        {
            ovrValue = Mathf.Max(ovrValue, 1f);
        }

        return Mathf.Clamp01(ovrValue);
    }

    private static Key ConvertKeyCode(KeyCode keyCode)
    {
        return keyCode switch
        {
            KeyCode.G => Key.G,
            KeyCode.Space => Key.Space,
            KeyCode.LeftShift => Key.LeftShift,
            KeyCode.RightShift => Key.RightShift,
            KeyCode.E => Key.E,
            _ => Key.None
        };
    }

    private bool IsInputSystemClosePressed()
    {
        Key inputSystemKey = ConvertKeyCode(keyboardCloseKey);
        if (Keyboard.current != null && inputSystemKey != Key.None)
        {
            var keyControl = Keyboard.current[inputSystemKey];
            if (keyControl != null && keyControl.isPressed)
            {
                return true;
            }
        }

        return useMouseLeftButton &&
               Mouse.current != null &&
               Mouse.current.leftButton != null &&
               Mouse.current.leftButton.isPressed;
    }

    private bool IsLegacyClosePressed()
    {
        try
        {
            if (Input.GetKey(keyboardCloseKey))
            {
                return true;
            }

            return useMouseLeftButton && Input.GetMouseButton(0);
        }
        catch
        {
            return false;
        }
    }

    private void TryGrab()
    {
        if (gripPoint == null)
        {
            return;
        }

        Rigidbody bestBody = FindBestCandidate();

        if (bestBody == null)
        {
            return;
        }

        heldRigidbody = bestBody;
        originalParent = heldRigidbody.transform.parent;
        originalUseGravity = heldRigidbody.useGravity;
        originalIsKinematic = heldRigidbody.isKinematic;
        originalColliders = heldRigidbody.GetComponentsInChildren<Collider>();
        originalColliderIsTriggerStates = new bool[originalColliders.Length];

        heldRigidbody.linearVelocity = Vector3.zero;
        heldRigidbody.angularVelocity = Vector3.zero;
        heldRigidbody.useGravity = false;
        heldRigidbody.isKinematic = true;
        heldRigidbody.transform.SetParent(gripPoint, true);
        heldRigidbody.transform.SetPositionAndRotation(gripPoint.position, gripPoint.rotation);

        SetHeldCollidersTriggerState(true);
        PulseHaptics(grabHapticFrequency, grabHapticAmplitude, grabHapticDuration);

        TrashItem grabbedItem = heldRigidbody.GetComponent<TrashItem>();
        if (grabbedItem != null && GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.NotifyPlayerAction(PlayerGuidanceAction.PickedUpTrash, grabbedItem);
        }
    }

    private void ReleaseHeldItem()
    {
        if (heldRigidbody == null)
        {
            return;
        }

        Rigidbody releasedBody = heldRigidbody;
        releasedBody.transform.SetParent(originalParent, true);
        releasedBody.useGravity = originalUseGravity;
        releasedBody.isKinematic = originalIsKinematic;
        SetHeldCollidersTriggerState(false);

        heldRigidbody = null;
        originalParent = null;
        originalColliders = null;
        originalColliderIsTriggerStates = null;
        PulseHaptics(releaseHapticFrequency, releaseHapticAmplitude, releaseHapticDuration);
    }

    private Rigidbody FindBestCandidate()
    {
        Collider[] overlaps = Physics.OverlapSphere(
            gripPoint.position,
            grabRadius,
            grabbableLayers,
            QueryTriggerInteraction.Ignore);

        if (overlaps.Length == 0 && grabReach > 0f)
        {
            overlaps = Physics.OverlapCapsule(
                gripPoint.position,
                gripPoint.position + (gripPoint.forward * grabReach),
                grabRadius,
                grabbableLayers,
                QueryTriggerInteraction.Ignore);
        }

        Rigidbody bestBody = null;
        float bestDistance = float.MaxValue;
        lastCandidateCount = 0;
        lastCandidateName = "none";

        foreach (Collider overlap in overlaps)
        {
            TrashItem trashItem = overlap.GetComponentInParent<TrashItem>();
            if (trashItem == null)
            {
                continue;
            }

            Rigidbody candidate = trashItem.GetComponent<Rigidbody>();
            if (candidate == null)
            {
                continue;
            }

            lastCandidateCount++;
            float distance = Vector3.SqrMagnitude(overlap.ClosestPoint(gripPoint.position) - gripPoint.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestBody = candidate;
                lastCandidateName = candidate.name;
            }
        }

        return bestBody;
    }

    private void SetHeldCollidersTriggerState(bool isTrigger)
    {
        if (originalColliders == null)
        {
            return;
        }

        for (int i = 0; i < originalColliders.Length; i++)
        {
            Collider itemCollider = originalColliders[i];
            if (itemCollider == null)
            {
                continue;
            }

            if (isTrigger)
            {
                originalColliderIsTriggerStates[i] = itemCollider.isTrigger;
                itemCollider.isTrigger = true;
            }
            else
            {
                itemCollider.isTrigger = originalColliderIsTriggerStates != null && i < originalColliderIsTriggerStates.Length
                    ? originalColliderIsTriggerStates[i]
                    : false;
            }
        }
    }

    private void EnsureGripPoint()
    {
        if (gripPoint != null)
        {
            return;
        }

        Transform existingGrip = transform.Find("GripPoint");
        if (existingGrip != null)
        {
            gripPoint = existingGrip;
            return;
        }

        GameObject grip = new GameObject("GripPoint");
        grip.transform.SetParent(transform, false);
        grip.transform.localPosition = new Vector3(0f, 0f, 0.12f);
        grip.transform.localRotation = Quaternion.identity;
        gripPoint = grip.transform;
    }

    private void EnsureVisuals()
    {
        leftJaw = transform.Find("LeftJaw");
        rightJaw = transform.Find("RightJaw");

        if (autoCreateJawVisuals)
        {
            leftJaw = leftJaw != null
                ? leftJaw
                : EnsurePart("LeftJaw", new Vector3(-jawOpenOffset, 0f, 0.09f), new Vector3(0.025f, 0.025f, 0.18f));
            rightJaw = rightJaw != null
                ? rightJaw
                : EnsurePart("RightJaw", new Vector3(jawOpenOffset, 0f, 0.09f), new Vector3(0.025f, 0.025f, 0.18f));
        }

        if (autoCreateBodyVisual && !HasCustomBodyVisual())
        {
            EnsurePart("ClampBody", new Vector3(0f, 0f, 0f), new Vector3(0.08f, 0.05f, 0.16f));
        }
    }

    private bool HasCustomBodyVisual()
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            if (renderer == null)
            {
                continue;
            }

            string rendererName = renderer.transform.name;
            if (rendererName == "LeftJaw" || rendererName == "RightJaw" || rendererName == "ClampBody")
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private Transform EnsurePart(string partName, Vector3 localPosition, Vector3 localScale)
    {
        Transform part = transform.Find(partName);
        if (part == null)
        {
            GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            primitive.name = partName;
            Destroy(primitive.GetComponent<Collider>());

            Renderer renderer = primitive.GetComponent<Renderer>();
            renderer.material.color = new Color(0.15f, 0.15f, 0.18f);

            primitive.transform.SetParent(transform, false);
            part = primitive.transform;
        }

        part.localPosition = localPosition;
        part.localRotation = Quaternion.identity;
        part.localScale = localScale;
        return part;
    }

    private void UpdateJawAnimation(float closeAmount)
    {
        jawCloseAmount = Mathf.MoveTowards(jawCloseAmount, closeAmount, Time.deltaTime * jawMoveSpeed);
        float jawOffset = Mathf.Lerp(jawOpenOffset, jawClosedOffset, jawCloseAmount);

        if (leftJaw != null)
        {
            leftJaw.localPosition = new Vector3(-jawOffset, 0f, 0.09f);
        }

        if (rightJaw != null)
        {
            rightJaw.localPosition = new Vector3(jawOffset, 0f, 0.09f);
        }
    }

    private void PulseHaptics(float frequency, float amplitude, float duration)
    {
        if (duration <= 0f || amplitude <= 0f)
        {
            return;
        }

        StopHaptics();
        hapticRoutine = StartCoroutine(HapticPulseRoutine(frequency, amplitude, duration));
    }

    private IEnumerator HapticPulseRoutine(float frequency, float amplitude, float duration)
    {
        OVRInput.SetControllerVibration(frequency, amplitude, controller);
        yield return new WaitForSeconds(duration);
        OVRInput.SetControllerVibration(0f, 0f, controller);
        hapticRoutine = null;
    }

    private void StopHaptics()
    {
        if (hapticRoutine != null)
        {
            StopCoroutine(hapticRoutine);
            hapticRoutine = null;
        }

        OVRInput.SetControllerVibration(0f, 0f, controller);
    }

    private void OnDrawGizmosSelected()
    {
        Transform targetGrip = gripPoint != null ? gripPoint : transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(targetGrip.position, grabRadius);
        if (grabReach > 0f)
        {
            Gizmos.DrawWireSphere(targetGrip.position + (targetGrip.forward * grabReach), grabRadius);
        }
    }

    public string GetDebugStatus()
    {
        string heldName = heldRigidbody != null ? heldRigidbody.name : "none";
        return $"Clamp close: {lastCloseAmount:F2} | Held: {heldName} | Candidates: {lastCandidateCount} | Best: {lastCandidateName}";
    }
}
