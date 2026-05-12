using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DebugDuo
{
    /// <summary>
    /// Tile-based command executor. The player has a discrete cell position and facing direction.
    /// Each command takes a fixed step duration so both players stay in lockstep.
    /// </summary>
    public class PlayerCommandExecutor : MonoBehaviour
    {
        [Header("Identity")]
        [Tooltip("Which player this is. TileGridManager pairs this with a Starting Tile that has the same Spawn Player.")]
        [SerializeField] private PlayerSlot slot = PlayerSlot.None;

        [Header("Grid State (auto-filled from Starting Tile at Play; no need to set manually)")]
        [SerializeField] private Vector2Int startCell;
        [SerializeField] private Direction startFacing = Direction.East;
        [SerializeField] private float yOffset = 0.65f;

        [Header("Timing")]
        [SerializeField] private float walkDuration = 0.35f;
        [SerializeField] private float turnDuration = 0.25f;
        [SerializeField] private float waitDuration = 1f;
        [SerializeField] private float pressDuration = 0.3f;

        [Header("Movement Polish")]
        [Tooltip("Vertical peak of the hop arc when stepping between tiles.")]
        [SerializeField] private float hopHeight = 0.35f;
        [Tooltip("Tint of the puff that pops under the player when they land.")]
        [SerializeField] private Color landingTint = new Color(1f, 1f, 1f);

        private Vector2Int cell;
        private Direction facing;
        private bool isRunning;
        private TileGridManager grid;

        public Vector2Int Cell => cell;
        public Direction Facing => facing;
        public bool IsRunning => isRunning;
        public PlayerSlot Slot => slot;

        public void SetSlot(PlayerSlot value) => slot = value;

        private void Awake()
        {
            cell = startCell;
            facing = startFacing;
        }

        private void Start()
        {
            grid = TileGridManager.Instance;
            SnapToCell();
        }

        public void SetStart(Vector2Int startCell, Direction startFacing)
        {
            this.startCell = startCell;
            this.startFacing = startFacing;
            ResetToStart();
        }

        public void ResetToStart()
        {
            cell = startCell;
            facing = startFacing;
            SnapToCell();
        }

        /// <summary>
        /// Place the player on its current logical cell. World position comes from the
        /// tile that's registered at that cell — never from cell*tileSize math — so the
        /// player always lands on the actual painted geometry.
        /// </summary>
        private void SnapToCell()
        {
            if (grid == null) grid = TileGridManager.Instance;
            if (grid == null) return;

            transform.rotation = Quaternion.Euler(0f, facing.ToYRotation(), 0f);

            if (grid.TryGetTile(cell, out var tile) && tile != null)
            {
                Vector3 center = tile.GetVisualCenter();
                transform.position = center + Vector3.up * yOffset;
                Debug.Log($"[DebugDuo] {name}: SnapToCell → currentCell={cell}, tile='{tile.name}', center={center}, world position set to {transform.position}.");
            }
            else
            {
                Debug.LogWarning($"[DebugDuo] {name}: SnapToCell — no tile registered at currentCell={cell}. Position not changed.");
            }
        }

        // Set true when something during a step requires the rest of the queue to stop
        // (e.g. landed on an active Enemy Tile and the question popup opened).
        private bool runInterrupted;

        public IEnumerator RunSequence(IList<CommandStep> commands)
        {
            if (isRunning) yield break;
            isRunning = true;
            runInterrupted = false;
            for (int i = 0; i < commands.Count; i++)
            {
                if (runInterrupted)
                {
                    Debug.Log($"[DebugDuo] {name}: Run interrupted at step {i + 1}/{commands.Count} — remaining steps skipped.");
                    break;
                }

                var step = commands[i];
                int repeats = step.RepeatCount < 1 ? 1 : step.RepeatCount;
                Debug.Log($"[DebugDuo] {name}: Executing {step.Type.DisplayName()} repeat {repeats}");

                for (int r = 0; r < repeats; r++)
                {
                    if (runInterrupted) break;
                    yield return Execute(step.Type);
                }
            }
            isRunning = false;
        }

        private IEnumerator Execute(CommandType command)
        {
            switch (command)
            {
                case CommandType.Walk:        yield return StepForward(1);  break;
                case CommandType.Run:         yield return StepForward(2);  break;
                case CommandType.WalkBack:    yield return StepBackward(1); break;
                case CommandType.TurnLeft:    yield return Turn(false);     break;
                case CommandType.TurnRight:   yield return Turn(true);      break;
                case CommandType.Wait:        yield return WaitWithBounce();              break;
                case CommandType.PressButton: yield return PressButton();   break;
            }
        }

        private IEnumerator StepForward(int tiles)
        {
            for (int i = 0; i < tiles; i++)
            {
                yield return TryStep(facing.ToCellDelta(), tiles == 2 ? "Run" : "Walk");
                if (lastStepBlocked) yield break; // bumped — skip remaining tiles in this command
            }
        }

        private IEnumerator StepBackward(int tiles)
        {
            for (int i = 0; i < tiles; i++)
            {
                yield return TryStep(-facing.ToCellDelta(), "Walk Back");
                if (lastStepBlocked) yield break;
            }
        }

        private bool lastStepBlocked;

        /// <summary>
        /// Cell-only step. Computes targetCell, looks up the tile, animates the player
        /// to that tile's visual center, then commits currentCell = targetCell.
        /// World position is never used as input here — only as the animation start point.
        /// </summary>
        private IEnumerator TryStep(Vector2Int delta, string commandLabel)
        {
            lastStepBlocked = false;

            if (grid == null) grid = TileGridManager.Instance;
            Vector2Int targetCell = cell + delta;

            Debug.Log($"[DebugDuo] {name}: {commandLabel} step — currentCell={cell}, facing={facing}, delta={delta}, targetCell={targetCell}.");

            if (grid == null)
            {
                Debug.LogWarning($"[DebugDuo] {name}: {commandLabel} blocked — no TileGridManager in scene. Target cell {targetCell}.");
                if (AudioManager.Instance != null) AudioManager.Instance.PlayFail();
                lastStepBlocked = true;
                yield return new WaitForSeconds(walkDuration);
                yield break;
            }

            bool exists = grid.TryGetTile(targetCell, out var nextTile);
            if (!exists || nextTile == null)
            {
                Debug.Log($"[DebugDuo] {name}: {commandLabel} blocked — TileGridManager has no tile at cell {targetCell} (off the grid / missing tile).");
                if (AudioManager.Instance != null) AudioManager.Instance.PlayFail();
                lastStepBlocked = true;
                yield return new WaitForSeconds(walkDuration);
                yield break;
            }

            Vector3 nextCenter = nextTile.GetVisualCenter();
            Debug.Log($"[DebugDuo] {name}: target tile found — name='{nextTile.name}', type={nextTile.Type}, center={nextCenter}.");

            if (!nextTile.IsPassable())
            {
                string why = nextTile.IsObstacle
                    ? "Obstacle Tile"
                    : (nextTile.Type == TileType.Door ? "closed Door Tile" : $"{nextTile.Type} not passable");
                Debug.Log($"[DebugDuo] {name}: {commandLabel} blocked — cell {targetCell} is {why} ('{nextTile.name}').");
                if (AudioManager.Instance != null) AudioManager.Instance.PlayFail();
                lastStepBlocked = true;
                yield return new WaitForSeconds(walkDuration);
                yield break;
            }

            Vector3 endPos = nextCenter + Vector3.up * yOffset;
            yield return AnimateMoveTo(endPos);
            cell = targetCell;
            Debug.Log($"[DebugDuo] {name}: step complete — currentCell now {cell}, world position {transform.position}.");

            // Landed on an active Enemy Tile? Halt the run and open the question UI.
            if (nextTile.enemy != null && !nextTile.enemy.IsDefeated)
            {
                Debug.Log($"[DebugDuo] {name}: stepped onto active Enemy Tile '{nextTile.name}' — pausing run, opening question.");
                if (EnemyQuestionManager.Instance != null)
                {
                    EnemyQuestionManager.Instance.Open(nextTile.enemy);
                }
                else
                {
                    Debug.LogWarning("[DebugDuo] No EnemyQuestionManager in the scene — enemy cannot be defeated.");
                }
                runInterrupted = true;
                lastStepBlocked = true; // also stop within the current command (e.g. Run = 2 tiles)
            }
        }

        /// <summary>
        /// Hops the player from current world position to endPos along a sine arc.
        /// On landing, spawns a small puff at the landing point.
        /// </summary>
        private IEnumerator AnimateMoveTo(Vector3 endPos)
        {
            Vector3 startPos = transform.position;
            float t = 0f;
            while (t < walkDuration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / walkDuration);
                Vector3 pos = Vector3.Lerp(startPos, endPos, u);
                pos.y += hopHeight * Mathf.Sin(u * Mathf.PI); // arc peaks at u=0.5
                transform.position = pos;
                yield return null;
            }
            transform.position = endPos;

            // Tiny puff at the player's feet on landing
            Vector3 puffPos = endPos - Vector3.up * (yOffset - 0.05f);
            LandingPuff.Spawn(puffPos, landingTint);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayStep();
        }

        private IEnumerator Turn(bool right)
        {
            Direction newFacing = right ? facing.TurnRight() : facing.TurnLeft();
            Quaternion startRot = transform.rotation;
            Quaternion endRot   = Quaternion.Euler(0f, newFacing.ToYRotation(), 0f);
            Vector3    origin   = transform.position;
            float t = 0f;
            while (t < turnDuration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / turnDuration);
                transform.rotation = Quaternion.Slerp(startRot, endRot, u);
                // Small bounce so turning feels animated
                transform.position = origin + Vector3.up * (Mathf.Sin(u * Mathf.PI) * 0.08f);
                yield return null;
            }
            transform.rotation = endRot;
            transform.position = origin;
            facing = newFacing;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayTurn();
        }

        /// <summary>
        /// Wait command: idle bounce in place. Visually telegraphs that the player is
        /// deliberately holding instead of frozen.
        /// </summary>
        private IEnumerator WaitWithBounce()
        {
            Vector3 origin = transform.position;
            float t = 0f;
            const float amplitude = 0.08f;
            const int   bounces   = 2;
            while (t < waitDuration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / waitDuration);
                float y = Mathf.Abs(Mathf.Sin(u * Mathf.PI * bounces)) * amplitude;
                transform.position = origin + Vector3.up * y;
                yield return null;
            }
            transform.position = origin;
        }

        private static readonly Vector2Int[] AdjacentDeltas =
        {
            new Vector2Int(0, 1), new Vector2Int(1, 0),
            new Vector2Int(0, -1), new Vector2Int(-1, 0)
        };

        private IEnumerator PressButton()
        {
            Debug.Log($"[DebugDuo] {name}: Press Button command executed at cell {cell}");

            ButtonTrigger found = null;
            Vector2Int foundCell = cell;

            // 1. Standing on a button tile
            if (grid != null && grid.TryGetTile(cell, out var tile) && tile.button != null)
            {
                found = tile.button;
                foundCell = cell;
            }
            else
            {
                // 2. Adjacent button tile (4-directional)
                for (int i = 0; i < AdjacentDeltas.Length; i++)
                {
                    Vector2Int adj = cell + AdjacentDeltas[i];
                    if (grid != null && grid.TryGetTile(adj, out var adjTile) && adjTile.button != null)
                    {
                        found = adjTile.button;
                        foundCell = adj;
                        break;
                    }
                }
            }

            if (found != null)
            {
                Debug.Log($"[DebugDuo] {name}: Button found at cell {foundCell}");
                found.Press();
                Debug.Log($"[DebugDuo] {name}: Button activated (door opened)");
            }
            else
            {
                Debug.Log($"[DebugDuo] {name}: No button found at or near cell {cell}");
            }

            yield return new WaitForSeconds(pressDuration);
        }
    }
}
