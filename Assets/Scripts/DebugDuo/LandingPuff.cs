using System.Collections;
using UnityEngine;

namespace DebugDuo
{
    /// <summary>
    /// Lightweight one-shot puff: a flat cylinder that expands outward and fades from
    /// its tint color to transparent over a short duration, then destroys itself. No
    /// ParticleSystem, no prefab — call LandingPuff.Spawn(position, color) and forget.
    /// </summary>
    public class LandingPuff : MonoBehaviour
    {
        [SerializeField] private float duration   = 0.40f;
        [SerializeField] private Vector3 startScale = new Vector3(0.30f, 0.05f, 0.30f);
        [SerializeField] private Vector3 endScale   = new Vector3(1.40f, 0.05f, 1.40f);

        public static LandingPuff Spawn(Vector3 worldPos, Color tint, float scaleMult = 1f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "LandingPuff";
            Object.Destroy(go.GetComponent<Collider>());

            var rend = go.GetComponent<Renderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            // Best-effort URP transparent setup
            if (mat.HasProperty("_Surface"))  mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend"))    mat.SetFloat("_Blend",   0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend",  (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend",  (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite"))   mat.SetInt("_ZWrite",    0);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
            var col = tint; col.a = 0.7f;
            MaterialColorHelper.Set(mat, col);
            rend.material = mat;

            go.transform.position   = worldPos;
            go.transform.localScale = Vector3.zero;

            var puff = go.AddComponent<LandingPuff>();
            puff.startScale *= scaleMult;
            puff.endScale   *= scaleMult;
            puff.StartCoroutine(puff.Animate(mat, tint));
            return puff;
        }

        private IEnumerator Animate(Material mat, Color tint)
        {
            float t = 0f;
            Color start = tint; start.a = 0.7f;
            Color end   = tint; end.a   = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / duration);
                float ease = 1f - (1f - u) * (1f - u); // ease-out
                transform.localScale = Vector3.Lerp(startScale, endScale, ease);
                MaterialColorHelper.Set(mat, Color.Lerp(start, end, ease));
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
