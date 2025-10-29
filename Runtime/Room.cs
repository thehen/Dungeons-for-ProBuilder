using UnityEngine;
using UnityEngine.ProBuilder;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DungeonsForProBuilder
{
    /// <summary>
    /// Component that tracks a room built from a ProBuilder cube.
    /// The original cube becomes the parent and visual components are disabled.
    /// </summary>
    public class Room : MonoBehaviour
    {
        [Header("Room Components")]
        [Tooltip("The floor GameObject")]
        public GameObject floor;
        
        [Tooltip("The ceiling GameObject")]
        public GameObject ceiling;
        
        // Legacy fields - kept for backward compatibility but hidden from inspector
        // Modern rooms use dynamic Walls and Corners parent GameObjects instead
        [HideInInspector] public GameObject northWall;
        [HideInInspector] public GameObject eastWall;
        [HideInInspector] public GameObject southWall;
        [HideInInspector] public GameObject westWall;
        [HideInInspector] public GameObject[] corners = new GameObject[4]; // 0=NE, 1=SE, 2=SW, 3=NW

        [Header("Edit State")]
        [Tooltip("Whether the room is currently in edit mode")]
        public bool isInEditMode = false;
        
        // Cached components for performance
        private ProBuilderMesh probuilderMesh;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;
        
        /// <summary>
        /// Gets all room component GameObjects (excluding nulls)
        /// </summary>
        public GameObject[] GetRoomComponents()
        {
            var components = new System.Collections.Generic.List<GameObject>();
            
            if (floor != null) components.Add(floor);
            if (ceiling != null) components.Add(ceiling);
            if (northWall != null) components.Add(northWall);
            if (eastWall != null) components.Add(eastWall);
            if (southWall != null) components.Add(southWall);
            if (westWall != null) components.Add(westWall);
            
            // Add legacy corners
            foreach (var corner in corners)
            {
                if (corner != null) components.Add(corner);
            }
            
            // Add dynamic walls from "Walls" parent GameObject
            // Look under "Room Mesh" child for the new hierarchy
            var roomMeshTransform = transform.Find("Room Mesh");
            if (roomMeshTransform != null)
            {
                var wallsParent = roomMeshTransform.Find("Walls");
                if (wallsParent != null)
                {
                    foreach (Transform child in wallsParent)
                    {
                        if (child.gameObject != null)
                        {
                            components.Add(child.gameObject);
                        }
                    }
                }
                
                // Add dynamic corners from "Corners" parent GameObject
                var cornersParent = roomMeshTransform.Find("Corners");
                if (cornersParent != null)
                {
                    foreach (Transform child in cornersParent)
                    {
                        if (child.gameObject != null)
                        {
                            components.Add(child.gameObject);
                        }
                    }
                }
            }
            
            return components.ToArray();
        }
        
        private void Awake()
        {
            CacheComponents();
        }
        
        private void CacheComponents()
        {
            // Cache components for performance
            // Use GetComponentInChildren to support the new hierarchy where Room component is on parent
            probuilderMesh = GetComponentInChildren<ProBuilderMesh>();
            meshRenderer = GetComponentInChildren<MeshRenderer>();
            meshCollider = GetComponentInChildren<MeshCollider>();
        }
        
        /// <summary>
        /// Sets the visibility of all room components
        /// </summary>
        public void SetRoomComponentsVisibility(bool visible)
        {
            foreach (var component in GetRoomComponents())
            {
                if (component != null)
                {
                    component.SetActive(visible);
                }
            }
        }
        
        /// <summary>
        /// Enables or disables the visual components of the cube (ProBuilder, MeshRenderer, MeshCollider)
        /// </summary>
        public void SetCubeVisualComponentsEnabled(bool enabled)
        {
            // Cache components if they're null (in case Awake wasn't called)
            if (probuilderMesh == null || meshRenderer == null || meshCollider == null)
            {
                CacheComponents();
            }
            
            
            if (probuilderMesh != null) probuilderMesh.enabled = enabled;
            if (meshRenderer != null) meshRenderer.enabled = enabled;
            if (meshCollider != null) meshCollider.enabled = enabled;
        }
        
        /// <summary>
        /// Enters edit mode - shows cube visual components and room components for real-time editing
        /// </summary>
        public void EnterEditMode()
        {
            isInEditMode = true;
            SetCubeVisualComponentsEnabled(true);
            SetRoomComponentsVisibility(true);
        }
        
        /// <summary>
        /// Exits edit mode - hides cube visual components, shows room components
        /// </summary>
        public void ExitEditMode()
        {
            isInEditMode = false;
            SetCubeVisualComponentsEnabled(false);
            SetRoomComponentsVisibility(true);
        }
        
        /// <summary>
        /// Updates room components based on the current cube bounds
        /// </summary>
        public void UpdateRoomComponents()
        {
            if (probuilderMesh == null) return;

            Bounds bounds = probuilderMesh.GetComponent<Renderer>().bounds;
            Vector3 center = bounds.center;
            Vector3 size = bounds.size;

            // Get room prefab settings if available
            var roomPrefabSettings = FindFirstObjectByType<RoomPrefabSettings>();
#if UNITY_EDITOR
            if (roomPrefabSettings == null)
            {
                // Try to find it in the project
                var guids = AssetDatabase.FindAssets("t:RoomPrefabSettings");
                if (guids.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    roomPrefabSettings = AssetDatabase.LoadAssetAtPath<RoomPrefabSettings>(path);
                }
            }
#endif

            // Update floor position and size
            if (floor != null)
            {
                float height = 0.1f; // Default height
                float width = 1f; // Default width
                
                if (roomPrefabSettings != null)
                {
                    height = roomPrefabSettings.floorHeight;
                    width = 1f; // Floor spans full room width
                }
                floor.transform.position = new Vector3(center.x, center.y - size.y * 0.5f, center.z);
                // Update floor size if it has ProBuilder mesh
                var floorMesh = floor.GetComponent<ProBuilderMesh>();
                if (floorMesh != null)
                {
                    ResizeProBuilderMesh(floorMesh, new Vector3(size.x * width, height, size.z * width));
                }
            }

            // Update walls
            float wallHeight = size.y; // Use cube height by default
            float wallWidth = 0.2f;
            float wallDepth = 0.2f;
            
            if (roomPrefabSettings != null)
            {
                wallHeight = roomPrefabSettings.wallHeight;
                wallWidth = roomPrefabSettings.wallWidth;
                wallDepth = roomPrefabSettings.wallDepth;
            }

            UpdateWall(northWall, new Vector3(center.x, center.y, center.z + size.z * 0.5f), new Vector3(size.x, wallHeight, wallDepth));
            UpdateWall(southWall, new Vector3(center.x, center.y, center.z - size.z * 0.5f), new Vector3(size.x, wallHeight, wallDepth));
            UpdateWall(eastWall, new Vector3(center.x + size.x * 0.5f, center.y, center.z), new Vector3(wallDepth, wallHeight, size.z));
            UpdateWall(westWall, new Vector3(center.x - size.x * 0.5f, center.y, center.z), new Vector3(wallDepth, wallHeight, size.z));

            // Update corner positions and sizes
            if (corners != null && corners.Length >= 4 && roomPrefabSettings != null)
            {
                // Corner height is now calculated as tallest wall + offset
                // For legacy corners, approximate using wall height + offset
                float cornerHeight = wallHeight + roomPrefabSettings.cornerHeightOffset;
                float cornerWidth = roomPrefabSettings.cornerWidth;
                float cornerDepth = roomPrefabSettings.cornerDepth;
                
                // North-East corner
                if (corners[0] != null)
                {
                    UpdateCorner(corners[0], new Vector3(center.x + size.x * 0.5f, center.y, center.z + size.z * 0.5f), new Vector3(cornerWidth, cornerHeight, cornerDepth));
                }
                
                // South-East corner
                if (corners[1] != null)
                {
                    UpdateCorner(corners[1], new Vector3(center.x + size.x * 0.5f, center.y, center.z - size.z * 0.5f), new Vector3(cornerWidth, cornerHeight, cornerDepth));
                }
                
                // South-West corner
                if (corners[2] != null)
                {
                    UpdateCorner(corners[2], new Vector3(center.x - size.x * 0.5f, center.y, center.z - size.z * 0.5f), new Vector3(cornerWidth, cornerHeight, cornerDepth));
                }
                
                // North-West corner
                if (corners[3] != null)
                {
                    UpdateCorner(corners[3], new Vector3(center.x - size.x * 0.5f, center.y, center.z + size.z * 0.5f), new Vector3(cornerWidth, cornerHeight, cornerDepth));
                }
            }

            // Update ceiling
            if (ceiling != null)
            {
                float height = 0.1f; // Default height
                float width = 1f; // Default width
                
                if (roomPrefabSettings != null)
                {
                    height = roomPrefabSettings.ceilingHeight;
                    width = 1f; // Ceiling spans full room width
                }
                ceiling.transform.position = new Vector3(center.x, center.y + size.y * 0.5f, center.z);
                // Update ceiling size if it has ProBuilder mesh
                var ceilingMesh = ceiling.GetComponent<ProBuilderMesh>();
                if (ceilingMesh != null)
                {
                    ResizeProBuilderMesh(ceilingMesh, new Vector3(size.x * width, height, size.z * width));
                }
            }
        }
        
        private void UpdateWall(GameObject wall, Vector3 position, Vector3 size)
        {
            if (wall == null) return;

            wall.transform.position = position;
            var wallMesh = wall.GetComponent<ProBuilderMesh>();
            if (wallMesh != null)
            {
                ResizeProBuilderMesh(wallMesh, size);
            }
        }
        
        private void UpdateCorner(GameObject corner, Vector3 position, Vector3 size)
        {
            if (corner == null) return;

            corner.transform.position = position;
            var cornerMesh = corner.GetComponent<ProBuilderMesh>();
            if (cornerMesh != null)
            {
                ResizeProBuilderMesh(cornerMesh, size);
            }
        }
        
        
        private void ResizeProBuilderMesh(ProBuilderMesh mesh, Vector3 targetSize)
        {
            // Work in local space using writable Vertex[]
            var vertices = mesh.GetVertices();
            Vector3 half = targetSize * 0.5f;

            for (int i = 0; i < mesh.vertexCount; i++)
            {
                var v = vertices[i];
                Vector3 p = v.position; // local-space position

                if (Mathf.Abs(p.x) > 0.0001f)
                    p.x = Mathf.Sign(p.x) * half.x;
                if (Mathf.Abs(p.y) > 0.0001f)
                    p.y = Mathf.Sign(p.y) * half.y;
                if (Mathf.Abs(p.z) > 0.0001f)
                    p.z = Mathf.Sign(p.z) * half.z;

                v.position = p;
                vertices[i] = v;
            }

            mesh.SetVertices(vertices);
            mesh.ToMesh();
            mesh.Refresh(UnityEngine.ProBuilder.RefreshMask.All);
        }

    }
}
