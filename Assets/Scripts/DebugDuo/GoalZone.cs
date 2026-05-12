using System.Collections.Generic;
using UnityEngine;

namespace DebugDuo
{
    /// <summary>
    /// Tile-based goal. Each frame after a player completes a step, the RunManager (or executor)
    /// triggers GoalZone.CheckAll() which checks each goal's expected player against its cell.
    /// </summary>
    public class GoalZone : MonoBehaviour
    {
        [SerializeField] private PlayerCommandExecutor expectedPlayer;
        [SerializeField] private Vector2Int cell;
        [SerializeField] private Renderer indicator;
        [SerializeField] private Color idleColor = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private Color reachedColor = new Color(0.2f, 1f, 0.4f, 1f);

        private static readonly HashSet<GoalZone> allZones = new HashSet<GoalZone>();
        private Material runtimeMat;
        private bool reached;

        public bool Reached => reached;

        public void SetLinks(PlayerCommandExecutor player, Vector2Int cell, Renderer ind)
        {
            expectedPlayer = player;
            this.cell = cell;
            indicator = ind;
        }

        private void Awake()
        {
            allZones.Add(this);
            if (indicator != null)
            {
                runtimeMat = indicator.material;
                MaterialColorHelper.Set(runtimeMat, idleColor);
            }
        }

        private void OnDestroy()
        {
            allZones.Remove(this);
        }

        public void EvaluateOnce()
        {
            if (reached) return;
            if (expectedPlayer == null) return;
            if (expectedPlayer.Cell != cell) return;
            reached = true;
            if (runtimeMat != null) MaterialColorHelper.Set(runtimeMat, reachedColor);

            // Success burst above the goal pad — bigger than a normal landing puff
            LandingPuff.Spawn(transform.position + Vector3.up * 0.4f, new Color(1f, 0.85f, 0.2f), scaleMult: 1.6f);
        }

        public void ResetGoal()
        {
            reached = false;
            if (runtimeMat != null) MaterialColorHelper.Set(runtimeMat, idleColor);
        }

        public static bool AllReached()
        {
            if (allZones.Count == 0) return false;
            foreach (var z in allZones) if (!z.reached) return false;
            return true;
        }

        public static void EvaluateAll()
        {
            foreach (var z in allZones) z.EvaluateOnce();
        }

        public static void ResetAll()
        {
            foreach (var z in allZones) z.ResetGoal();
        }
    }
}
