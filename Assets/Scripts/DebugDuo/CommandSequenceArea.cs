using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace DebugDuo
{
    /// <summary>
    /// Drop zone that holds an ordered sequence of CommandBlocks.
    /// Drag a palette block onto it to append. Click any block to remove it.
    /// </summary>
    public class CommandSequenceArea : MonoBehaviour, IDropHandler
    {
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private TMP_Text emptyHint;
        [SerializeField] private Vector2 blockSize = new Vector2(150f, 40f);
        [SerializeField] private float fontSize = 18f;
        [SerializeField] private Color blockColor = new Color(0.3f, 0.6f, 1f);

        private readonly List<CommandBlockUI> blocks = new List<CommandBlockUI>();

        public IReadOnlyList<CommandBlockUI> Blocks => blocks;

        public void Configure(RectTransform content, TMP_Text hint, Vector2 size, float font, Color color)
        {
            contentRoot = content;
            emptyHint = hint;
            blockSize = size;
            fontSize = font;
            blockColor = color;
            UpdateHint();
        }

        public void OnDrop(PointerEventData eventData)
        {
            var dragged = eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponent<CommandBlockUI>()
                : null;
            if (dragged == null || !dragged.IsPaletteSource) return;
            AddCommand(dragged.CommandType);
        }

        public CommandBlockUI AddCommand(CommandType type)
        {
            var block = CommandBlockUI.Create(contentRoot, type, isPaletteSource: false, owner: this, color: blockColor, size: blockSize, fontSize: fontSize);
            blocks.Add(block);
            UpdateHint();
            return block;
        }

        public void RemoveBlock(CommandBlockUI block)
        {
            if (block == null) return;
            blocks.Remove(block);
            Destroy(block.gameObject);
            UpdateHint();
        }

        public void ClearAll()
        {
            for (int i = blocks.Count - 1; i >= 0; i--)
            {
                if (blocks[i] != null) Destroy(blocks[i].gameObject);
            }
            blocks.Clear();
            UpdateHint();
        }

        public List<CommandStep> GetCommands()
        {
            var list = new List<CommandStep>(blocks.Count);
            for (int i = 0; i < blocks.Count; i++)
            {
                var b = blocks[i];
                if (b != null) list.Add(new CommandStep(b.CommandType, b.EffectiveRepeat));
            }
            return list;
        }

        private void UpdateHint()
        {
            if (emptyHint != null) emptyHint.gameObject.SetActive(blocks.Count == 0);
        }
    }
}
