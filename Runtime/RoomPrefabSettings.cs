using UnityEngine;

namespace DungeonsForProBuilder
{
    public enum SizeMode
    {
        UsePrefabSize,
        UseBoundsSize,
        CustomSize
    }

    public enum BackDirection
    {
        NorthEast,
        SouthEast,
        SouthWest,
        NorthWest
    }

    /// <summary>
    /// Defines prefab and dimension settings for generating a room from a ProBuilder cube.
    /// If a prefab reference is null, the system will fall back to generating a ProBuilder cube
    /// with the requested dimensions and apply the provided materials.
    /// </summary>
    [CreateAssetMenu(fileName = "RoomPrefabSettings", menuName = "DungeonsForPB/Room Prefab Settings", order = 0)]
    public class RoomPrefabSettings : ScriptableObject
    {
        [Header("Container Prefabs")]
        [Tooltip("Optional prefab to instantiate as parent of the room")]
        public GameObject roomPrefab;
        
        [Tooltip("Optional prefab to instantiate as parent of the door")]
        public GameObject doorPrefab;
        
        [Header("Back Direction")]
        [Tooltip("Specifies which corner direction is considered the 'back'")]
        public BackDirection backDirection = BackDirection.NorthEast;

        [Header("Floor and Ceiling Settings")]
        [Tooltip("Enable floor generation")]
        public bool enableFloor = true;
        
        [Tooltip("Height of the floor (thickness)")]
        [Min(0.001f)] public float floorHeight = 0.1f;
        
        [Tooltip("Enable ceiling generation")]
        public bool enableCeiling = true;
        
        [Tooltip("Height of the ceiling (thickness)")]
        [Min(0.001f)] public float ceilingHeight = 0.1f;

        [Header("Wall Settings")]
        [Tooltip("Prefab to use for walls")]
        public GameObject wallPrefab;
        
        [Min(0.001f)] public float wallWidth = 1f;
        [Min(0.001f)] public float wallHeight = 1f;
        [Min(0.001f)] public float wallDepth = 0.2f;
        
        [Tooltip("Additional height added to back walls (base height + this value)")]
        [Min(0.0f)] public float wallBackHeight = 1f;

        [Header("Corner Settings")]
        [Tooltip("Prefab to use for corners")]
        public GameObject cornerPrefab;
        
        [Min(0.001f)] public float cornerWidth = 1f;
        [Min(0.001f)] public float cornerHeight = 1f;
        [Min(0.001f)] public float cornerDepth = 0.2f;
        
        [Tooltip("Additional height added to back corners (base height + this value)")]
        [Min(0.0f)] public float cornerBackHeight = 1f;
    }
}


