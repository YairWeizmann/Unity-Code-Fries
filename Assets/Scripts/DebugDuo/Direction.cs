using UnityEngine;

namespace DebugDuo
{
    public enum Direction { North, East, South, West }

    public static class DirectionExtensions
    {
        public static Vector2Int ToCellDelta(this Direction d)
        {
            switch (d)
            {
                case Direction.North: return new Vector2Int(0, 1);
                case Direction.East:  return new Vector2Int(1, 0);
                case Direction.South: return new Vector2Int(0, -1);
                case Direction.West:  return new Vector2Int(-1, 0);
            }
            return Vector2Int.zero;
        }

        public static float ToYRotation(this Direction d)
        {
            switch (d)
            {
                case Direction.North: return 0f;
                case Direction.East:  return 90f;
                case Direction.South: return 180f;
                case Direction.West:  return 270f;
            }
            return 0f;
        }

        public static Direction TurnRight(this Direction d) => (Direction)(((int)d + 1) % 4);
        public static Direction TurnLeft(this Direction d)  => (Direction)(((int)d + 3) % 4);
    }
}
