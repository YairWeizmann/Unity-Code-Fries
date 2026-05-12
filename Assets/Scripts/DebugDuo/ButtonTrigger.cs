using UnityEngine;

namespace DebugDuo
{
    /// <summary>
    /// A button that lives on a tile. Calling Press() activates it (intended to be called by
    /// PlayerCommandExecutor when the player issues "Press Button" while standing on this tile).
    /// </summary>
    public class ButtonTrigger : MonoBehaviour
    {
        [SerializeField] private DoorController linkedDoor;
        [SerializeField] private Renderer indicator;
        [SerializeField] private Color idleColor = Color.red;
        [SerializeField] private Color pressedColor = Color.green;

        private bool isPressed;
        private Material runtimeMat;

        public bool IsPressed => isPressed;

        public void SetLinks(DoorController door, Renderer ind)
        {
            linkedDoor = door;
            indicator = ind;
        }

        private void Awake()
        {
            if (indicator != null)
            {
                runtimeMat = indicator.material;
                MaterialColorHelper.Set(runtimeMat, idleColor);
            }
        }

        public void Press()
        {
            if (isPressed) return;
            isPressed = true;
            if (runtimeMat != null) MaterialColorHelper.Set(runtimeMat, pressedColor);
            if (linkedDoor != null) linkedDoor.Open();
        }

        public void ResetButton()
        {
            isPressed = false;
            if (runtimeMat != null) MaterialColorHelper.Set(runtimeMat, idleColor);
        }
    }
}
