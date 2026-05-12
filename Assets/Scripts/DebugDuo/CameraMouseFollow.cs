using UnityEngine;

namespace DebugDuo
{
    /// <summary>
    /// Subtle interactive camera — applies a small offset to the base position based on
    /// where the mouse is on screen, then lerps toward it each frame. Captures the
    /// camera's transform.position at Start as the rest pose, so it can be added to
    /// any camera without disturbing the original framing.
    /// </summary>
    public class CameraMouseFollow : MonoBehaviour
    {
        [Tooltip("How many world units the camera drifts at the screen edge. Keep small.")]
        [SerializeField, Range(0f, 2f)] private float strengthX = 0.45f;

        [Tooltip("Vertical drift is usually subtler than horizontal.")]
        [SerializeField, Range(0f, 2f)] private float strengthY = 0.25f;

        [Tooltip("Higher = camera reacts faster. Frame-rate independent.")]
        [SerializeField, Range(1f, 12f)] private float lerpSpeed = 4f;

        [Tooltip("Optional small parallax pitch (degrees). Set to 0 to keep rotation locked.")]
        [SerializeField, Range(0f, 5f)] private float pitchSwayDegrees = 0f;

        private Vector3 basePosition;
        private Quaternion baseRotation;

        private void Start()
        {
            basePosition = transform.position;
            baseRotation = transform.rotation;
        }

        private void Update()
        {
            // Normalize mouse position to [-1, 1] from screen center
            float w = Mathf.Max(1f, Screen.width);
            float h = Mathf.Max(1f, Screen.height);
            float nx = Mathf.Clamp((Input.mousePosition.x / w) * 2f - 1f, -1f, 1f);
            float ny = Mathf.Clamp((Input.mousePosition.y / h) * 2f - 1f, -1f, 1f);

            Vector3 targetPos = basePosition + new Vector3(nx * strengthX, ny * strengthY, 0f);

            float k = 1f - Mathf.Exp(-lerpSpeed * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, targetPos, k);

            if (pitchSwayDegrees > 0f)
            {
                Quaternion targetRot = baseRotation * Quaternion.Euler(-ny * pitchSwayDegrees, nx * pitchSwayDegrees, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, k);
            }
        }
    }
}
