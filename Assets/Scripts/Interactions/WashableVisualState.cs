using UnityEngine;

public class WashableVisualState : MonoBehaviour
{
    public Material[] cleanMaterials;
    public Material[] dirtyMaterials;

    private Renderer[] renderers;

    private void Awake()
    {
        CacheRenderers();
        TrashItem item = GetComponent<TrashItem>();
        ApplyState(item != null && item.isDirty);
    }

    public void ApplyDirty()
    {
        ApplyState(true);
    }

    public void ApplyClean()
    {
        ApplyState(false);
    }

    private void ApplyState(bool dirty)
    {
        CacheRenderers();
        Material[] sourceMaterials = dirty ? dirtyMaterials : cleanMaterials;
        if (sourceMaterials == null || sourceMaterials.Length == 0)
        {
            return;
        }

        int materialIndex = 0;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length && materialIndex < sourceMaterials.Length; i++)
            {
                if (sourceMaterials[materialIndex] != null)
                {
                    materials[i] = sourceMaterials[materialIndex];
                }

                materialIndex++;
            }

            renderer.sharedMaterials = materials;
        }
    }

    private void CacheRenderers()
    {
        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>(true);
        }
    }
}
