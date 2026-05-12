using UnityEngine;
using UnityEngine.UI;

namespace DebugDuo
{
    /// <summary>
    /// Shows the "How to Play" panel on scene load. Clicking the Start button hides it.
    /// The button onClick is wired at Awake (runtime) so it survives scene save/load —
    /// edit-time AddListener calls produce non-persistent listeners that are dropped
    /// when the scene is serialized.
    /// </summary>
    public class RulesPopup : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button startButton;

        public bool IsOpen => panel != null && panel.activeSelf;

        public void SetReferences(GameObject panel, Button startButton)
        {
            this.panel = panel;
            this.startButton = startButton;
        }

        private void Awake()
        {
            if (panel != null) panel.SetActive(true);
            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(Hide);
            }
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }
    }
}
