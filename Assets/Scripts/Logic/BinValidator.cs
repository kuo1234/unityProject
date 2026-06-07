using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class BinValidator : MonoBehaviour
{
    public TrashCategory targetCategory;

    private const float BounceForce = 6f;
    private const float UpwardForce = 4f;
    private static readonly Color ErrorColor = new Color(1f, 0.08f, 0.04f);

    private Renderer[] renderers;
    private Color[] originalColors;
    private Coroutine feedbackRoutine;
    private readonly Dictionary<TrashItem, float> rejectedItems = new Dictionary<TrashItem, float>();

    public float successVolume = 0.7f;
    private static AudioClip s_successClip;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Awake()
    {
        CacheRenderers();
    }

    private void OnValidate()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (ScoreManager.Instance != null && !ScoreManager.Instance.IsRoundActive)
        {
            return;
        }

        TrashItem trashItem = other.GetComponentInParent<TrashItem>();
        if (trashItem == null)
        {
            return;
        }

        if (trashItem.isResolved)
        {
            return;
        }

        if (trashItem.itemType == targetCategory && !trashItem.isDirty)
        {
            trashItem.isResolved = true;
            PlaySuccessFeedback(trashItem.transform.position + Vector3.up * 0.3f);
            Destroy(trashItem.gameObject);
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddSortedItem();
            }

            Debug.Log("[Success] Sorted correctly");
            return;
        }

        if (rejectedItems.TryGetValue(trashItem, out float rejectUntilTime) && Time.time < rejectUntilTime)
        {
            return;
        }

        rejectedItems[trashItem] = Time.time + 0.75f;
        TriggerWrongBinFeedback();

        Rigidbody itemRigidbody = trashItem.GetComponent<Rigidbody>();
        if (itemRigidbody != null)
        {
            Vector3 outwardDirection = (trashItem.transform.position - transform.position).normalized;
            Vector3 bounceDirection = (outwardDirection * BounceForce) + (Vector3.up * UpwardForce);
            itemRigidbody.AddForce(bounceDirection, ForceMode.Impulse);
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddSortingMistake(trashItem.isDirty ? "Wash first" : "Wrong bin");
        }

#if OCULUS_INTEGRATION || META_XR_CORE_SDK || OVRPLUGIN_PRESENT
        OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.All);
#else
        Debug.LogWarning("OVRInput is not available. Install or enable the Meta XR SDK to use haptic error feedback.");
#endif
    }

    private void PlaySuccessFeedback(Vector3 position)
    {
        Color color = CategoryColor(targetCategory);

        AudioClip clip = GetSuccessClip();
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, position, successVolume);
        }

        SpawnSuccessParticles(position, color);
    }

    private void SpawnSuccessParticles(Vector3 position, Color color)
    {
        GameObject fx = new GameObject("SortSuccessFX");
        fx.transform.position = position;

        ParticleSystem ps = fx.AddComponent<ParticleSystem>();
        ps.Stop();

        ParticleSystem.MainModule main = ps.main;
        main.loop = false;
        main.duration = 1f;
        main.startLifetime = 0.7f;
        main.startSpeed = 3.2f;
        main.startSize = 0.1f;
        main.gravityModifier = 0.4f;
        main.maxParticles = 80;
        main.startColor = color;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 45) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.18f;

        ParticleSystemRenderer particleRenderer = fx.GetComponent<ParticleSystemRenderer>();
        particleRenderer.material = new Material(Shader.Find("Sprites/Default"));

        ps.Play();
        Destroy(fx, 2f);
    }

    private static AudioClip GetSuccessClip()
    {
        if (s_successClip != null)
        {
            return s_successClip;
        }

        int sampleRate = 44100;
        float duration = 0.35f;
        int sampleCount = (int)(sampleRate * duration);
        float[] data = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-7f * t);
            // 兩個和諧音(C6 + E6)做出清脆的「叮」
            float wave = (0.6f * Mathf.Sin(2f * Mathf.PI * 1046.5f * t)) +
                         (0.4f * Mathf.Sin(2f * Mathf.PI * 1318.5f * t));
            data[i] = wave * envelope * 0.5f;
        }

        s_successClip = AudioClip.Create("SortSuccessDing", sampleCount, 1, sampleRate, false);
        s_successClip.SetData(data, 0);
        return s_successClip;
    }

    private static Color CategoryColor(TrashCategory category)
    {
        switch (category)
        {
            case TrashCategory.General: return new Color(0.25f, 0.80f, 0.38f);
            case TrashCategory.Recyclable_Plastic: return new Color(0.20f, 0.55f, 1.00f);
            case TrashCategory.Recyclable_Paper: return new Color(0.98f, 0.82f, 0.22f);
            case TrashCategory.Recyclable_Metal: return new Color(0.78f, 0.82f, 0.85f);
            case TrashCategory.Recyclable_Glass: return new Color(0.20f, 0.85f, 0.85f);
            case TrashCategory.FoodWaste_Raw: return new Color(0.95f, 0.45f, 0.12f);
            case TrashCategory.FoodWaste_Cooked: return new Color(0.95f, 0.45f, 0.12f);
            default: return Color.white;
        }
    }

    private void CacheRenderers()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            Material material = renderers[i].material;
            originalColors[i] = material.color;
        }
    }

    private void TriggerWrongBinFeedback()
    {
        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
        }

        feedbackRoutine = StartCoroutine(FlashWrongBin());
    }

    private IEnumerator FlashWrongBin()
    {
        if (renderers == null || renderers.Length == 0)
        {
            CacheRenderers();
        }

        Vector3 originalScale = transform.localScale;

        for (int pulse = 0; pulse < 3; pulse++)
        {
            SetRendererColors(ErrorColor);
            transform.localScale = originalScale * 1.06f;
            yield return new WaitForSeconds(0.12f);

            RestoreRendererColors();
            transform.localScale = originalScale;
            yield return new WaitForSeconds(0.1f);
        }

        feedbackRoutine = null;
    }

    private void SetRendererColors(Color color)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.color = color;
        }
    }

    private void RestoreRendererColors()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.color = originalColors[i];
        }
    }
}
