using UnityEngine;

public enum TrashCategory
{
    General,
    Recyclable_Plastic,
    Recyclable_Paper,
    Recyclable_Metal,
    Recyclable_Glass,
    FoodWaste_Raw,
    FoodWaste_Cooked
}

public class TrashItem : MonoBehaviour
{
    public TrashCategory itemType;
    public bool isDirty = false;
    public bool isCompoundItem = false;

    [System.NonSerialized] public bool isResolved;
}
