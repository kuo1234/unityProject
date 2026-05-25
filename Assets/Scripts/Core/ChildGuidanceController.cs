using UnityEngine;

public class ChildGuidanceController : MonoBehaviour
{
    private const float PromptDistance = 0.55f;
    private const float PromptTextSize = 0.018f;
    private const float FlashlightPulseSpeed = 3.2f;
    private const float FlashlightHeight = 2.4f;
    private const float FlashlightRange = 4.8f;
    private const float FlashlightSpotAngle = 62f;
    private const float FlashlightBaseIntensity = 5.6f;
    private const float FlashlightPulseAmount = 0.22f;
    private const float HideFlashlightNearTargetDistance = 0.65f;
    private const float BeamTopRadius = 0.1f;
    private static readonly Color GuidanceTextColor = new Color(0.08f, 0.11f, 0.14f);
    private static readonly Color PositiveGuidanceTextColor = new Color(0.03f, 0.28f, 0.12f);

    private Transform promptAnchor;
    private TextMesh promptText;
    private Transform targetFlashlight;
    private Light targetFlashlightLight;
    private Transform targetFlashlightBeam;
    private Renderer targetFlashlightBeamRenderer;
    private Transform targetFlashlightSpot;
    private Renderer targetFlashlightSpotRenderer;
    private Transform highlightedTarget;
    private string currentMessage = string.Empty;

    public bool spotlightEnabled = true;
    public string CurrentMessage => currentMessage;

    private void Awake()
    {
        EnsurePrompt();
        EnsureFlashlight();
    }

    private void LateUpdate()
    {
        PositionPrompt();
        UpdateFlashlight();
    }

    public void Show(string message, Transform target = null, bool positive = false)
    {
        EnsurePrompt();
        if (spotlightEnabled)
        {
            EnsureFlashlight();
        }

        currentMessage = string.IsNullOrWhiteSpace(message) ? "Pick up a trash item" : message;
        highlightedTarget = spotlightEnabled ? target : null;

        if (promptText != null)
        {
            promptText.text = currentMessage;
            promptText.color = positive ? PositiveGuidanceTextColor : GuidanceTextColor;
            promptText.gameObject.SetActive(true);
        }

        if (targetFlashlight != null)
        {
            targetFlashlight.gameObject.SetActive(spotlightEnabled && highlightedTarget != null);
        }
    }

    public void SetSpotlightEnabled(bool enabled)
    {
        spotlightEnabled = enabled;
        if (!spotlightEnabled)
        {
            highlightedTarget = null;
            if (targetFlashlight != null)
            {
                targetFlashlight.gameObject.SetActive(false);
            }
        }
    }

    public void ClearTarget()
    {
        highlightedTarget = null;
        if (targetFlashlight != null)
        {
            targetFlashlight.gameObject.SetActive(false);
        }
    }

    private void EnsurePrompt()
    {
        if (promptText != null)
        {
            return;
        }

        GameObject anchor = new GameObject("ChildGuidancePrompt");
        promptAnchor = anchor.transform;

        GameObject textObject = new GameObject("ChildGuidanceText");
        textObject.transform.SetParent(promptAnchor, false);
        textObject.transform.localPosition = Vector3.zero;
        textObject.transform.localRotation = Quaternion.identity;

        promptText = textObject.AddComponent<TextMesh>();
        promptText.alignment = TextAlignment.Center;
        promptText.anchor = TextAnchor.MiddleCenter;
        promptText.characterSize = PromptTextSize;
        promptText.fontSize = 68;
        promptText.fontStyle = FontStyle.Bold;
        promptText.richText = false;
        promptText.color = GuidanceTextColor;

        MeshRenderer textRenderer = textObject.GetComponent<MeshRenderer>();
        textRenderer.sharedMaterial = CreateOverlayTextMaterial(promptText.font, 5015);
        textRenderer.sortingOrder = 5015;
    }

    private void EnsureFlashlight()
    {
        if (targetFlashlight != null)
        {
            return;
        }

        GameObject lightObject = new GameObject("ChildGuidanceBinFlashlight");
        targetFlashlight = lightObject.transform;

        targetFlashlightLight = lightObject.AddComponent<Light>();
        targetFlashlightLight.type = LightType.Spot;
        targetFlashlightLight.color = new Color(1f, 0.9f, 0.35f);
        targetFlashlightLight.range = FlashlightRange;
        targetFlashlightLight.spotAngle = FlashlightSpotAngle;
        targetFlashlightLight.intensity = FlashlightBaseIntensity;
        targetFlashlightLight.shadows = LightShadows.None;

        GameObject beam = new GameObject("ChildGuidanceLighthouseBeam");
        beam.transform.SetParent(targetFlashlight, false);
        targetFlashlightBeam = beam.transform;
        MeshFilter beamMeshFilter = beam.AddComponent<MeshFilter>();
        beamMeshFilter.sharedMesh = CreateBeamMesh();
        targetFlashlightBeamRenderer = beam.AddComponent<MeshRenderer>();
        targetFlashlightBeamRenderer.sharedMaterial = CreateTransparentMaterial("Runtime_Child_Guidance_Beam", new Color(1f, 0.88f, 0.18f, 0.34f));
        targetFlashlightBeamRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        targetFlashlightBeamRenderer.receiveShadows = false;

        GameObject spot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        spot.name = "ChildGuidanceBinLightSpot";
        spot.transform.SetParent(targetFlashlight, false);
        targetFlashlightSpot = spot.transform;
        targetFlashlightSpotRenderer = spot.GetComponent<Renderer>();
        targetFlashlightSpotRenderer.sharedMaterial = CreateSpotMaterial();
        targetFlashlightSpotRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        targetFlashlightSpotRenderer.receiveShadows = false;

        Collider spotCollider = spot.GetComponent<Collider>();
        if (spotCollider != null)
        {
            Destroy(spotCollider);
        }

        targetFlashlight.gameObject.SetActive(false);
    }

    private void PositionPrompt()
    {
        Camera camera = Camera.main;
        if (camera == null || promptAnchor == null)
        {
            return;
        }

        if (promptAnchor.parent != camera.transform)
        {
            promptAnchor.SetParent(camera.transform, false);
        }

        promptAnchor.localPosition = new Vector3(0f, 0.17f, Mathf.Max(camera.nearClipPlane + 0.12f, PromptDistance));
        promptAnchor.localRotation = Quaternion.identity;
    }

    private void UpdateFlashlight()
    {
        if (!spotlightEnabled || targetFlashlight == null || highlightedTarget == null)
        {
            if (targetFlashlight != null)
            {
                targetFlashlight.gameObject.SetActive(false);
            }

            return;
        }

        Camera camera = Camera.main;
        if (camera != null && Vector3.Distance(camera.transform.position, highlightedTarget.position) <= HideFlashlightNearTargetDistance)
        {
            targetFlashlight.gameObject.SetActive(false);
            return;
        }

        Bounds targetBounds = GetTargetBounds(highlightedTarget);
        float diameter = Mathf.Max(targetBounds.size.x, targetBounds.size.z);
        targetFlashlight.gameObject.SetActive(true);
        float lightHeightAboveTarget = Mathf.Max(FlashlightHeight, targetBounds.extents.y + 0.65f);
        float beamBottomY = 0.02f;
        float beamHeight = Mathf.Max(FlashlightHeight, targetBounds.center.y + lightHeightAboveTarget - beamBottomY);
        targetFlashlight.position = new Vector3(targetBounds.center.x, beamBottomY + beamHeight, targetBounds.center.z);
        targetFlashlight.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);

        if (targetFlashlightLight != null)
        {
            float pulse = 1f + (Mathf.Sin(Time.time * FlashlightPulseSpeed) * FlashlightPulseAmount);
            targetFlashlightLight.intensity = FlashlightBaseIntensity * pulse;
            targetFlashlightLight.range = Mathf.Max(FlashlightRange, diameter * 1.35f);
        }

        if (targetFlashlightBeam != null)
        {
            float bottomRadius = Mathf.Clamp(diameter * 0.72f, 0.8f, 1.55f);
            targetFlashlightBeam.localPosition = Vector3.zero;
            targetFlashlightBeam.localRotation = Quaternion.identity;
            targetFlashlightBeam.localScale = new Vector3(bottomRadius, bottomRadius, beamHeight);
        }

        if (targetFlashlightSpot != null)
        {
            float spotPulse = 1f + (Mathf.Sin(Time.time * FlashlightPulseSpeed) * 0.035f);
            targetFlashlightSpot.position = new Vector3(targetBounds.center.x, targetBounds.max.y + 0.035f, targetBounds.center.z);
            targetFlashlightSpot.rotation = Quaternion.identity;
            targetFlashlightSpot.localScale = new Vector3(Mathf.Clamp(diameter * 0.34f, 0.28f, 0.55f) * spotPulse, 0.012f, Mathf.Clamp(diameter * 0.34f, 0.28f, 0.55f) * spotPulse);
        }
    }

    private static float GetTargetDiameter(Transform target)
    {
        Bounds bounds = GetTargetBounds(target);
        return Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
    }

    private static Bounds GetTargetBounds(Transform target)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds;
        }

        renderer = target.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds;
        }

        return new Bounds(target.position, target.lossyScale);
    }

    private static Mesh CreateBeamMesh()
    {
        const int segments = 32;
        Vector3[] vertices = new Vector3[segments * 2];
        int[] triangles = new int[segments * 6];

        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            float x = Mathf.Cos(angle);
            float y = Mathf.Sin(angle);
            vertices[i] = new Vector3(x * BeamTopRadius, y * BeamTopRadius, 0f);
            vertices[i + segments] = new Vector3(x, y, 1f);

            int next = (i + 1) % segments;
            int triangleIndex = i * 6;
            triangles[triangleIndex] = i;
            triangles[triangleIndex + 1] = i + segments;
            triangles[triangleIndex + 2] = next;
            triangles[triangleIndex + 3] = next;
            triangles[triangleIndex + 4] = i + segments;
            triangles[triangleIndex + 5] = next + segments;
        }

        Mesh mesh = new Mesh();
        mesh.name = "Runtime_Child_Guidance_Beam_Mesh";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Material CreateOverlayTextMaterial(Font font, int renderQueue)
    {
        Material material = new Material(font.material);
        material.name = "Runtime_Child_Guidance_Text";
        material.renderQueue = renderQueue;
        material.SetInt("_ZWrite", 0);
        material.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        return material;
    }

    private static Material CreateSpotMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material material = new Material(shader);
        material.name = "Runtime_Child_Guidance_Bin_Spot";
        Color color = new Color(1f, 0.92f, 0.28f, 1f);
        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", color * 1.5f);
        }

        return material;
    }

    private static Material CreateTransparentMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material material = new Material(shader);
        material.name = materialName;
        material.color = color;
        material.renderQueue = 3000;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        material.SetInt("_Surface", 1);
        material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        return material;
    }
}
