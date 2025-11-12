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

    /// <summary>
    /// Component that identifies a wall prefab instance with its direction
    /// </summary>
    public class RoomWall : MonoBehaviour
    {
        [Header("Room Wall")]
        [Tooltip("The direction this wall faces")]
        public WallDirection direction;
        
        [Header("Height Override")]
        [Tooltip("Enable to override the wall height")]
        public bool overrideHeight = false;
        
        [Tooltip("Custom height for this wall (only used if override is enabled)")]
        public float customHeight = 3f;
        
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
}

