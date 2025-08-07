using UnityEngine;

public static class Utils
{
    public static T GetOrAdd<T>(this GameObject gameObject) where T : Component
    {
        if (!gameObject.TryGetComponent<T>(out var existing))
        {
            existing = gameObject.AddComponent<T>();
        }
        
        return existing;
    }
}