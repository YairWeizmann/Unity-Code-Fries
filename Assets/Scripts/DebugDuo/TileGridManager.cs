using System.Collections.Generic;
using UnityEngine;

namespace DebugDuo
{
    /// <summary>
    /// Owns the runtime tile dictionary. At Awake, finds every Tile in the scene,
    /// asks each one to recompute its cell from its world position, and registers
    /// it by Vector2Int cell so the player movement system can do tile lookups.
    ///
    /// Tile prefabs dropped into a scene work without manual Cell X/Y entry —
    /// position them on Unity's grid (Move snap = TileSize) and they self-register.
    /// </summary>
    public class TileGridManager : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField, Tooltip("World units per tile. Set to 1 to match Unity's default Move snap.")]
        private float tileSize = 1f;

        [SerializeField, Tooltip("World position that maps to cell (0,0).")]
        private Vector3 origin = Vector3.zero;

        // Cached static values so Tile.OnValidate / Tile.Update (edit mode) don't
        // need a FindObjectByType call per tile per frame.
        public static float CurrentTileSize { get; private set; } = 1f;
        public static Vector3 CurrentOrigin { get; private set; } = Vector3.zero;

        private static TileGridManager instance;
        public static TileGridManager Instance => instance;

        public float TileSize => tileSize;
        public Vector3 Origin => origin;

        private readonly Dictionary<Vector2Int, Tile> tiles = new Dictionary<Vector2Int, Tile>();

        public static TileGridManager FindInScene()
        {
            if (instance != null) return instance;
            return FindFirstObjectByType<TileGridManager>();
        }

        private void OnValidate()
        {
            CurrentTileSize = Mathf.Max(0.0001f, tileSize);
            CurrentOrigin = origin;
        }

        private void OnEnable()
        {
            CurrentTileSize = Mathf.Max(0.0001f, tileSize);
            CurrentOrigin = origin;
        }

        private void Awake()
        {
            instance = this;
            CurrentTileSize = Mathf.Max(0.0001f, tileSize);
            CurrentOrigin = origin;
            RebuildIndex();
            AssignPlayerSpawns();
        }

        /// <summary>
        /// Walks the registered tiles, finds StartingTiles with a non-None SpawnPlayer,
        /// and snaps the matching PlayerCommandExecutor to that tile's cell/facing.
        /// </summary>
        public void AssignPlayerSpawns()
        {
            var players = FindObjectsByType<PlayerCommandExecutor>(FindObjectsSortMode.None);
            int spawned = 0;

            foreach (var pair in tiles)
            {
                var tile = pair.Value;
                if (tile == null) continue;
                if (tile.Type != TileType.Starting) continue;
                if (tile.SpawnPlayer == PlayerSlot.None) continue;

                PlayerCommandExecutor target = null;
                for (int i = 0; i < players.Length; i++)
                {
                    if (players[i].Slot == tile.SpawnPlayer) { target = players[i]; break; }
                }

                if (target == null)
                {
                    Debug.LogWarning(
                        $"[TileGridManager] StartingTile '{tile.name}' wants to spawn {tile.SpawnPlayer} " +
                        $"but no PlayerCommandExecutor in the scene has Slot={tile.SpawnPlayer}.");
                    continue;
                }

                target.SetStart(tile.Cell, tile.StartFacing);
                spawned++;
                Debug.Log(
                    $"[TileGridManager] Spawned {target.name} ({tile.SpawnPlayer}) — " +
                    $"currentCell={tile.Cell} facing={tile.StartFacing}, " +
                    $"StartingTile='{tile.name}' visual center={tile.GetVisualCenter()}, " +
                    $"player world position={target.transform.position}.");
            }

            if (spawned == 0)
            {
                Debug.Log("[TileGridManager] No StartingTiles assigned spawns — players keep their existing startCell/startFacing.");
            }
        }

        /// <summary>
        /// Re-scans the entire scene for Tile components and rebuilds the cell-lookup
        /// dictionary. Safe to call at runtime — call this if you spawn / move tiles
        /// after Awake (e.g. from a level loader, Tilemap GameObject brush at edit time
        /// followed by entering Play, etc.).
        /// </summary>
        public void RebuildIndex()
        {
            tiles.Clear();
            var allTiles = FindObjectsByType<Tile>(FindObjectsSortMode.None);
            Debug.Log($"[TileGridManager] Scanning scene — found {allTiles.Length} Tile component(s).");

            for (int i = 0; i < allTiles.Length; i++)
            {
                var t = allTiles[i];
                t.RecomputeCell();

                if (tiles.TryGetValue(t.Cell, out var existing) && existing != t)
                {
                    Debug.LogWarning(
                        $"[TileGridManager] Two tiles at cell {t.Cell}: '{existing.name}' and '{t.name}'. " +
                        $"Last one wins — check your grid for duplicates / overlapping prefabs.");
                }
                tiles[t.Cell] = t;

                Debug.Log(
                    $"[TileGridManager] Registered '{t.name}' ({t.Type}) — " +
                    $"visual center={t.GetVisualCenter()} → cell={t.Cell}.");
            }

            Debug.Log($"[TileGridManager] Index rebuild complete. {tiles.Count} unique cell(s) registered.");
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        public void Register(Tile tile)
        {
            if (tile == null) return;
            tiles[tile.Cell] = tile;
        }

        public bool TryGetTile(Vector2Int cell, out Tile tile) => tiles.TryGetValue(cell, out tile);

        public bool TileExists(Vector2Int cell) => tiles.ContainsKey(cell);

        public Vector3 CellToWorld(Vector2Int cell, float y = 0f)
        {
            return origin + new Vector3(cell.x * tileSize, y, cell.y * tileSize);
        }

        public Vector2Int WorldToCell(Vector3 worldPos)
        {
            float ts = Mathf.Max(0.0001f, tileSize);
            return new Vector2Int(
                Mathf.RoundToInt((worldPos.x - origin.x) / ts),
                Mathf.RoundToInt((worldPos.z - origin.z) / ts));
        }

        // Movement queries the player's executor uses
        public bool IsPassable(Vector2Int cell)
        {
            return tiles.TryGetValue(cell, out var t) && t.IsPassable();
        }

        public bool IsObstacle(Vector2Int cell)
        {
            return tiles.TryGetValue(cell, out var t) && t.IsObstacle;
        }

        public bool IsEnd(Vector2Int cell)
        {
            return tiles.TryGetValue(cell, out var t) && t.IsEnd;
        }
    }
}
