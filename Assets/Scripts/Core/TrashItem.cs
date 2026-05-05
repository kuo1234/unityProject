using UnityEngine;

public enum TrashCategory
{
    General,
    Recyclable,
    Food
}

public class TrashItem : MonoBehaviour
{
    public TrashCategory itemType;
    public bool isDirty = false;
    public bool isCompoundItem = false;
}
