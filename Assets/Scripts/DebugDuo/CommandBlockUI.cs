using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace DebugDuo
{
    /// <summary>
    /// A single command block. Two modes:
    ///  - Palette source: dragging spawns a ghost; on drop into a sequence area, that area creates a new block.
    ///  - Sequence block: clicked to remove itself from the sequence.
    /// </summary>
    public class CommandBlockUI : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerClickHandler
    {
        // These two MUST be serialized so they survive scene save → Play reload.
        // Without [SerializeField], private fields reset to defaults at Play start
        // and every block thinks IsPaletteSource=false (drag never starts).
        [SerializeField] private CommandType commandType;
        [SerializeField] private bool isPaletteSource;

        [Header("Loop / Repeat (sequence blocks only)")]
        [SerializeField] private bool loopEnabled = false;
        [SerializeField, Range(1, 20)] private int repeatCount = 1;

        // Cached loop UI references (sequence blocks only)
        private Toggle loopToggle;
        private TMP_Text countLabel;

        public CommandType CommandType => commandType;
        public bool IsPaletteSource => isPaletteSource;
        public bool LoopEnabled => loopEnabled;
        public int RepeatCount => repeatCount;

        /// <summary>Repeat count actually used by the executor (1 when loop is off).</summary>
        public int EffectiveRepeat => loopEnabled ? Mathf.Clamp(repeatCount, 1, 20) : 1;

        private Canvas rootCanvas;


        // Sequence-only owner; only set at runtime when blocks are spawned by drop, so
        // it does not need to survive scene reload. Palette-source blocks have it null.
        public CommandSequenceArea OwnerSequence { get; private set; }

        private Image background;
        private TMP_Text label;
        private RectTransform rt;
        private GameObject ghost;

        private void Awake()
        {
            // Re-resolve component refs on Play. They were set in editor by Init() but
            // private (non-serialized) fields don't persist across scene reload.
            rt         = (RectTransform)transform;
            background = GetComponent<Image>();
            label      = GetComponentInChildren<TMP_Text>();
        }

        public void Init(CommandType type, bool paletteSource, CommandSequenceArea owner, Image bg, TMP_Text txt)
        {
            commandType     = type;
            isPaletteSource = paletteSource;
            OwnerSequence   = owner;
            background      = bg;
            label           = txt;
            rt              = (RectTransform)transform;
            if (label != null) label.text = type.DisplayName();
        }

        public void SetColor(Color color)
        {
            if (background != null) background.color = color;
        }

        // ---- Drag (palette only) ----

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!IsPaletteSource) return;
            rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
            // Spawn ghost — a copy that follows the cursor without affecting layout
            ghost = new GameObject($"GhostBlock_{CommandType}", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            var ghostRT = (RectTransform)ghost.transform;
            ghostRT.SetParent(rootCanvas.transform, false);
            ghostRT.sizeDelta = rt.sizeDelta;
            ghostRT.position = eventData.position;

            var ghostImg = ghost.GetComponent<Image>();
            ghostImg.color = background != null ? background.color : Color.white;

            var ghostCG = ghost.GetComponent<CanvasGroup>();
            ghostCG.blocksRaycasts = false;
            ghostCG.alpha = 0.85f;

            var ghostLabelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var ghostLabelRT = (RectTransform)ghostLabelGO.transform;
            ghostLabelRT.SetParent(ghostRT, false);
            ghostLabelRT.anchorMin = Vector2.zero;
            ghostLabelRT.anchorMax = Vector2.one;
            ghostLabelRT.offsetMin = new Vector2(8, 4);
            ghostLabelRT.offsetMax = new Vector2(-8, -4);
            var ghostTmp = ghostLabelGO.GetComponent<TextMeshProUGUI>();
            ghostTmp.text = CommandType.DisplayName();
            ghostTmp.alignment = TextAlignmentOptions.Center;
            ghostTmp.fontSize = label != null ? label.fontSize : 18f;
            ghostTmp.color = Color.white;
            ghostTmp.raycastTarget = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (ghost == null) return;
            ((RectTransform)ghost.transform).position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (ghost != null) Destroy(ghost);
            ghost = null;
        }

        // ---- Click (sequence only) ----

        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsPaletteSource) return;
            if (eventData.dragging) return; // ignore click after drag
            // Don't remove the block when the user clicks a control inside it
            // (toggle / +/-). pointerPress goes to the actual hit object.
            if (eventData.pointerPress != null && eventData.pointerPress != gameObject) return;
            if (OwnerSequence != null) OwnerSequence.RemoveBlock(this);
        }

        // ---- Loop controls (sequence blocks only) ----

        private void SetLoopEnabled(bool value)
        {
            loopEnabled = value;
            UpdateCountDisplay();
        }

        private void Bump(int delta)
        {
            repeatCount = Mathf.Clamp(repeatCount + delta, 1, 20);
            // Bumping implies the user wants to use the loop — auto-enable on first bump up
            if (delta > 0 && !loopEnabled)
            {
                loopEnabled = true;
                if (loopToggle != null) loopToggle.SetIsOnWithoutNotify(true);
            }
            UpdateCountDisplay();
        }

        private void UpdateCountDisplay()
        {
            if (countLabel == null) return;
            countLabel.text = repeatCount.ToString();
            countLabel.color = loopEnabled ? Color.white : new Color(1f, 1f, 1f, 0.4f);
        }

        // ---- Factory ----

        public static CommandBlockUI Create(RectTransform parent, CommandType type, bool isPaletteSource, CommandSequenceArea owner, Color color, Vector2 size, float fontSize = 20f)
        {
            var go = new GameObject($"CmdBlock_{type}", typeof(RectTransform), typeof(Image), typeof(CommandBlockUI));
            var goRT = (RectTransform)go.transform;
            goRT.SetParent(parent, false);
            goRT.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.color = color;

            // Label
            var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var labelRT = (RectTransform)labelGO.transform;
            labelRT.SetParent(goRT, false);
            var tmp = labelGO.GetComponent<TextMeshProUGUI>();
            tmp.text = type.DisplayName();
            tmp.color = Color.white;
            tmp.fontSize = fontSize;
            tmp.raycastTarget = false;

            if (isPaletteSource)
            {
                // Label fills the block
                labelRT.anchorMin = Vector2.zero;
                labelRT.anchorMax = Vector2.one;
                labelRT.offsetMin = new Vector2(8, 4);
                labelRT.offsetMax = new Vector2(-8, -4);
                tmp.alignment = TextAlignmentOptions.Center;
            }
            else
            {
                // Label on the left half so loop controls fit on the right
                labelRT.anchorMin = new Vector2(0f, 0f);
                labelRT.anchorMax = new Vector2(0f, 1f);
                labelRT.pivot     = new Vector2(0f, 0.5f);
                labelRT.anchoredPosition = new Vector2(8f, 0f);
                labelRT.sizeDelta = new Vector2(95f, 0f);
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
            }

            var block = go.GetComponent<CommandBlockUI>();
            block.Init(type, isPaletteSource, owner, img, tmp);

            // Subtle hover/click brightness so the block feels interactive
            go.AddComponent<UIButtonHover>();

            if (!isPaletteSource) block.BuildLoopControls();
            return block;
        }

        private void BuildLoopControls()
        {
            var blockRT = (RectTransform)transform;

            // Loop toggle — small white square that turns green when on
            var togGO = new GameObject("LoopToggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
            var togRT = (RectTransform)togGO.transform;
            togRT.SetParent(blockRT, false);
            togRT.anchorMin = togRT.anchorMax = togRT.pivot = new Vector2(0.5f, 0.5f);
            togRT.anchoredPosition = new Vector2(15f, 0f);
            togRT.sizeDelta = new Vector2(20f, 20f);
            var togBg = togGO.GetComponent<Image>();
            togBg.color = new Color(0.95f, 0.95f, 0.95f, 0.85f);
            var toggle = togGO.GetComponent<Toggle>();
            toggle.targetGraphic = togBg;
            toggle.transition = Selectable.Transition.None;

            var checkGO = new GameObject("Check", typeof(RectTransform), typeof(Image));
            var checkRT = (RectTransform)checkGO.transform;
            checkRT.SetParent(togRT, false);
            checkRT.anchorMin = Vector2.zero; checkRT.anchorMax = Vector2.one;
            checkRT.offsetMin = new Vector2(3f, 3f); checkRT.offsetMax = new Vector2(-3f, -3f);
            var checkImg = checkGO.GetComponent<Image>();
            checkImg.color = new Color(0.20f, 0.70f, 0.30f);
            checkImg.raycastTarget = false;
            toggle.graphic = checkImg;
            toggle.isOn = loopEnabled;
            toggle.onValueChanged.AddListener(SetLoopEnabled);
            loopToggle = toggle;

            // − button
            MakeMiniButton(blockRT, "Dec", "−", new Vector2(43f, 0f)).onClick.AddListener(() => Bump(-1));

            // count label
            var countGO = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
            var countRT = (RectTransform)countGO.transform;
            countRT.SetParent(blockRT, false);
            countRT.anchorMin = countRT.anchorMax = countRT.pivot = new Vector2(0.5f, 0.5f);
            countRT.anchoredPosition = new Vector2(67f, 0f);
            countRT.sizeDelta = new Vector2(24f, 22f);
            countLabel = countGO.GetComponent<TextMeshProUGUI>();
            countLabel.text = repeatCount.ToString();
            countLabel.alignment = TextAlignmentOptions.Center;
            countLabel.fontSize = 18f;
            countLabel.color = Color.white;
            countLabel.raycastTarget = false;

            // + button
            MakeMiniButton(blockRT, "Inc", "+", new Vector2(91f, 0f)).onClick.AddListener(() => Bump(+1));

            UpdateCountDisplay();
        }

        private static Button MakeMiniButton(RectTransform parent, string name, string label, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(20f, 20f);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.30f, 0.30f, 0.36f);
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;
            go.AddComponent<UIButtonHover>();
            go.AddComponent<UIButtonClickSound>();

            var lblGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var lblRT = (RectTransform)lblGO.transform;
            lblRT.SetParent(rt, false);
            lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = Vector2.zero; lblRT.offsetMax = Vector2.zero;
            var tmp = lblGO.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 22f;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            return btn;
        }
    }
}
