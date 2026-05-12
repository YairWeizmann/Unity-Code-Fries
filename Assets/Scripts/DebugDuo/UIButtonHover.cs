using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DebugDuo
{
    /// <summary>
    /// Lightweight hover/press feedback that smoothly lerps brightness and scale toward
    /// a target each frame. Pointer state is tracked locally; visuals are driven from
    /// Update so the transitions feel animated instead of snapping.
    ///
    /// Plays nicely with existing Buttons (set transition to None — UIButtonHover owns
    /// the visual) and with custom click handlers; only touches Image.color and
    /// transform.localScale, never click logic.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class UIButtonHover : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        [Header("Brightness")]
        [SerializeField, Range(1f, 2f)]    private float hoverBrightness = 1.20f;
        [SerializeField, Range(0.3f, 1f)]  private float pressBrightness = 0.85f;

        [Header("Scale")]
        [SerializeField, Range(1f, 1.3f)]  private float hoverScale = 1.05f;
        [SerializeField, Range(0.7f, 1f)]  private float pressScale = 0.95f;

        [Header("Smoothing")]
        [Tooltip("Higher = snappier transitions. Frame-rate independent.")]
        [SerializeField, Range(2f, 30f)]   private float lerpSpeed = 14f;

        private Image img;
        private RectTransform rt;
        private Color baseColor;
        private Vector3 baseScale = Vector3.one;

        private bool pointerInside;
        private bool pointerDown;

        private float currentBrightness = 1f, targetBrightness = 1f;
        private float currentScale      = 1f, targetScale      = 1f;

        private void Awake()
        {
            img = GetComponent<Image>();
            rt  = (RectTransform)transform;
        }

        private void OnEnable()
        {
            if (img == null) img = GetComponent<Image>();
            if (rt  == null) rt  = (RectTransform)transform;
            if (img != null) baseColor = img.color;
            baseScale = rt != null ? rt.localScale : Vector3.one;

            currentBrightness = targetBrightness = 1f;
            currentScale      = targetScale      = 1f;
            Apply();
        }

        /// <summary>
        /// Refresh the cached base color — call after programmatically changing
        /// Image.color (e.g. ready button toggling between gray/green).
        /// </summary>
        public void RefreshBaseColor()
        {
            if (img != null) baseColor = img.color;
            Apply();
        }

        public void OnPointerEnter(PointerEventData _) { pointerInside = true;  UpdateTargets(); }
        public void OnPointerExit (PointerEventData _) { pointerInside = false; UpdateTargets(); }
        public void OnPointerDown (PointerEventData _) { pointerDown   = true;  UpdateTargets(); }
        public void OnPointerUp   (PointerEventData _) { pointerDown   = false; UpdateTargets(); }

        private void UpdateTargets()
        {
            if (pointerInside && pointerDown)
            {
                targetBrightness = pressBrightness;
                targetScale      = pressScale;
            }
            else if (pointerInside)
            {
                targetBrightness = hoverBrightness;
                targetScale      = hoverScale;
            }
            else
            {
                targetBrightness = 1f;
                targetScale      = 1f;
            }
        }

        private void Update()
        {
            // Frame-rate independent damp toward target
            float k = 1f - Mathf.Exp(-lerpSpeed * Time.unscaledDeltaTime);
            currentBrightness = Mathf.Lerp(currentBrightness, targetBrightness, k);
            currentScale      = Mathf.Lerp(currentScale,      targetScale,      k);
            Apply();
        }

        private void Apply()
        {
            if (img != null)
            {
                img.color = new Color(
                    Mathf.Clamp01(baseColor.r * currentBrightness),
                    Mathf.Clamp01(baseColor.g * currentBrightness),
                    Mathf.Clamp01(baseColor.b * currentBrightness),
                    baseColor.a);
            }
            if (rt != null)
            {
                rt.localScale = baseScale * currentScale;
            }
        }
    }
}
