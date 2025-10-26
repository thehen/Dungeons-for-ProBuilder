using UnityEngine;
using UnityEngine.ProBuilder;

namespace DungeonsForProBuilder
{
    /// <summary>
    /// Main runtime component for the Dungeons for ProBuilder skeleton plugin
    /// </summary>
    public class DungeonEditor : MonoBehaviour
    {
        [Header("Dungeon Settings")]
        [SerializeField] private int dungeonWidth = 20;
        [SerializeField] private int dungeonHeight = 20;
        [SerializeField] private int roomCount = 10;
        
        [Header("Materials")]
        [SerializeField] private Material floorMaterial;
        [SerializeField] private Material wallMaterial;
        [SerializeField] private Material ceilingMaterial;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        
        /// <summary>
        /// Gets the current dungeon width
        /// </summary>
        public int DungeonWidth => dungeonWidth;
        
        /// <summary>
        /// Gets the current dungeon height
        /// </summary>
        public int DungeonHeight => dungeonHeight;
        
        /// <summary>
        /// Gets the current room count
        /// </summary>
        public int RoomCount => roomCount;
        
        /// <summary>
        /// Gets the floor material
        /// </summary>
        public Material FloorMaterial => floorMaterial;
        
        /// <summary>
        /// Gets the wall material
        /// </summary>
        public Material WallMaterial => wallMaterial;
        
        /// <summary>
        /// Gets the ceiling material
        /// </summary>
        public Material CeilingMaterial => ceilingMaterial;
        
        private void Start()
        {
        }
        
        /// <summary>
        /// Creates a simple ProBuilder cube for testing
        /// </summary>
        [ContextMenu("Create Test Cube")]
        public void CreateTestCube()
        {
            var cube = ShapeGenerator.GenerateCube(PivotLocation.Center, new Vector3(1, 1, 1));
            cube.gameObject.name = "Test Cube";
            cube.transform.SetParent(transform);
            
            if (floorMaterial != null)
            {
                cube.GetComponent<Renderer>().sharedMaterial = floorMaterial;
            }
        }
        
        /// <summary>
        /// Clears all child objects
        /// </summary>
        [ContextMenu("Clear All")]
        public void ClearAll()
        {
            while (transform.childCount > 0)
            {
                if (Application.isPlaying)
                    Destroy(transform.GetChild(0).gameObject);
                else
                    DestroyImmediate(transform.GetChild(0).gameObject);
            }
        }
        
        private void OnValidate()
        {
            // Clamp values to reasonable ranges
            dungeonWidth = Mathf.Max(1, dungeonWidth);
            dungeonHeight = Mathf.Max(1, dungeonHeight);
            roomCount = Mathf.Max(0, roomCount);
        }
        
        private void OnDrawGizmos()
        {
            if (showDebugInfo)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(transform.position + new Vector3(dungeonWidth * 0.5f, 0, dungeonHeight * 0.5f), 
                    new Vector3(dungeonWidth, 0.1f, dungeonHeight));
            }
        }
    }
}
