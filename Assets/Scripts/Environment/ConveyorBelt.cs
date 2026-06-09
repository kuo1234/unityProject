using UnityEngine;

// 簡易程式化輸送帶:帶面接觸到的 Rigidbody 會被沿 worldDirection 等速輸送。
// 帶面需有「實體(非 trigger)Collider」,垃圾才會躺在上面被帶動。
[RequireComponent(typeof(Collider))]
public class ConveyorBelt : MonoBehaviour
{
    [Tooltip("輸送速度(公尺/秒)")]
    public float speed = 1.2f;

    [Tooltip("輸送方向(世界座標)。預設 +X = 由左往右")]
    public Vector3 worldDirection = Vector3.right;

    [Tooltip("帶面材質(用來捲動貼圖營造移動感),可留空")]
    public Renderer scrollRenderer;
    public float textureScrollScale = 1.5f;

    private void OnCollisionStay(Collision collision)
    {
        Rigidbody body = collision.rigidbody;
        if (body == null || body.isKinematic)
        {
            return;
        }

        Vector3 dir = worldDirection.normalized;
        Vector3 horizontal = dir * speed;
        // 水平方向用帶速,垂直(重力)維持,讓垃圾貼著帶面往前送
        body.linearVelocity = new Vector3(horizontal.x, body.linearVelocity.y, horizontal.z);
    }

    private void Update()
    {
        if (scrollRenderer == null)
        {
            return;
        }

        Material mat = scrollRenderer.material;
        float offset = -Time.time * speed * textureScrollScale;
        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTextureOffset("_BaseMap", new Vector2(offset, 0f));
        }
        if (mat.HasProperty("_MainTex"))
        {
            mat.SetTextureOffset("_MainTex", new Vector2(offset, 0f));
        }
    }
}
