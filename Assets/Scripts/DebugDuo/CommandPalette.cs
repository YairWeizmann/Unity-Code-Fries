using UnityEngine;

namespace DebugDuo
{
    /// <summary>
    /// Holds the source command blocks the player drags from. Spawned by the scene builder.
    /// </summary>
    public class CommandPalette : MonoBehaviour
    {
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private Vector2 blockSize = new Vector2(150f, 50f);
        [SerializeField] private float fontSize = 20f;
        [SerializeField] private Color blockColor = new Color(0.3f, 0.6f, 1f);

        private static readonly CommandType[] DefaultPalette = new[]
        {
            CommandType.Walk,
            CommandType.Run,
            CommandType.TurnLeft,
            CommandType.TurnRight,
            CommandType.Wait,
            CommandType.PressButton
        };

        public void Build()
        {
            BuildWith(DefaultPalette);
        }

        public void BuildWith(CommandType[] commands)
        {
            if (contentRoot == null) contentRoot = (RectTransform)transform;
            // Clear existing
            for (int i = contentRoot.childCount - 1; i >= 0; i--)
            {
                DestroyImmediateOrPlay(contentRoot.GetChild(i).gameObject);
            }
            for (int i = 0; i < commands.Length; i++)
            {
                CommandBlockUI.Create(contentRoot, commands[i], isPaletteSource: true, owner: null, color: blockColor, size: blockSize, fontSize: fontSize);
            }
        }

        public void Configure(RectTransform content, Vector2 size, float font, Color color)
        {
            contentRoot = content;
            blockSize = size;
            fontSize = font;
            blockColor = color;
        }

        private static void DestroyImmediateOrPlay(GameObject go)
        {
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
    }
}
