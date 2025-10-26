using UnityEngine;

namespace DungeonsForProBuilder
{
    public enum WallDirection
    {
        North,  // +Z direction
        South,  // -Z direction
        East,   // +X direction
        West    // -X direction
    }

    public enum CornerDirection
    {
        NorthEast,  // +X, +Z
        SouthEast,  // +X, -Z
        SouthWest,  // -X, -Z
        NorthWest   // -X, +Z
    }

    /// <summary>
    /// Component that identifies a floor prefab instance
    /// </summary>
    public class RoomFloor : MonoBehaviour
    {
    }

    /// <summary>
    /// Component that identifies a ceiling prefab instance
    /// </summary>
    public class RoomCeiling : MonoBehaviour
    {
    }

    /// <summary>
    /// Component that identifies a wall prefab instance with its direction
    /// </summary>
    public class RoomWall : MonoBehaviour
    {
        [Header("Room Wall")]
        [Tooltip("The direction this wall faces")]
        public WallDirection direction;
        
        /// <summary>
        /// Get the normalized direction vector for this wall
        /// </summary>
        public Vector3 GetDirectionVector()
        {
            switch (direction)
            {
                case WallDirection.North:
                    return Vector3.forward;
                case WallDirection.South:
                    return Vector3.back;
                case WallDirection.East:
                    return Vector3.right;
                case WallDirection.West:
                    return Vector3.left;
                default:
                    return Vector3.zero;
            }
        }
        
        /// <summary>
        /// Get the opposite direction of this wall
        /// </summary>
        public WallDirection GetOppositeDirection()
        {
            switch (direction)
            {
                case WallDirection.North:
                    return WallDirection.South;
                case WallDirection.South:
                    return WallDirection.North;
                case WallDirection.East:
                    return WallDirection.West;
                case WallDirection.West:
                    return WallDirection.East;
                default:
                    return WallDirection.North;
            }
        }
    }

    /// <summary>
    /// Component that identifies a corner prefab instance with its direction
    /// </summary>
    public class RoomCorner : MonoBehaviour
    {
        [Header("Room Corner")]
        [Tooltip("The direction this corner faces")]
        public CornerDirection direction;
        
        /// <summary>
        /// Get the normalized direction vector for this corner
        /// </summary>
        public Vector3 GetDirectionVector()
        {
            switch (direction)
            {
                case CornerDirection.NorthEast:
                    return new Vector3(1, 0, 1).normalized;
                case CornerDirection.SouthEast:
                    return new Vector3(1, 0, -1).normalized;
                case CornerDirection.SouthWest:
                    return new Vector3(-1, 0, -1).normalized;
                case CornerDirection.NorthWest:
                    return new Vector3(-1, 0, 1).normalized;
                default:
                    return Vector3.zero;
            }
        }
        
        /// <summary>
        /// Get the wall directions that form this corner
        /// </summary>
        public (WallDirection, WallDirection) GetAdjacentWallDirections()
        {
            switch (direction)
            {
                case CornerDirection.NorthEast:
                    return (WallDirection.North, WallDirection.East);
                case CornerDirection.SouthEast:
                    return (WallDirection.South, WallDirection.East);
                case CornerDirection.SouthWest:
                    return (WallDirection.South, WallDirection.West);
                case CornerDirection.NorthWest:
                    return (WallDirection.North, WallDirection.West);
                default:
                    return (WallDirection.North, WallDirection.East);
            }
        }
    }
}
