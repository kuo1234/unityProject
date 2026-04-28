using UnityEngine;

public enum TrashCategory
{
    General,
    Recyclable_Plastic,
    Recyclable_Paper,
    FoodWaste_Raw,
    FoodWaste_Cooked
}

public class TrashItem : MonoBehaviour
{
    public TrashCategory itemType;
    public bool isDirty = false;
    public bool isCompoundItem = false;
}
