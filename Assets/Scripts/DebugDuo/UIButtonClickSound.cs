using UnityEngine;
using UnityEngine.EventSystems;

namespace DebugDuo
{
    /// <summary>
    /// Drop on any clickable UI element (Button, Toggle, custom). Plays the click SFX
    /// via AudioManager whenever Unity dispatches a click event to this GameObject.
    /// Doesn't interfere with Button.onClick or other handlers — IPointerClickHandler
    /// fires alongside them.
    /// </summary>
    public class UIButtonClickSound : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData _)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayClick();
        }
    }
}
