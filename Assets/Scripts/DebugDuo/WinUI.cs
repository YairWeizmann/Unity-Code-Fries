using UnityEngine;

namespace DebugDuo
{
    public class WinUI : MonoBehaviour
    {
        [SerializeField] private GameObject winPanel;
        private static WinUI instance;

        public void SetPanel(GameObject panel) { winPanel = panel; if (panel != null) panel.SetActive(false); }

        private void Awake()
        {
            instance = this;
            if (winPanel != null) winPanel.SetActive(false);
        }

        public static void Show()
        {
            if (instance == null || instance.winPanel == null) return;
            bool wasInactive = !instance.winPanel.activeSelf;
            instance.winPanel.SetActive(true);
            if (wasInactive && AudioManager.Instance != null) AudioManager.Instance.PlaySuccess();
        }

        public static void Hide()
        {
            if (instance != null && instance.winPanel != null)
                instance.winPanel.SetActive(false);
        }
    }
}
