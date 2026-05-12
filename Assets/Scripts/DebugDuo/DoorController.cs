using System.Collections;
using UnityEngine;

namespace DebugDuo
{
    /// <summary>
    /// A door that lives on a tile. While closed, its tile reports IsPassable=false.
    /// Opening animates the visual down and flips the IsOpen flag.
    /// </summary>
    public class DoorController : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField] private Vector3 openOffset = new Vector3(0f, -2.2f, 0f);
        [SerializeField] private float openDuration = 0.6f;

        private Vector3 visualClosedPos;
        private bool isOpen;
        private Coroutine animCo;

        public bool IsOpen => isOpen;

        public void SetVisual(Transform t)
        {
            visual = t;
        }

        private void Awake()
        {
            if (visual != null) visualClosedPos = visual.localPosition;
        }

        public void Open()
        {
            if (isOpen) return;
            isOpen = true;
            if (animCo != null) StopCoroutine(animCo);
            if (visual != null) animCo = StartCoroutine(MoveVisualTo(visualClosedPos + openOffset));
        }

        public void ResetDoor()
        {
            isOpen = false;
            if (animCo != null) StopCoroutine(animCo);
            if (visual != null) visual.localPosition = visualClosedPos;
        }

        private IEnumerator MoveVisualTo(Vector3 target)
        {
            Vector3 start = visual.localPosition;
            float t = 0f;
            while (t < openDuration)
            {
                t += Time.deltaTime;
                visual.localPosition = Vector3.Lerp(start, target, Mathf.Clamp01(t / openDuration));
                yield return null;
            }
            visual.localPosition = target;
        }
    }
}
