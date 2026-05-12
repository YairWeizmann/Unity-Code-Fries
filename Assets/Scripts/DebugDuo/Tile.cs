using UnityEngine;

namespace DebugDuo
{
    public enum TileType
    {
        // Existing values are preserved (do not renumber) so saved scenes keep working.
        // Display names override the default enum spelling in the Inspector dropdown.
        [InspectorName("Floor Tile")]    Floor    = 0,
        [InspectorName("Obstacle Tile")] Wall     = 1,
        [InspectorName("Button Tile")]   Button   = 2,
        [InspectorName("Door Tile")]     Door     = 3,
        [InspectorName("End Tile")]      Goal     = 4,
        [InspectorName("Starting Tile")] Starting = 5,
        [InspectorName("Enemy Tile")]    Enemy    = 6
    }

    /// <summary>
    /// One tile in the level grid. Cell is auto-computed from the world position
    /// using TileGridManager.TileSize, so designers don't manually type Cell X/Y —
    /// they just snap the prefab to Unity's grid.
    ///
    /// Cell is recomputed at edit-time (OnValidate / [ExecuteAlways] Update) and
    /// at Awake so runtime registration always matches the tile's actual position.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class Tile : MonoBehaviour
    {
        [Header("Tile Settings")]
        [Tooltip("Pick what kind of tile this is. Floor tiles are passable; Obstacle tiles block movement; End tiles trigger the win condition.")]
        [SerializeField] private TileType type = TileType.Floor;

        [Tooltip("Auto-computed from world position via TileGridManager.TileSize. Read-only — do not edit by hand.")]
        [SerializeField] private Vector2Int cell;

        // Same-GameObject component links populated either by prefab setup or scene builder.
        [Header("Component Links")]
        public ButtonTrigger button;
        public DoorController door;
        public GoalZone goal;
        public EnemyEncounter enemy;

        [Header("Starting Tile Settings (only used when Type = Starting Tile)")]
        [Tooltip("Which player should spawn on this tile when the level starts. Set to None for non-spawn Starting Tiles.")]
        [SerializeField] private PlayerSlot spawnPlayer = PlayerSlot.None;

        [Tooltip("Direction the spawned player should face.")]
        [SerializeField] private Direction startFacing = Direction.East;

        public PlayerSlot SpawnPlayer => spawnPlayer;
        public Direction StartFacing => startFacing;

        public void SetSpawn(PlayerSlot slot, Direction facing)
        {
            spawnPlayer = slot;
            startFacing = facing;
        }

        public TileType Type => type;
        public Vector2Int Cell => cell;

        public bool IsObstacle => type == TileType.Wall;
        public bool IsEnd      => type == TileType.Goal;
        public bool IsStarting => type == TileType.Starting;

        /// <summary>Used by the scene builder; designers normally don't call this.</summary>
        public void Init(Vector2Int cell, TileType type)
        {
            this.cell = cell;
            this.type = type;
        }

        public void SetType(TileType type) => this.type = type;

        /// <summary>
        /// Recompute cell from the tile's VISUAL CENTER (Renderer.bounds.center) rather
        /// than transform.position, because Unity's Grid can place prefabs whose pivots
        /// are at corner/edge — e.g., visual center (-0.5, 0, 1.5) for a tile that
        /// logically belongs at cell (0, 2). The Floor(c + 0.5) formula maps each
        /// half-offset center to the cell that contains it, so adjacent tiles always
        /// register at adjacent cells regardless of pivot.
        /// </summary>
        public Vector2Int RecomputeCell()
        {
            Vector3 center = GetVisualCenter();
            cell = new Vector2Int(
                Mathf.FloorToInt(center.x + 0.5f),
                Mathf.FloorToInt(center.z + 0.5f));
            return cell;
        }

        private void Awake()
        {
            RecomputeCell();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RecomputeCell();
        }

        // [ExecuteAlways] makes Update fire in edit mode, so cell stays correct as
        // the user drags the tile in the scene view (OnValidate alone misses that).
        private void Update()
        {
            if (!Application.isPlaying) RecomputeCell();
        }
#endif

        public bool IsPassable()
        {
            switch (type)
            {
                case TileType.Wall: return false;
                case TileType.Door: return door != null && door.IsOpen;
                default:            return true; // Floor, Button, Goal, Starting all walkable
            }
        }

        /// <summary>
        /// World-space center of this tile's visible footprint. Used for both spawning
        /// and movement so the player always lands on the actual geometry — not on a
        /// computed cell-grid position which can drift if the Grid component's anchor
        /// or cell size doesn't perfectly match TileGridManager.tileSize.
        ///
        /// Prefers the bounds of a child named "Floor" (the walkable surface for every
        /// floor-style tile in this project). Falls back to the first child Renderer,
        /// then to transform.position.
        /// </summary>
        public Vector3 GetVisualCenter()
        {
            var floor = transform.Find("Floor");
            if (floor != null)
            {
                var fr = floor.GetComponent<Renderer>();
                if (fr != null) return fr.bounds.center;
            }

            var any = GetComponentInChildren<Renderer>();
            if (any != null) return any.bounds.center;

            return transform.position;
        }
    }
}
