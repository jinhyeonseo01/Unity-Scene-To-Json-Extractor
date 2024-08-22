using UnityEngine;

public static class MaterialExtensions
{
    public static Texture TryGetTexture(this Material material, params string[] name)
    {
        foreach (var n in name)
        {
            if (material.HasProperty(n))
                return material.GetTexture(n);
        }

        return null;
    }

    public static Color TryGetColor(this Material material, params string[] name)
    {
        foreach (var n in name)
        {
            if (material.HasProperty(n))
                return material.GetColor(n);
        }

        return Color.black;
    }

    public static Vector4 TryGetVector(this Material material, params string[] name)
    {
        foreach (var n in name)
        {
            if (material.HasProperty(n))
                return material.GetVector(n);
        }

        return Vector4.zero;
    }

    public static float TryGetFloat(this Material material, params string[] name)
    {
        foreach (var n in name)
        {
            if (material.HasProperty(n))
                return material.GetFloat(n);
        }

        return 0.0f;
    }
}