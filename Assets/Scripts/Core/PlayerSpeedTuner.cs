using System;
using System.Reflection;
using UnityEngine;

public class PlayerSpeedTuner : MonoBehaviour
{
    public float speedMultiplier = 2.25f;
    public float minimumMoveSpeed = 4.2f;

    private bool applied;
    private int attempts;

    private void Start()
    {
        ApplySpeedBoost();
    }

    private void Update()
    {
        if (!applied && attempts < 180)
        {
            ApplySpeedBoost();
        }
    }

    private void ApplySpeedBoost()
    {
        attempts++;
        applied = false;

        foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude))
        {
            if (behaviour == null)
            {
                continue;
            }

            Type type = behaviour.GetType();
            string typeName = type.Name;
            bool looksLikeLocomotion =
                ContainsIgnoreCase(typeName, "PlayerController") ||
                ContainsIgnoreCase(typeName, "Locomotion") ||
                ContainsIgnoreCase(typeName, "Movement");

            if (!looksLikeLocomotion)
            {
                continue;
            }

            applied |= BoostMember(behaviour, type, "MoveScaleMultiplier", 1f, speedMultiplier);
            applied |= BoostMember(behaviour, type, "moveSpeed", minimumMoveSpeed, speedMultiplier);
            applied |= BoostMember(behaviour, type, "MoveSpeed", minimumMoveSpeed, speedMultiplier);
            applied |= BoostMember(behaviour, type, "Speed", minimumMoveSpeed, speedMultiplier);
            applied |= BoostMember(behaviour, type, "Acceleration", 0f, 1.55f);
        }
    }

    private static bool ContainsIgnoreCase(string text, string value)
    {
        return text != null && text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool BoostMember(object target, Type type, string memberName, float minimumValue, float multiplier)
    {
        FieldInfo field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(float))
        {
            float currentValue = (float)field.GetValue(target);
            field.SetValue(target, Mathf.Max(minimumValue, currentValue * multiplier));
            return true;
        }

        PropertyInfo property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property != null && property.PropertyType == typeof(float) && property.CanRead && property.CanWrite)
        {
            float currentValue = (float)property.GetValue(target);
            property.SetValue(target, Mathf.Max(minimumValue, currentValue * multiplier));
            return true;
        }

        return false;
    }
}
