using UnityEngine;

namespace DebugDuo
{
    /// <summary>
    /// Sets material color in a way that works for both URP Lit (_BaseColor) and Built-in Standard (_Color).
    /// </summary>
    public static class MaterialColorHelper
    {
        public static void Set(Material mat, Color color)
        {
            if (mat == null) return;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))     mat.SetColor("_Color", color);
        }
    }
}
