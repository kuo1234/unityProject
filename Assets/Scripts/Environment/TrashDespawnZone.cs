using UnityEngine;

// 放在輸送帶右端的隱形回收區:沒被分類、送到底掉落的垃圾進入此區即消失,
// 避免垃圾在地上堆積、卡住 TrashSpawner 的數量上限。
[RequireComponent(typeof(Collider))]
public class TrashDespawnZone : MonoBehaviour
{
    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnValidate()
    {
        Collider c = GetComponent<Collider>();
        if (c != null)
        {
            c.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TrashItem item = other.GetComponentInParent<TrashItem>();
        if (item != null)
        {
            Destroy(item.gameObject);
        }
    }
}
