using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using DungeonsForProBuilder;

namespace DungeonsForProBuilderEditor
{
    /// <summary>
    /// Main editor window for the Dungeons for ProBuilder tool
    /// </summary>
    public class DungeonEditorWindow : EditorWindow
    {
        private const string SettingsGuidEditorPrefsKey = "DungeonsForPB.RoomPrefabSettingsGUID";
        private VisualElement rootElement;
        private VisualElement statusElement;

        // New UI elements per mockup
        private Button buildRoomButton;
        private Button resetRoomButton;
        private Button showAllRoomsButton;
        private Button hideAllRoomsButton;
        private Button isolateRoomButton;
        private VisualElement visibilitySectionContainer;
        private VisualElement selectionWarningHost;
        private VisualElement settingsWarningHost;
        private HelpBox selectionWarningBox;
        private HelpBox settingsWarningBox;
        private ObjectField settingsField;
        private Button createSettingsButton;
        private Foldout settingsFoldout;
        private VisualElement settingsInspectorContainer;
        private Label statusLabel;
        
        // Door editor elements
        private Button buildDoorButton;
        private Button resetDoorButton;
        private VisualElement actionButtonsContainer;
        private VisualElement doorActionButtonsContainer;
        private VisualElement doorWarningHost;
        private HelpBox doorWarningBox;

        // data
        private RoomPrefabSettings currentSettings;
        private Editor settingsEditor;
        
        [MenuItem("Window/Dungeons for PB/Dungeon Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<DungeonEditorWindow>();
            window.titleContent = new GUIContent("Dungeon Editor");
            window.minSize = new Vector2(420, 420);
        }
        
        private void CreateGUI()
        {
            // Load the UXML template
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Packages/com.newfangled.dungeons-for-probuilder/Editor/DungeonEditorWindow.uxml");
            
            if (visualTree == null)
            {
                // Fallback if UXML is not found
                CreateFallbackGUI();
                return;
            }
            
            rootElement = visualTree.CloneTree();
            
            // Load and apply the USS stylesheet
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Packages/com.newfangled.dungeons-for-probuilder/Editor/DungeonEditorWindow.uss");
            
            if (styleSheet != null)
            {
                rootElement.styleSheets.Add(styleSheet);
            }
            
            rootVisualElement.Add(rootElement);
            
            // Get references to UI elements
            statusElement = rootElement.Q<VisualElement>("status");
            buildRoomButton = rootElement.Q<Button>("build-room");
            resetRoomButton = rootElement.Q<Button>("reset-room");
            showAllRoomsButton = rootElement.Q<Button>("show-all-rooms");
            hideAllRoomsButton = rootElement.Q<Button>("hide-all-rooms");
            isolateRoomButton = rootElement.Q<Button>("isolate-room");
            visibilitySectionContainer = rootElement.Q<VisualElement>("visibility-section");
            selectionWarningHost = rootElement.Q<VisualElement>("selection-warning");
            settingsWarningHost = rootElement.Q<VisualElement>("settings-warning");
            settingsField = rootElement.Q<ObjectField>("settings-field");
            createSettingsButton = rootElement.Q<Button>("create-settings");
            settingsFoldout = rootElement.Q<Foldout>("settings-foldout");
            settingsInspectorContainer = rootElement.Q<VisualElement>("settings-inspector-container");
            statusLabel = rootElement.Q<Label>("status-text");
            
            // Door editor elements
            buildDoorButton = rootElement.Q<Button>("build-door");
            resetDoorButton = rootElement.Q<Button>("reset-door");
            actionButtonsContainer = rootElement.Q<VisualElement>("action-buttons");
            doorActionButtonsContainer = rootElement.Q<VisualElement>("door-action-buttons");
            doorWarningHost = rootElement.Q<VisualElement>("door-warning");
            
            // Set up event handlers
            SetupEventHandlers();
            
            // Initialize the window
            InitializeWindow();
        }
        
        private void CreateFallbackGUI()
        {
            // Create a simple fallback GUI if UXML is not available
            rootElement = new VisualElement();
            rootElement.style.flexGrow = 1;
            rootElement.style.paddingTop = 10;
            rootElement.style.paddingBottom = 10;
            rootElement.style.paddingLeft = 10;
            rootElement.style.paddingRight = 10;
            
            var titleLabel = new Label("Dungeon Editor");
            titleLabel.style.fontSize = 18;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 10;
            
            var infoLabel = new Label("This is a skeleton plugin for dungeon editing tools using ProBuilder.");
            infoLabel.style.marginBottom = 10;
            
            rootElement.Add(titleLabel);
            rootElement.Add(infoLabel);
            
            rootVisualElement.Add(rootElement);
        }
        
        private void SetupEventHandlers()
        {
            if (buildRoomButton != null)
                buildRoomButton.clicked += OnBuildRoomClicked;

            if (resetRoomButton != null)
                resetRoomButton.clicked += OnResetRoomClicked;

            if (showAllRoomsButton != null)
                showAllRoomsButton.clicked += OnShowAllRoomsClicked;

            if (hideAllRoomsButton != null)
                hideAllRoomsButton.clicked += OnHideAllRoomsClicked;

            if (isolateRoomButton != null)
                isolateRoomButton.clicked += OnIsolateRoomClicked;

            if (settingsField != null)
            {
                settingsField.objectType = typeof(RoomPrefabSettings);
                settingsField.RegisterValueChangedCallback(evt => OnSettingsChanged(evt.newValue as RoomPrefabSettings));
            }

            if (createSettingsButton != null)
                createSettingsButton.clicked += CreateSettingsAsset;
                
            // Door editor event handlers
            if (buildDoorButton != null)
                buildDoorButton.clicked += OnBuildDoorClicked;
                
            if (resetDoorButton != null)
                resetDoorButton.clicked += OnResetDoorClicked;
        }
        
        private void InitializeWindow()
        {
            // Initialize any default values or state
            UpdateStatus("You need to select a ProBuilder cube to build a room");
            RefreshSelectionWarning();
            LoadPersistedSettings();
            RefreshSettingsUI();
            RefreshButtonStates();
            CheckExperimentalFeatures();
        }

        private void LoadPersistedSettings()
        {
            var savedGuid = EditorPrefs.GetString(SettingsGuidEditorPrefsKey, string.Empty);
            if (!string.IsNullOrEmpty(savedGuid))
            {
                var path = AssetDatabase.GUIDToAssetPath(savedGuid);
                if (!string.IsNullOrEmpty(path))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<RoomPrefabSettings>(path);
                    if (asset != null)
                    {
                        currentSettings = asset;
                        if (settingsField != null) settingsField.value = asset;
                    }
                }
            }
        }
        
        private void OnBuildRoomClicked()
        {
            // Get the selected ProBuilder mesh
            var selectedObject = Selection.activeGameObject;
            if (selectedObject == null)
            {
                UpdateStatus("No object selected. Please select a ProBuilder cube.");
                return;
            }
            
            var probuilderMesh = selectedObject.GetComponent<ProBuilderMesh>();
            if (probuilderMesh == null)
            {
                UpdateStatus("Selected object is not a ProBuilder mesh. Please select a ProBuilder cube.");
                return;
            }
            
            // Register undo operation
            Undo.RegisterCompleteObjectUndo(selectedObject, "Build Room");
            
            var roomParent = CreateRoomFromCube(probuilderMesh);
            
            // After room is built, automatically build doors for any door meshes inside the room
            if (roomParent != null)
            {
                AutoBuildDoorsInRoom(roomParent);
                
                // Mark the object as dirty for undo
                EditorUtility.SetDirty(roomParent);
            }
            
            UpdateStatus("Room created successfully");
            RefreshButtonStates();
        }
        
        private void AutoBuildDoorsInRoom(GameObject roomObject)
        {
            var room = roomObject.GetComponent<Room>();
            if (room == null) return;
            
            // Find ALL doors in the scene (including disabled ones)
            var allDoors = FindObjectsByType<DoorOperation>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            if (allDoors.Length == 0) return;
            
            // Build doors for each door that overlaps with this room's walls
            foreach (var door in allDoors)
            {
                if (door.originalDoorMesh == null) continue;
                
                var doorMesh = door.originalDoorMesh.GetComponent<ProBuilderMesh>();
                if (doorMesh == null) continue;
                
                // Find overlapping walls in THIS room
                var overlappingWalls = FindOverlappingWallsInRoom(doorMesh, room);
                
                if (overlappingWalls.Length > 0)
                {
                    // This door overlaps with walls in this room
                    // Apply the door operation to this room's walls
                    
                    // Create door cuts in the walls
                    foreach (var wall in overlappingWalls)
                    {
                        var wallMesh = wall.GetComponent<ProBuilderMesh>();
                        if (wallMesh == null) continue;
                        
                        // Perform boolean subtraction
                        wall.SetActive(true);
                        door.originalDoorMesh.SetActive(true);
                        
                        Selection.activeGameObject = wall;
                        
                        var newWall = ProBuilderBooleanUtility.PerformBooleanSubtraction(wallMesh, doorMesh);
                        if (newWall != null)
                        {
                            newWall.transform.SetParent(room.transform);
                            newWall.name = wall.name;
                            
                            var originalWallComponent = wall.GetComponent<RoomWall>();
                            if (originalWallComponent != null)
                            {
                                var newWallComponent = newWall.AddComponent<RoomWall>();
                                newWallComponent.direction = originalWallComponent.direction;
                            }
                            
                            // Store reference in door operation
                            var doorWallsList = new System.Collections.Generic.List<GameObject>(door.originalWalls ?? new GameObject[0]);
                            var doorNewWallsList = new System.Collections.Generic.List<GameObject>(door.newWallMeshes ?? new GameObject[0]);
                            
                            doorWallsList.Add(wall);
                            doorNewWallsList.Add(newWall);
                            
                            door.originalWalls = doorWallsList.ToArray();
                            door.newWallMeshes = doorNewWallsList.ToArray();
                            
                            Undo.RegisterCreatedObjectUndo(newWall, "Auto Build Door");
                            wall.SetActive(false);
                        }
                    }
                    
                    // Disable door renderer
                    var doorRenderer = door.originalDoorMesh.GetComponent<MeshRenderer>();
                    if (doorRenderer != null)
                    {
                        doorRenderer.enabled = false;
                    }
                }
            }
        }
        
        private GameObject[] FindOverlappingWallsInRoom(ProBuilderMesh doorMesh, Room room)
        {
            var doorBounds = doorMesh.GetComponent<Renderer>().bounds;
            var overlappingWalls = new System.Collections.Generic.List<GameObject>();
            
            var roomComponents = room.GetRoomComponents();
            
            foreach (var component in roomComponents)
            {
                if (component != null)
                {
                    var wallComponent = component.GetComponent<RoomWall>();
                    if (wallComponent != null)
                    {
                        var renderer = component.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            var wallBounds = renderer.bounds;
                            bool intersects = wallBounds.Intersects(doorBounds);
                            
                            if (intersects)
                            {
                                overlappingWalls.Add(component);
                            }
                        }
                    }
                }
            }
            
            return overlappingWalls.ToArray();
        }
        
        private GameObject CreateRoomFromCube(ProBuilderMesh sourceCube)
        {
            // Get the bounds of the source cube
            Bounds bounds = sourceCube.GetComponent<Renderer>().bounds;
            Vector3 center = bounds.center;
            Vector3 size = bounds.size;
            
            // Clean up any existing room components and parent GameObjects first
            // Check if the parent has a Room component (new hierarchy: Room > Room Mesh)
            var existingRoom = sourceCube.transform.parent != null ? 
                sourceCube.transform.parent.GetComponent<Room>() : null;
            
            if (existingRoom != null)
            {
                GameObject existingRoomParent = existingRoom.gameObject;
                
                // Destroy existing room components
                foreach (var component in existingRoom.GetRoomComponents())
                {
                    if (component != null)
                    {
                        Undo.DestroyObjectImmediate(component);
                    }
                }
                
                // Destroy existing parent organization GameObjects under sourceCube (Room Mesh)
                var existingWallsParent = sourceCube.transform.Find("Walls");
                if (existingWallsParent != null)
                {
                    Undo.DestroyObjectImmediate(existingWallsParent.gameObject);
                }
                
                var existingCornersParent = sourceCube.transform.Find("Corners");
                if (existingCornersParent != null)
                {
                    Undo.DestroyObjectImmediate(existingCornersParent.gameObject);
                }
                
                // Remove existing Room component
                Undo.DestroyObjectImmediate(existingRoom);
                
                // Destroy the room parent container
                Undo.DestroyObjectImmediate(existingRoomParent);
            }
            
            // Create the parent GameObject structure
            GameObject roomParent;
            
            // Check if we're using a prefab container
            if (currentSettings != null && currentSettings.roomPrefab != null)
            {
                // Instantiate the room prefab as the parent
                roomParent = (GameObject)PrefabUtility.InstantiatePrefab(currentSettings.roomPrefab);
                roomParent.name = "Room";
                roomParent.transform.position = sourceCube.transform.position;
                roomParent.transform.rotation = sourceCube.transform.rotation;
                Undo.RegisterCreatedObjectUndo(roomParent, "Build Room");
            }
            else
            {
                // Create a new parent GameObject
                roomParent = new GameObject("Room");
                roomParent.transform.position = sourceCube.transform.position;
                roomParent.transform.rotation = sourceCube.transform.rotation;
                Undo.RegisterCreatedObjectUndo(roomParent, "Build Room");
            }
            
            // Add Room component to the parent
            var roomComponent = roomParent.AddComponent<Room>();
            Undo.RegisterCreatedObjectUndo(roomComponent, "Build Room");
            
            // Make the source cube a child and rename it to "Room Mesh"
            Undo.SetTransformParent(sourceCube.transform, roomParent.transform, "Build Room");
            sourceCube.transform.localPosition = Vector3.zero;
            sourceCube.transform.localRotation = Quaternion.identity;
            sourceCube.gameObject.name = "Room Mesh";
            
            // Create floor (only if enabled and prefab provided)
            GameObject floor = null;
            if (currentSettings != null && currentSettings.enableFloor)
            {
                floor = CreateFloor(sourceCube.gameObject, center, size);
            }
            roomComponent.floor = floor;
            
            // Analyze the mesh geometry to detect corners and walls
            var (detectedCorners, detectedWalls) = AnalyzeRoomGeometry(sourceCube, angleThreshold: 165f);
            
            // Always use dynamic creation - the complex implementation handles all room shapes including simple cubes
            CreateDynamicWallsAndCorners(sourceCube.gameObject, roomComponent, detectedCorners, detectedWalls, size.y);
            
            // Create ceiling (only if enabled and prefab provided)
            GameObject ceiling = null;
            if (currentSettings != null && currentSettings.enableCeiling)
            {
                ceiling = CreateCeiling(sourceCube.gameObject, center, size);
            }
            roomComponent.ceiling = ceiling;

            // Disable visual components on the original cube
            roomComponent.SetCubeVisualComponentsEnabled(false);
            
            // Select the room parent
            Selection.activeGameObject = roomParent;
            
            return roomParent;
        }
        
        /// <summary>
        /// Creates walls and corners based on detected geometry for complex room shapes
        /// </summary>
        private void CreateDynamicWallsAndCorners(GameObject parent, Room roomComponent,
            System.Collections.Generic.List<DetectedCorner> corners,
            System.Collections.Generic.List<DetectedWall> walls,
            float roomHeight)
        {
            if (currentSettings == null) return;
            
            // Get the room's world position and local bounds for proper positioning
            Vector3 roomWorldPosition = parent.transform.position;
            
            // Get the mesh's local bounds to determine the actual bottom position
            var meshFilter = parent.GetComponent<MeshFilter>();
            float localBottomY = 0f;
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Bounds localBounds = meshFilter.sharedMesh.bounds;
                localBottomY = localBounds.center.y - localBounds.size.y * 0.5f;
            }
            else
            {
                // Fallback: assume centered at 0
                localBottomY = -roomHeight * 0.5f;
            }
            
            // Clear any existing room components by destroying them
            // (We'll create new ones below)
            
            // Clean up existing parent organization GameObjects
            var existingWallsParent = parent.transform.Find("Walls");
            if (existingWallsParent != null)
            {
                Undo.DestroyObjectImmediate(existingWallsParent.gameObject);
            }
            
            var existingCornersParent = parent.transform.Find("Corners");
            if (existingCornersParent != null)
            {
                Undo.DestroyObjectImmediate(existingCornersParent.gameObject);
            }
            
            // Create parent GameObjects for organization
            GameObject wallsParent = new GameObject("Walls");
            wallsParent.transform.SetParent(parent.transform);
            wallsParent.transform.localPosition = Vector3.zero;
            wallsParent.transform.localRotation = Quaternion.identity;
            Undo.RegisterCreatedObjectUndo(wallsParent, "Build Room");
            
            GameObject cornersParent = new GameObject("Corners");
            cornersParent.transform.SetParent(parent.transform);
            cornersParent.transform.localPosition = Vector3.zero;
            cornersParent.transform.localRotation = Quaternion.identity;
            Undo.RegisterCreatedObjectUndo(cornersParent, "Build Room");
            
            // Determine which walls are back based on perimeter detection and face normals
            var backWalls = DetermineBackWallsFromPerimeter(walls);
            var backCorners = DetermineBackCornersFromPerimeter(corners, backWalls);
            
            // Create corner objects
            int cornerIndex = 0;
            foreach (var corner in corners)
            {
                GameObject cornerPrefab = GetCornerPrefabForDynamicCorner(corner);
                if (cornerPrefab != null)
                {
                    CornerDirection cornerDirection = DetermineCornerDirection(corner.normal);
                    
                    // Check if this corner is back in its direction and use override settings if enabled
                    bool isBack = backCorners.Contains(corner);
                    float cornerHeight, cornerWidth, cornerDepth;
                    
                    if (isBack)
                    {
                        // Use back override settings
                        cornerHeight = GetBackCornerHeight(roomHeight, cornerDirection);
                        cornerWidth = GetBackCornerWidth(cornerDirection);
                        cornerDepth = GetBackCornerDepth(cornerDirection);
                    }
                    else
                    {
                        // Use regular settings
                        cornerHeight = GetDynamicCornerHeight(roomHeight, cornerDirection);
                        cornerWidth = GetDynamicCornerWidth(cornerDirection);
                        cornerDepth = GetDynamicCornerDepth(cornerDirection);
                    }
                    float cornerBoundsY = 0f; // Default to 0 (floor position)
                    
                    // Convert detected world position to local position relative to parent
                    // Use the corner's actual world position (already accounts for rotation)
                    Vector3 cornerPosition = parent.transform.InverseTransformPoint(corner.position);
                    
                    var cornerObj = CreateCorner(cornersParent, $"Corner {cornerIndex}",
                        cornerPosition,
                        new Vector3(cornerWidth, cornerHeight, cornerDepth),
                        cornerPrefab, localBottomY, cornerBoundsY);
                    
                    if (cornerObj != null)
                    {
                        // Set corner component direction based on angle
                        var cornerComp = cornerObj.GetComponent<RoomCorner>();
                        if (cornerComp != null)
                        {
                            cornerComp.direction = cornerDirection;
                        }
                    }
                }
                cornerIndex++;
            }
            
            // Create wall objects
            int wallIndex = 0;
            foreach (var wall in walls)
            {
                GameObject wallPrefab = GetWallPrefabForDynamicWall(wall);
                if (wallPrefab != null)
                {
                    // Use the stored face normal from mesh analysis (already in world space)
                    WallDirection wallDirection = DetermineWallDirection(wall.faceNormal);
                    
                    
                    // Check if this wall is back in its direction and use override settings if enabled
                    bool isBack = backWalls.Contains(wall);
                    float wallHeight, wallWidth, wallDepth;
                    
                    if (isBack)
                    {
                        // Use back override settings
                        wallHeight = GetBackWallHeight(roomHeight, wallDirection);
                        wallWidth = GetBackWallWidth(wallDirection);
                        wallDepth = GetBackWallDepth(wallDirection);
                    }
                    else
                    {
                        // Use regular settings
                        wallHeight = GetDynamicWallHeight(roomHeight, wallDirection);
                        wallWidth = GetDynamicWallWidth(wallDirection);
                        wallDepth = GetDynamicWallDepth(wallDirection);
                    }
                    float wallBoundsY = 0f; // Default to 0 (floor position)
                    
                    // Convert detected world position to local position relative to parent
                    // Use the wall's actual world center (already accounts for rotation)
                    Vector3 wallCenter = parent.transform.InverseTransformPoint(wall.center);
                    
                    // Calculate wall rotation to align with the wall direction (in world space)
                    Vector3 crossProduct = Vector3.Cross(wall.direction, Vector3.up);
                    Quaternion worldWallRotation = crossProduct.magnitude > 0.001f ? 
                        Quaternion.LookRotation(crossProduct) : Quaternion.identity;
                    
                    // Convert world rotation to local rotation relative to parent
                    Quaternion wallRotation = Quaternion.Inverse(parent.transform.rotation) * worldWallRotation;
                    
                    var wallObj = CreateDynamicWall(wallsParent, $"Wall {wallIndex}",
                        wallCenter,
                        new Vector3(wall.length, wallHeight, wallDepth),
                        wallPrefab, localBottomY, wallBoundsY, wallRotation, wall.faceNormal);
                    
                    if (wallObj != null)
                    {
                        // Wall created successfully
                    }
                }
                wallIndex++;
            }
        }
        
        /// <summary>
        /// Creates a dynamic wall with custom rotation
        /// </summary>
        private GameObject CreateDynamicWall(GameObject parent, string name, Vector3 position, Vector3 size,
            GameObject prefab, float localBottomY, float boundsY, Quaternion rotation, Vector3 faceNormal)
        {
            if (prefab == null) return null;
            
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.SetParent(parent.transform);
            Undo.RegisterCreatedObjectUndo(go, "Build Room");
            
            // Calculate Y position - bottom of wall aligns with the mesh's local bottom
            float prefabHeight = size.y;
            // Center the wall vertically so its bottom is at localBottomY
            float yPos = localBottomY + prefabHeight * 0.5f;
            
            // Set local position (position is already in local space for dynamic walls)
            go.transform.localPosition = new Vector3(position.x, yPos, position.z);
            go.transform.localRotation = rotation;
            
            var probuilderMesh = go.GetComponent<ProBuilderMesh>();
            if (probuilderMesh != null)
            {
                ResizeProBuilderMesh(probuilderMesh, size);
            }
            
            // Add wall component with direction
            var wallComponent = go.GetComponent<RoomWall>();
            if (wallComponent == null)
            {
                wallComponent = go.AddComponent<RoomWall>();
                Undo.RegisterCreatedObjectUndo(wallComponent, "Build Room");
            }
            
            // Use the stored face normal from mesh analysis (already in world space)
            wallComponent.direction = DetermineWallDirection(faceNormal);
            
            // Add Box Collider for raycast detection
            if (go.GetComponent<BoxCollider>() == null)
            {
                var boxCollider = go.AddComponent<BoxCollider>();
                Undo.RegisterCreatedObjectUndo(boxCollider, "Build Room");
            }
            
            return go;
        }
        
        /// <summary>
        /// Determines wall direction based on face normal with 45-degree thresholds
        /// </summary>
        private WallDirection DetermineWallDirection(Vector3 faceNormal)
        {
            // Project to XZ plane and normalize
            Vector3 norm2D = new Vector3(faceNormal.x, 0, faceNormal.z).normalized;
            
            // Calculate angles for each cardinal direction
            float angleNorth = Vector3.Angle(norm2D, Vector3.forward); // 0 degrees = North
            float angleEast = Vector3.Angle(norm2D, Vector3.right);    // 90 degrees = East
            float angleSouth = Vector3.Angle(norm2D, -Vector3.forward); // 180 degrees = South
            float angleWest = Vector3.Angle(norm2D, -Vector3.right);   // 270 degrees = West
            
            // Find the closest cardinal direction (smallest angle)
            float minAngle = Mathf.Min(angleNorth, angleEast, angleSouth, angleWest);
            
            if (minAngle <= 45f) // Within 45 degrees of a cardinal direction
            {
                if (minAngle == angleNorth) return WallDirection.North;
                if (minAngle == angleEast) return WallDirection.East;
                if (minAngle == angleSouth) return WallDirection.South;
                if (minAngle == angleWest) return WallDirection.West;
            }
            
            // Default to North if no clear direction
            return WallDirection.North;
        }
        
        /// <summary>
        /// Determines corner direction based on the corner's normal vector (pointing outward from room)
        /// </summary>
        private CornerDirection DetermineCornerDirection(Vector3 normal)
        {
            // Project to XZ plane and normalize
            Vector3 norm2D = new Vector3(normal.x, 0, normal.z).normalized;
            
            // Determine quadrant based on the outward-pointing normal
            // NorthEast: normal points +X and +Z
            // SouthEast: normal points +X and -Z  
            // SouthWest: normal points -X and -Z
            // NorthWest: normal points -X and +Z
            if (norm2D.x > 0 && norm2D.z > 0)
                return CornerDirection.NorthEast;
            else if (norm2D.x > 0 && norm2D.z < 0)
                return CornerDirection.SouthEast;
            else if (norm2D.x < 0 && norm2D.z < 0)
                return CornerDirection.SouthWest;
            else
                return CornerDirection.NorthWest;
        }
        
        // Helper methods to get prefabs and dimensions for dynamic components
        private GameObject GetWallPrefabForDynamicWall(DetectedWall wall)
        {
            if (currentSettings == null) return null;
            return currentSettings.wallPrefab;
        }
        
        private GameObject GetCornerPrefabForDynamicCorner(DetectedCorner corner)
        {
            if (currentSettings == null) return null;
            return currentSettings.cornerPrefab;
        }
        
        private float GetDynamicWallHeight(float roomHeight, WallDirection direction)
        {
            if (currentSettings == null) return roomHeight;
            return currentSettings.wallHeight;
        }
        
        private float GetDynamicWallWidth(WallDirection direction)
        {
            if (currentSettings == null) return 0.2f;
            return currentSettings.wallWidth;
        }
        
        private float GetDynamicWallDepth(WallDirection direction)
        {
            if (currentSettings == null) return 0.2f;
            return currentSettings.wallDepth;
        }
        
        
        private float GetDynamicCornerHeight(float roomHeight, CornerDirection direction)
        {
            if (currentSettings == null) return roomHeight;
            return currentSettings.cornerHeight;
        }
        
        private float GetDynamicCornerWidth(CornerDirection direction)
        {
            if (currentSettings == null) return 0.2f;
            return currentSettings.cornerWidth;
        }
        
        private float GetDynamicCornerDepth(CornerDirection direction)
        {
            if (currentSettings == null) return 0.2f;
            return currentSettings.cornerDepth;
        }
        
        
        private GameObject CreateFloor(GameObject parent, Vector3 center, Vector3 size)
        {
            if (currentSettings == null) return null;
            
            // Create a new GameObject for the floor
            var go = new GameObject("Floor");
            go.transform.SetParent(parent.transform);
            Undo.RegisterCreatedObjectUndo(go, "Build Room");
            
            // Copy the ProBuilder mesh from the parent
            var originalMesh = parent.GetComponent<ProBuilderMesh>();
            if (originalMesh == null) return null;
            
            var floorMesh = go.AddComponent<ProBuilderMesh>();
            
            // Copy mesh data properly
            floorMesh.Clear();
            
            // Copy vertices
            var originalVertices = originalMesh.GetVertices();
            var newVertices = new UnityEngine.ProBuilder.Vertex[originalVertices.Length];
            for (int i = 0; i < originalVertices.Length; i++)
            {
                newVertices[i] = originalVertices[i];
            }
            
            // Modify vertices to create floor (move top vertices down to create thickness)
            float floorHeight = currentSettings.floorHeight;
            float cubeBottom = center.y - size.y * 0.5f;
            
            for (int i = 0; i < newVertices.Length; i++)
            {
                var vertex = newVertices[i];
                Vector3 worldPos = parent.transform.TransformPoint(vertex.position);
                
                // If this vertex is at the top of the original shape, move it down to create floor thickness
                if (Mathf.Abs(worldPos.y - (center.y + size.y * 0.5f)) < 0.01f)
                {
                    // This is a top vertex, move it down to create floor thickness
                    worldPos.y = cubeBottom + floorHeight;
                    vertex.position = parent.transform.InverseTransformPoint(worldPos);
                    newVertices[i] = vertex;
                }
            }
            
            // Set vertices and copy faces
            floorMesh.SetVertices(newVertices);
            
            // Copy faces
            var originalFaces = originalMesh.faces;
            var newFaces = new System.Collections.Generic.List<UnityEngine.ProBuilder.Face>();
            foreach (var face in originalFaces)
            {
                newFaces.Add(new UnityEngine.ProBuilder.Face(face));
            }
            floorMesh.faces = newFaces;
            
            // Rebuild the mesh
            floorMesh.ToMesh();
            floorMesh.Refresh(RefreshMask.All);
            
            // Copy materials from original mesh
            var originalRenderer = parent.GetComponent<MeshRenderer>();
            var floorRenderer = go.GetComponent<MeshRenderer>();
            if (originalRenderer != null && floorRenderer != null)
            {
                floorRenderer.sharedMaterials = originalRenderer.sharedMaterials;
            }
            
            // Position the floor at the same Y level as the original cube
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            
            // Add floor component
            var floorComponent = go.AddComponent<RoomFloor>();
            Undo.RegisterCreatedObjectUndo(floorComponent, "Build Room");
            
            // Add Mesh Collider for raycast detection (matches actual floor geometry)
            var meshCollider = go.AddComponent<MeshCollider>();
            meshCollider.convex = false; // Non-convex for accurate floor detection
            Undo.RegisterCreatedObjectUndo(meshCollider, "Build Room");
            
            return go;
        }
        
        
        private GameObject CreateWall(GameObject parent, string name, Vector3 position, Vector3 size, GameObject prefab, float localBottomY, float boundsY)
        {
            if (prefab == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.SetParent(parent.transform);
            Undo.RegisterCreatedObjectUndo(go, "Build Room");
            
            // Calculate Y position - bottom of wall aligns with the mesh's local bottom
            float prefabHeight = size.y;
            // Center the wall vertically so its bottom is at localBottomY
            float yPos = localBottomY + prefabHeight * 0.5f;
            
            // Set local position (position is already in local space for dynamic components)
            go.transform.localPosition = new Vector3(position.x, yPos, position.z);
            
            var probuilderMesh = go.GetComponent<ProBuilderMesh>();
            if (probuilderMesh != null)
            {
                ResizeProBuilderMesh(probuilderMesh, size);
            }
            
            // Add wall component with direction
            var wallComponent = go.GetComponent<RoomWall>();
            if (wallComponent == null)
            {
                wallComponent = go.AddComponent<RoomWall>();
                Undo.RegisterCreatedObjectUndo(wallComponent, "Build Room");
            }
            
            // Determine wall direction based on name
            if (name.Contains("North"))
                wallComponent.direction = WallDirection.North;
            else if (name.Contains("South"))
                wallComponent.direction = WallDirection.South;
            else if (name.Contains("East"))
                wallComponent.direction = WallDirection.East;
            else if (name.Contains("West"))
                wallComponent.direction = WallDirection.West;
            
            // Add Box Collider for raycast detection
            if (go.GetComponent<BoxCollider>() == null)
            {
                var boxCollider = go.AddComponent<BoxCollider>();
                Undo.RegisterCreatedObjectUndo(boxCollider, "Build Room");
            }
            
            return go;
        }
        
        private GameObject CreateCeiling(GameObject parent, Vector3 center, Vector3 size)
        {
            if (currentSettings == null) return null;
            
            // Create a new GameObject for the ceiling
            var go = new GameObject("Ceiling");
            go.transform.SetParent(parent.transform);
            Undo.RegisterCreatedObjectUndo(go, "Build Room");
            
            // Copy the ProBuilder mesh from the parent
            var originalMesh = parent.GetComponent<ProBuilderMesh>();
            if (originalMesh == null) return null;
            
            var ceilingMesh = go.AddComponent<ProBuilderMesh>();
            
            // Copy mesh data properly
            ceilingMesh.Clear();
            
            // Copy vertices
            var originalVertices = originalMesh.GetVertices();
            var newVertices = new UnityEngine.ProBuilder.Vertex[originalVertices.Length];
            for (int i = 0; i < originalVertices.Length; i++)
            {
                newVertices[i] = originalVertices[i];
            }
            
            // Modify vertices to create ceiling (move bottom vertices up to create thickness)
            float ceilingHeight = currentSettings.ceilingHeight;
            float cubeTop = center.y + size.y * 0.5f;
            
            for (int i = 0; i < newVertices.Length; i++)
            {
                var vertex = newVertices[i];
                Vector3 worldPos = parent.transform.TransformPoint(vertex.position);
                
                // If this vertex is at the bottom of the original shape, move it up to create ceiling thickness
                if (Mathf.Abs(worldPos.y - (center.y - size.y * 0.5f)) < 0.01f)
                {
                    // This is a bottom vertex, move it up to create ceiling thickness
                    worldPos.y = cubeTop - ceilingHeight;
                    vertex.position = parent.transform.InverseTransformPoint(worldPos);
                    newVertices[i] = vertex;
                }
            }
            
            // Set vertices and copy faces
            ceilingMesh.SetVertices(newVertices);
            
            // Copy faces
            var originalFaces = originalMesh.faces;
            var newFaces = new System.Collections.Generic.List<UnityEngine.ProBuilder.Face>();
            foreach (var face in originalFaces)
            {
                newFaces.Add(new UnityEngine.ProBuilder.Face(face));
            }
            ceilingMesh.faces = newFaces;
            
            // Rebuild the mesh
            ceilingMesh.ToMesh();
            ceilingMesh.Refresh(RefreshMask.All);
            
            // Copy materials from original mesh
            var originalRenderer = parent.GetComponent<MeshRenderer>();
            var ceilingRenderer = go.GetComponent<MeshRenderer>();
            if (originalRenderer != null && ceilingRenderer != null)
            {
                ceilingRenderer.sharedMaterials = originalRenderer.sharedMaterials;
            }
            
            // Position the ceiling at the same Y level as the original cube
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            
            // Add ceiling component
            var ceilingComponent = go.AddComponent<RoomCeiling>();
            Undo.RegisterCreatedObjectUndo(ceilingComponent, "Build Room");
            
            // Add Mesh Collider (matches actual ceiling geometry)
            var meshCollider = go.AddComponent<MeshCollider>();
            meshCollider.convex = false;
            Undo.RegisterCreatedObjectUndo(meshCollider, "Build Room");
            
            return go;
        }
        
        
        private GameObject CreateCorner(GameObject parent, string name, Vector3 position, Vector3 size, GameObject prefab, float localBottomY, float boundsY)
        {
            if (prefab == null) return null;
            
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.SetParent(parent.transform);
            Undo.RegisterCreatedObjectUndo(go, "Build Room");
            
            // Calculate Y position - bottom of corner aligns with the mesh's local bottom
            float prefabHeight = size.y;
            // Center the corner vertically so its bottom is at localBottomY
            float yPos = localBottomY + prefabHeight * 0.5f;
            
            // Set local position (position is already in local space for dynamic components)
            go.transform.localPosition = new Vector3(position.x, yPos, position.z);
            
            var probuilderMesh = go.GetComponent<ProBuilderMesh>();
            if (probuilderMesh != null)
            {
                ResizeProBuilderMesh(probuilderMesh, size);
            }
            
            // Add corner component with direction
            var cornerComponent = go.GetComponent<RoomCorner>();
            if (cornerComponent == null)
            {
                cornerComponent = go.AddComponent<RoomCorner>();
                Undo.RegisterCreatedObjectUndo(cornerComponent, "Build Room");
            }
            
            // Determine corner direction based on name
            if (name.Contains("NE"))
                cornerComponent.direction = CornerDirection.NorthEast;
            else if (name.Contains("SE"))
                cornerComponent.direction = CornerDirection.SouthEast;
            else if (name.Contains("SW"))
                cornerComponent.direction = CornerDirection.SouthWest;
            else if (name.Contains("NW"))
                cornerComponent.direction = CornerDirection.NorthWest;
            
            // Add Box Collider for raycast detection
            if (go.GetComponent<BoxCollider>() == null)
            {
                var boxCollider = go.AddComponent<BoxCollider>();
                Undo.RegisterCreatedObjectUndo(boxCollider, "Build Room");
            }
            
            return go;
        }
        
        private float GetComponentHeight(SizeMode mode, float customValue, float defaultValue)
        {
            switch (mode)
            {
                case SizeMode.UsePrefabSize:
                    return 1f; // No scaling
                case SizeMode.UseBoundsSize:
                    return defaultValue;
                case SizeMode.CustomSize:
                    return customValue;
                default:
                    return defaultValue;
            }
        }
        
        private float GetComponentWidth(SizeMode mode, float customValue, float defaultValue)
        {
            switch (mode)
            {
                case SizeMode.UsePrefabSize:
                    return 1f; // No scaling
                case SizeMode.UseBoundsSize:
                    return defaultValue;
                case SizeMode.CustomSize:
                    return customValue;
                default:
                    return defaultValue;
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
            mesh.Refresh(RefreshMask.All);
        }
        
        private void UpdateStatus(string message)
        {
            if (statusLabel != null)
            {
                statusLabel.text = message;
            }
        }

        private void RefreshSelectionWarning()
        {
            if (selectionWarningHost == null) return;
            var go = Selection.activeGameObject;
            bool show = go == null || go.GetComponent<ProBuilderMesh>() == null;
            EnsureSelectionHelpBox();
            selectionWarningHost.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void EnsureSelectionHelpBox()
        {
            if (selectionWarningHost != null && selectionWarningBox == null)
            {
                selectionWarningBox = new HelpBox("You need to select a ProBuilder cube to build a room", HelpBoxMessageType.Warning);
                selectionWarningHost.Add(selectionWarningBox);
            }
        }

        private void OnSettingsChanged(RoomPrefabSettings newSettings)
        {
            currentSettings = newSettings;
            if (currentSettings != null)
            {
                var path = AssetDatabase.GetAssetPath(currentSettings);
                if (!string.IsNullOrEmpty(path))
                {
                    var guid = AssetDatabase.AssetPathToGUID(path);
                    if (!string.IsNullOrEmpty(guid))
                    {
                        EditorPrefs.SetString(SettingsGuidEditorPrefsKey, guid);
                    }
                }
            }
            RefreshSettingsUI();
        }
        
        private void OnSettingsPropertyChanged()
        {
            // Refresh the settings UI when properties change
            if (currentSettings != null)
            {
                RefreshSettingsUI();
            }
        }

        private void RefreshSettingsUI()
        {
            if (settingsWarningHost != null)
            {
                if (settingsWarningBox == null)
                {
                    settingsWarningBox = new HelpBox("No room settings defined. Will use ProBuilder cubes as default.", HelpBoxMessageType.Warning);
                    settingsWarningHost.Add(settingsWarningBox);
                }
                bool show = currentSettings == null;
                settingsWarningHost.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (settingsFoldout != null)
            {
                settingsFoldout.SetEnabled(currentSettings != null);
            }

            if (settingsInspectorContainer == null) return;

            settingsInspectorContainer.Clear();
            if (currentSettings != null)
            {
                // Only create a new editor if we don't have one or if the target changed
                if (settingsEditor == null || settingsEditor.target != currentSettings)
                {
                    if (settingsEditor != null)
                    {
                        DestroyImmediate(settingsEditor);
                    }
                    settingsEditor = Editor.CreateEditor(currentSettings);
                }
                
                if (settingsEditor != null)
                {
                    // Use the UXML-based custom editor instead of IMGUI
                    var inspector = settingsEditor.CreateInspectorGUI();
                    settingsInspectorContainer.Add(inspector);
                }
            }

            if (createSettingsButton != null)
            {
                createSettingsButton.style.display = currentSettings == null ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void CreateSettingsAsset()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create Room Prefab Settings",
                "RoomPrefabSettings",
                "asset",
                "Choose a location for the Room Prefab Settings asset.");
            if (string.IsNullOrEmpty(path)) return;

            var asset = ScriptableObject.CreateInstance<RoomPrefabSettings>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            settingsField.value = asset;
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (!string.IsNullOrEmpty(guid))
            {
                EditorPrefs.SetString(SettingsGuidEditorPrefsKey, guid);
            }
        }


        private void OnResetRoomClicked()
        {
            var selectedObject = Selection.activeGameObject;
            if (selectedObject == null)
            {
                UpdateStatus("No object selected. Please select a room to reset.");
                return;
            }

            // Check if selected object has Room component or search up the hierarchy
            var room = selectedObject.GetComponent<Room>();
            GameObject roomParent = selectedObject;
            
            if (room == null)
            {
                // Search up the hierarchy to find the room parent
                Transform parent = selectedObject.transform.parent;
                while (parent != null)
                {
                    room = parent.GetComponent<Room>();
                    if (room != null)
                    {
                        roomParent = parent.gameObject;
                        break;
                    }
                    parent = parent.parent;
                }
            }
            
            if (room == null)
            {
                UpdateStatus("Selected object is not a room. Please select a room to reset.");
                return;
            }

            // Register undo operation
            Undo.RegisterCompleteObjectUndo(roomParent, "Reset Room");

            // Find all doors in the scene and clean up walls associated with this room
            var allDoors = FindObjectsByType<DoorOperation>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var door in allDoors)
            {
                if (door.newWallMeshes != null)
                {
                    foreach (var newWall in door.newWallMeshes)
                    {
                        // Check if this modified wall belongs to the room being reset
                        if (newWall != null && newWall.transform.IsChildOf(roomParent.transform))
                        {
                            Undo.DestroyObjectImmediate(newWall);
                        }
                    }
                }
                
                // Clean up references to walls from this room in the door operation
                if (door.originalWalls != null)
                {
                    var updatedOriginalWalls = new System.Collections.Generic.List<GameObject>();
                    var updatedNewWalls = new System.Collections.Generic.List<GameObject>();
                    
                    for (int i = 0; i < door.originalWalls.Length; i++)
                    {
                        var originalWall = door.originalWalls[i];
                        // Keep walls that don't belong to this room
                        if (originalWall != null && !originalWall.transform.IsChildOf(roomParent.transform))
                        {
                            updatedOriginalWalls.Add(originalWall);
                            if (door.newWallMeshes != null && i < door.newWallMeshes.Length)
                            {
                                updatedNewWalls.Add(door.newWallMeshes[i]);
                            }
                        }
                    }
                    
                    door.originalWalls = updatedOriginalWalls.ToArray();
                    door.newWallMeshes = updatedNewWalls.ToArray();
                }
            }

            // Find the "Room Mesh" child (the ProBuilder mesh)
            Transform roomMeshTransform = roomParent.transform.Find("Room Mesh");
            GameObject roomMesh = roomMeshTransform != null ? roomMeshTransform.gameObject : null;
            
            if (roomMesh == null)
            {
                // Fallback: look for any child with ProBuilderMesh component
                var probuilderMeshInChild = roomParent.GetComponentInChildren<ProBuilderMesh>();
                if (probuilderMeshInChild != null)
                {
                    roomMesh = probuilderMeshInChild.gameObject;
                }
            }
            
            if (roomMesh == null)
            {
                UpdateStatus("Failed to find Room Mesh. Room structure may be corrupted.");
                return;
            }

            // Destroy all room components (Floor, Walls, Corners, Ceiling)
            foreach (var component in room.GetRoomComponents())
            {
                if (component != null)
                {
                    Undo.DestroyObjectImmediate(component);
                }
            }

            // Destroy parent organization GameObjects (Walls and Corners) under Room Mesh
            var wallsParent = roomMesh.transform.Find("Walls");
            if (wallsParent != null)
            {
                Undo.DestroyObjectImmediate(wallsParent.gameObject);
            }
            
            var cornersParent = roomMesh.transform.Find("Corners");
            if (cornersParent != null)
            {
                Undo.DestroyObjectImmediate(cornersParent.gameObject);
            }

            // Remove the Room component
            Undo.DestroyObjectImmediate(room);

            // Move the Room Mesh back to root
            Undo.SetTransformParent(roomMesh.transform, null, "Reset Room");
            
            // Re-enable visual components on the ProBuilder mesh
            var probuilderMesh = roomMesh.GetComponent<ProBuilderMesh>();
            var meshRenderer = roomMesh.GetComponent<MeshRenderer>();
            var meshCollider = roomMesh.GetComponent<MeshCollider>();
            
            if (probuilderMesh != null) probuilderMesh.enabled = true;
            if (meshRenderer != null) meshRenderer.enabled = true;
            if (meshCollider != null) meshCollider.enabled = true;
            
            // Destroy the room parent (whether it's a prefab or just "Room" GameObject)
            Undo.DestroyObjectImmediate(roomParent);
            
            // Select the ProBuilder mesh
            Selection.activeGameObject = roomMesh;
            
            // Mark the object as dirty for undo
            EditorUtility.SetDirty(roomMesh);

            UpdateStatus("Room reset. Original cube is now active.");
            RefreshButtonStates();
        }

        private void OnShowAllRoomsClicked()
        {
            // Use FindObjectsByType with includeInactive to find all rooms, even disabled ones
            var rooms = FindObjectsByType<Room>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            if (rooms.Length == 0)
            {
                UpdateStatus("No rooms found in the scene.");
                return;
            }

            Undo.RegisterCompleteObjectUndo(rooms.Select(r => r.gameObject).ToArray(), "Show All Rooms");
            
            foreach (var room in rooms)
            {
                room.gameObject.SetActive(true);
            }

            UpdateStatus($"Shown {rooms.Length} room(s).");
        }

        private void OnHideAllRoomsClicked()
        {
            var rooms = FindObjectsByType<Room>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            if (rooms.Length == 0)
            {
                UpdateStatus("No rooms found in the scene.");
                return;
            }

            Undo.RegisterCompleteObjectUndo(rooms.Select(r => r.gameObject).ToArray(), "Hide All Rooms");
            
            foreach (var room in rooms)
            {
                room.gameObject.SetActive(false);
            }

            UpdateStatus($"Hidden {rooms.Length} room(s).");
        }

        private void OnIsolateRoomClicked()
        {
            var selectedObject = Selection.activeGameObject;
            if (selectedObject == null)
            {
                UpdateStatus("No object selected. Please select a room to isolate.");
                return;
            }

            var selectedRoom = selectedObject.GetComponent<Room>();
            if (selectedRoom == null)
            {
                UpdateStatus("Selected object is not a room. Please select a room to isolate.");
                return;
            }

            var rooms = FindObjectsByType<Room>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            if (rooms.Length == 0)
            {
                UpdateStatus("No rooms found in the scene.");
                return;
            }

            Undo.RegisterCompleteObjectUndo(rooms.Select(r => r.gameObject).ToArray(), "Isolate Room");
            
            int hiddenCount = 0;
            foreach (var room in rooms)
            {
                if (room == selectedRoom)
                {
                    room.gameObject.SetActive(true);
                }
                else
                {
                    room.gameObject.SetActive(false);
                    hiddenCount++;
                }
            }

            UpdateStatus($"Isolated '{selectedRoom.name}'. Hidden {hiddenCount} other room(s).");
        }

        private void RefreshButtonStates()
        {
            var selectedObject = Selection.activeGameObject;
            bool hasProBuilderCube = selectedObject != null && selectedObject.GetComponent<ProBuilderMesh>() != null;
            bool hasRoom = selectedObject != null && selectedObject.GetComponent<Room>() != null;
            bool hasDoorOperation = selectedObject != null && selectedObject.GetComponent<DoorOperation>() != null;
            
            // Check if ProBuilder mesh is a child of a room (should disable build room)
            bool isProBuilderChildOfRoom = false;
            if (hasProBuilderCube && selectedObject != null)
            {
                // Check if this ProBuilder mesh is a child of a room
                Transform parent = selectedObject.transform.parent;
                while (parent != null)
                {
                    if (parent.GetComponent<Room>() != null)
                    {
                        isProBuilderChildOfRoom = true;
                        break;
                    }
                    parent = parent.parent;
                }
            }
            
            // Check if selected object is a child of a room (should enable reset room)
            bool isChildOfRoom = false;
            if (selectedObject != null && !hasRoom)
            {
                Transform parent = selectedObject.transform.parent;
                while (parent != null)
                {
                    if (parent.GetComponent<Room>() != null)
                    {
                        isChildOfRoom = true;
                        break;
                    }
                    parent = parent.parent;
                }
            }
            
            // If no ProBuilder cube found directly, check children (prefab container case)
            if (!hasProBuilderCube && selectedObject != null)
            {
                var childProBuilder = selectedObject.GetComponentInChildren<ProBuilderMesh>();
                if (childProBuilder != null)
                {
                    hasProBuilderCube = true;
                    
                    // Also check if that child ProBuilder mesh is part of a room
                    Transform parent = childProBuilder.transform.parent;
                    while (parent != null)
                    {
                        if (parent.GetComponent<Room>() != null)
                        {
                            isProBuilderChildOfRoom = true;
                            break;
                        }
                        parent = parent.parent;
                    }
                }
            }
            
            // If no DoorOperation found directly, check children (prefab container case)
            if (!hasDoorOperation && selectedObject != null)
            {
                hasDoorOperation = selectedObject.GetComponentInChildren<DoorOperation>() != null;
            }

            // Build Room button - enabled only if ProBuilder cube is selected, no room exists, not a child of room, and selected object is not itself a child of a room
            if (buildRoomButton != null)
            {
                buildRoomButton.SetEnabled(hasProBuilderCube && !hasRoom && !isProBuilderChildOfRoom && !isChildOfRoom);
            }

            // Reset Room button - enabled if room is selected OR if selected object is a child of a room
            if (resetRoomButton != null)
            {
                resetRoomButton.SetEnabled(hasRoom || isChildOfRoom);
            }
            
            // Build Door button - enabled only if ProBuilder cube is selected and experimental features are enabled
            if (buildDoorButton != null)
            {
                buildDoorButton.SetEnabled(hasProBuilderCube && ProBuilderBooleanUtility.IsExperimentalFeaturesEnabled());
            }
            
            // Reset Door button - enabled only if door operation is selected
            if (resetDoorButton != null)
            {
                resetDoorButton.SetEnabled(hasDoorOperation);
            }
        }
        
        private void CheckExperimentalFeatures()
        {
            bool experimentalEnabled = ProBuilderBooleanUtility.IsExperimentalFeaturesEnabled();
            
            if (doorWarningHost != null)
            {
                doorWarningHost.style.display = experimentalEnabled ? DisplayStyle.None : DisplayStyle.Flex;
                
                if (!experimentalEnabled)
                {
                    if (doorWarningBox == null)
                    {
                        doorWarningBox = new HelpBox("ProBuilder experimental features are disabled. Door functionality requires experimental features to be enabled.", HelpBoxMessageType.Warning);
                        doorWarningHost.Add(doorWarningBox);
                    }
                }
                else
                {
                    doorWarningHost.Clear();
                    doorWarningBox = null;
                }
            }
        }
        
        private void OnBuildDoorClicked()
        {
            var selectedObject = Selection.activeGameObject;
            if (selectedObject == null)
            {
                UpdateStatus("No object selected. Please select a ProBuilder cube to use as a door.");
                return;
            }
            
            var doorMesh = selectedObject.GetComponent<ProBuilderMesh>();
            if (doorMesh == null)
            {
                UpdateStatus("Selected object is not a ProBuilder mesh. Please select a ProBuilder cube to use as a door.");
                return;
            }
            
            // If this door already has a DoorOperation, reset it first
            var existingDoorOp = selectedObject.GetComponent<DoorOperation>();
            if (existingDoorOp != null)
            {
                // Find the Door Mesh child
                Transform doorMeshTransform = existingDoorOp.transform.Find("Door Mesh");
                GameObject existingDoorMesh = doorMeshTransform != null ? doorMeshTransform.gameObject : null;
                
                if (existingDoorMesh != null)
                {
                    ResetDoor(existingDoorOp, existingDoorMesh);
                    
                    // Move the Door Mesh back to root
                    Undo.SetTransformParent(existingDoorMesh.transform, null, "Reset Door");
                    
                    // Destroy the door parent
                    Undo.DestroyObjectImmediate(existingDoorOp.gameObject);
                    
                    // Now the doorMesh we were going to use needs to reference this reset mesh
                    doorMesh = existingDoorMesh.GetComponent<ProBuilderMesh>();
                }
            }
            
            // Find ALL overlapping walls from ALL rooms
            var overlappingWalls = FindAllOverlappingWalls(doorMesh);
            
            if (overlappingWalls.Length == 0)
            {
                UpdateStatus("No overlapping walls found. The door must overlap with room walls.");
                return;
            }
            
            // Register undo operation
            Undo.RegisterCompleteObjectUndo(doorMesh.gameObject, "Build Door");
            foreach (var wall in overlappingWalls)
            {
                Undo.RegisterCompleteObjectUndo(wall, "Build Door");
            }
            
            // Determine door name based on overlapping wall directions
            string doorName = "Door";
            if (overlappingWalls.Length > 0)
            {
                var firstWallComponent = overlappingWalls[0].GetComponent<RoomWall>();
                if (firstWallComponent != null)
                {
                    doorName = firstWallComponent.direction.ToString() + " Door";
                }
            }
            
            // Get or create "Doors" GameObject at root level
            GameObject doorsContainer = GameObject.Find("Doors");
            if (doorsContainer == null)
            {
                doorsContainer = new GameObject("Doors");
                Undo.RegisterCreatedObjectUndo(doorsContainer, "Build Door");
            }
            
            // Create the parent GameObject structure
            GameObject doorParent;
            
            // Check if we're using a prefab container
            if (currentSettings != null && currentSettings.doorPrefab != null)
            {
                // Instantiate the door prefab as the parent
                doorParent = (GameObject)PrefabUtility.InstantiatePrefab(currentSettings.doorPrefab);
                doorParent.name = doorName;
                doorParent.transform.position = doorMesh.transform.position;
                doorParent.transform.rotation = doorMesh.transform.rotation;
                doorParent.transform.SetParent(doorsContainer.transform);
                Undo.RegisterCreatedObjectUndo(doorParent, "Build Door");
            }
            else
            {
                // Create a new parent GameObject
                doorParent = new GameObject(doorName);
                doorParent.transform.position = doorMesh.transform.position;
                doorParent.transform.rotation = doorMesh.transform.rotation;
                doorParent.transform.SetParent(doorsContainer.transform);
                Undo.RegisterCreatedObjectUndo(doorParent, "Build Door");
            }
            
            // Add DoorOperation component to the parent
            var doorOperation = doorParent.AddComponent<DoorOperation>();
            Undo.RegisterCreatedObjectUndo(doorOperation, "Build Door");
            
            // Make the door mesh a child and rename it to "Door Mesh"
            Undo.SetTransformParent(doorMesh.transform, doorParent.transform, "Build Door");
            doorMesh.transform.localPosition = Vector3.zero;
            doorMesh.transform.localRotation = Quaternion.identity;
            doorMesh.gameObject.name = "Door Mesh";
            
            // Perform the door operation
            if (PerformDoorOperation(doorOperation, doorMesh, overlappingWalls))
            {
                // Register all new wall meshes for undo
                foreach (var newWall in doorOperation.newWallMeshes)
                {
                    if (newWall != null)
                    {
                        Undo.RegisterCreatedObjectUndo(newWall, "Build Door");
                    }
                }
                
                // Select the door parent
                Selection.activeGameObject = doorParent;
                
                UpdateStatus($"Door created successfully! Modified {overlappingWalls.Length} wall(s).");
            }
            else
            {
                UpdateStatus("Failed to create door. Check console for errors.");
            }
            
            RefreshButtonStates();
        }
        
        private void OnResetDoorClicked()
        {
            var selectedObject = Selection.activeGameObject;
            if (selectedObject == null)
            {
                UpdateStatus("No door operation selected. Please select a door to reset.");
                return;
            }
            
            // Check if selected object has DoorOperation component or if we need to check parent
            var doorOperation = selectedObject.GetComponent<DoorOperation>();
            GameObject doorParent = selectedObject;
            
            if (doorOperation == null)
            {
                // Check if parent has DoorOperation component (user might have selected the Door Mesh child)
                if (selectedObject.transform.parent != null)
                {
                    doorOperation = selectedObject.transform.parent.GetComponent<DoorOperation>();
                    if (doorOperation != null)
                    {
                        doorParent = selectedObject.transform.parent.gameObject;
                    }
                }
            }
            
            if (doorOperation == null)
            {
                UpdateStatus("Selected object is not a door operation. Please select a door to reset.");
                return;
            }
            
            // Find the "Door Mesh" child (the ProBuilder mesh)
            Transform doorMeshTransform = doorParent.transform.Find("Door Mesh");
            GameObject doorMesh = doorMeshTransform != null ? doorMeshTransform.gameObject : null;
            
            if (doorMesh == null)
            {
                // Fallback: look for any child with ProBuilderMesh component
                var probuilderMeshInChild = doorParent.GetComponentInChildren<ProBuilderMesh>();
                if (probuilderMeshInChild != null)
                {
                    doorMesh = probuilderMeshInChild.gameObject;
                }
            }
            
            if (doorMesh == null)
            {
                UpdateStatus("Failed to find Door Mesh. Door structure may be corrupted.");
                return;
            }
            
            ResetDoor(doorOperation, doorMesh);
            
            // Move the Door Mesh back to root
            Undo.SetTransformParent(doorMesh.transform, null, "Reset Door");
            
            // Destroy the door parent (whether it's a prefab or just "Door" GameObject)
            Undo.DestroyObjectImmediate(doorParent);
            
            // Select the ProBuilder mesh
            Selection.activeGameObject = doorMesh;
            
            UpdateStatus("Door reset successfully!");
            RefreshButtonStates();
        }
        
        private void ResetDoor(DoorOperation doorOperation, GameObject doorMesh)
        {
            if (doorOperation == null) return;
            
            // Register undo operation
            Undo.RegisterCompleteObjectUndo(doorOperation.gameObject, "Reset Door");
            Undo.RegisterCompleteObjectUndo(doorMesh, "Reset Door");
            
            // Register original walls for undo
            if (doorOperation.originalWalls != null)
            {
                foreach (var wall in doorOperation.originalWalls)
                {
                    if (wall != null)
                    {
                        Undo.RegisterCompleteObjectUndo(wall, "Reset Door");
                    }
                }
            }
            
            // Destroy new wall meshes (they will be destroyed)
            if (doorOperation.newWallMeshes != null)
            {
                foreach (var newWall in doorOperation.newWallMeshes)
                {
                    if (newWall != null)
                    {
                        Undo.DestroyObjectImmediate(newWall);
                    }
                }
            }
            
            // Re-enable original walls
            if (doorOperation.originalWalls != null)
            {
                foreach (var wall in doorOperation.originalWalls)
                {
                    if (wall != null)
                    {
                        wall.SetActive(true);
                    }
                }
            }
            
            // Re-enable the door renderer
            var doorRenderer = doorMesh.GetComponent<MeshRenderer>();
            if (doorRenderer != null)
            {
                doorRenderer.enabled = true;
            }
            
            // Destroy the door operation component
            Undo.DestroyObjectImmediate(doorOperation);
        }
        
        /// <summary>
        /// Rebuilds a door operation when it's moved
        /// </summary>
        private void RebuildDoorOperation(DoorOperation doorOperation)
        {
            if (doorOperation == null || !doorOperation.IsValid()) return;
            
            // Store references
            var doorMesh = doorOperation.originalDoorMesh;
            var doorParent = doorOperation.gameObject;
            if (doorMesh == null) return;
            
            // Destroy new wall meshes
            if (doorOperation.newWallMeshes != null)
            {
                foreach (var newWall in doorOperation.newWallMeshes)
                {
                    if (newWall != null)
                    {
                        Undo.DestroyObjectImmediate(newWall);
                    }
                }
            }
            
            // Re-enable original walls
            if (doorOperation.originalWalls != null)
            {
                foreach (var wall in doorOperation.originalWalls)
                {
                    if (wall != null)
                    {
                        wall.SetActive(true);
                    }
                }
            }
            
            // Re-enable the door renderer
            var doorRenderer = doorMesh.GetComponent<MeshRenderer>();
            if (doorRenderer != null)
            {
                doorRenderer.enabled = true;
            }
            
            // Clear the door operation data but keep the component
            doorOperation.originalWalls = null;
            doorOperation.newWallMeshes = null;
            
            // Find all overlapping walls with the door at its new position
            var doorProBuilderMesh = doorMesh.GetComponent<ProBuilderMesh>();
            var overlappingWalls = FindAllOverlappingWalls(doorProBuilderMesh);
            
            if (overlappingWalls.Length > 0)
            {
                // Perform the door operation again (reusing the existing DoorOperation component)
                PerformDoorOperation(doorOperation, doorProBuilderMesh, overlappingWalls);
            }
        }
        
        private Room FindRoomContainingDoor(ProBuilderMesh doorMesh)
        {
            var doorBounds = doorMesh.GetComponent<Renderer>().bounds;
            var rooms = FindObjectsByType<Room>(FindObjectsSortMode.None);
            
            UnityEngine.Debug.Log($"=== FINDING ROOM FOR DOOR ===");
            UnityEngine.Debug.Log($"Door bounds: Center={doorBounds.center}, Size={doorBounds.size}");
            UnityEngine.Debug.Log($"Total rooms in scene: {rooms.Length}");
            
            foreach (var room in rooms)
            {
                var roomBounds = room.GetComponent<Renderer>().bounds;
                bool intersects = roomBounds.Intersects(doorBounds);
                UnityEngine.Debug.Log($"  Room '{room.name}': Bounds Center={roomBounds.center}, Size={roomBounds.size}, Intersects={intersects}");
                
                if (intersects)
                {
                    UnityEngine.Debug.Log($"  -> Using this room for door creation");
                    return room;
                }
            }
            
            UnityEngine.Debug.LogWarning("No room found containing the door!");
            return null;
        }
        
        private GameObject[] FindAllOverlappingWalls(ProBuilderMesh doorMesh)
        {
            var doorBounds = doorMesh.GetComponent<Renderer>().bounds;
            var overlappingWalls = new System.Collections.Generic.List<GameObject>();
            
            // Find all rooms in the scene
            var rooms = FindObjectsByType<Room>(FindObjectsSortMode.None);
            
            foreach (var room in rooms)
            {
                var roomComponents = room.GetRoomComponents();
                
                foreach (var component in roomComponents)
                {
                    if (component != null)
                    {
                        var wallComponent = component.GetComponent<RoomWall>();
                        if (wallComponent != null)
                        {
                            var renderer = component.GetComponent<Renderer>();
                            if (renderer != null)
                            {
                                var wallBounds = renderer.bounds;
                                if (wallBounds.Intersects(doorBounds))
                                {
                                    overlappingWalls.Add(component);
                                }
                            }
                        }
                    }
                }
            }
            
            return overlappingWalls.ToArray();
        }
        
        private bool PerformDoorOperation(DoorOperation doorOperation, ProBuilderMesh doorMesh, GameObject[] overlappingWalls)
        {
            // Store original selection to restore later
            var originalSelection = Selection.activeGameObject;
            
            // Setup door operation data
            doorOperation.originalDoorMesh = doorMesh.gameObject;
            doorOperation.room = null; // No longer tied to a single room
            doorOperation.originalWalls = new GameObject[overlappingWalls.Length];
            doorOperation.newWallMeshes = new GameObject[overlappingWalls.Length];
            
            // Process each overlapping wall
            for (int i = 0; i < overlappingWalls.Length; i++)
            {
                var wall = overlappingWalls[i];
                UnityEngine.Debug.Log($"--- Processing wall {i}: {wall.name} ---");
                
                // Store reference to original wall
                doorOperation.originalWalls[i] = wall;
                
                // Create new wall from boolean operation
                var wallMesh = wall.GetComponent<ProBuilderMesh>();
                if (wallMesh != null)
                {
                    UnityEngine.Debug.Log($"  Wall has ProBuilderMesh component");
                    
                    // Ensure both meshes are active for boolean operation
                    wall.SetActive(true);
                    doorMesh.gameObject.SetActive(true);
                    UnityEngine.Debug.Log($"  Both meshes activated");
                    
                    // Clear selection and select the wall for boolean operation
                    Selection.activeGameObject = wall;
                    UnityEngine.Debug.Log($"  Wall selected for boolean operation");
                    
                    var newWall = ProBuilderBooleanUtility.PerformBooleanSubtraction(wallMesh, doorMesh);
                    if (newWall != null)
                    {
                        UnityEngine.Debug.Log($"  Boolean subtraction SUCCESS - new wall created: {newWall.name}");
                        
                        // Make new wall a child of the wall's parent room
                        var wallRoom = wall.GetComponentInParent<Room>();
                        if (wallRoom != null)
                        {
                            newWall.transform.SetParent(wallRoom.transform);
                        }
                        newWall.name = wall.name;
                        
                        // Copy wall component to new wall
                        var originalWallComponent = wall.GetComponent<RoomWall>();
                        if (originalWallComponent != null)
                        {
                            var newWallComponent = newWall.AddComponent<RoomWall>();
                            newWallComponent.direction = originalWallComponent.direction;
                            UnityEngine.Debug.Log($"  RoomWall component copied with direction: {originalWallComponent.direction}");
                        }
                        
                        doorOperation.newWallMeshes[i] = newWall;
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning($"  Boolean subtraction FAILED - returned null");
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"  Wall missing ProBuilderMesh component!");
                }
                
                // Disable original wall after boolean operation completes
                wall.SetActive(false);
                UnityEngine.Debug.Log($"  Original wall disabled");
            }
            
            // Disable door mesh renderer
            var doorRenderer = doorMesh.GetComponent<MeshRenderer>();
            if (doorRenderer != null)
            {
                doorRenderer.enabled = false;
            }
            
            // Restore original selection
            Selection.activeGameObject = originalSelection;
            
            return true;
        }
        
        // ====================================================================================
        // GEOMETRY ANALYSIS FOR COMPLEX ROOM SHAPES
        // ====================================================================================
        
        /// <summary>
        /// Represents a detected corner in the room shape
        /// </summary>
        private struct DetectedCorner
        {
            public Vector3 position;
            public float angle; // Angle in degrees between the two edges meeting at this corner
            public Vector3 normal; // Average normal direction
            
            public DetectedCorner(Vector3 pos, float ang, Vector3 norm)
            {
                position = pos;
                angle = ang;
                normal = norm;
            }
        }
        
        /// <summary>
        /// Represents a detected wall segment between two corners
        /// </summary>
        private struct DetectedWall
        {
            public Vector3 start;
            public Vector3 end;
            public Vector3 center;
            public Vector3 direction; // Normalized direction from start to end
            public float length;
            public Vector3 faceNormal; // Face normal in world space
            
            public DetectedWall(Vector3 startPos, Vector3 endPos, Vector3 normal)
            {
                start = startPos;
                end = endPos;
                center = (start + end) * 0.5f;
                direction = (end - start).normalized;
                length = Vector3.Distance(start, end);
                faceNormal = normal;
            }
        }
        
        /// <summary>
        /// Analyzes a ProBuilder mesh to detect corners and walls for complex room shapes
        /// </summary>
        /// <param name="mesh">The ProBuilder mesh to analyze</param>
        /// <param name="angleThreshold">Minimum angle in degrees to consider a corner (default 135 = angles sharper than 135°)</param>
        /// <returns>Tuple of detected corners and walls</returns>
        private (System.Collections.Generic.List<DetectedCorner> corners, System.Collections.Generic.List<DetectedWall> walls) 
            AnalyzeRoomGeometry(ProBuilderMesh mesh, float angleThreshold = 135f)
        {
            var detectedCorners = new System.Collections.Generic.List<DetectedCorner>();
            var detectedWalls = new System.Collections.Generic.List<DetectedWall>();
            
            // Get the bottom face edges (assuming Y-up and the floor is at the bottom)
            var bottomEdges = GetBottomPerimeterEdges(mesh);
            
            if (bottomEdges.Count < 3)
            {
                return (detectedCorners, detectedWalls);
            }
            
            // Sort edges to form a continuous loop
            var sortedEdges = SortEdgesIntoLoop(bottomEdges, mesh);
            
            // Calculate the room center for outward normal calculation (in world space)
            var positions = mesh.positions;
            Vector3 roomCenter = Vector3.zero;
            foreach (var edge in sortedEdges)
            {
                // Convert local positions to world positions
                roomCenter += mesh.transform.TransformPoint(positions[edge.a]);
                roomCenter += mesh.transform.TransformPoint(positions[edge.b]);
            }
            roomCenter /= (sortedEdges.Count * 2);
            
            // Detect corners by analyzing angles between consecutive edges
            for (int i = 0; i < sortedEdges.Count; i++)
            {
                int nextIdx = (i + 1) % sortedEdges.Count;
                
                var currentEdge = sortedEdges[i];
                var nextEdge = sortedEdges[nextIdx];
                
                // Get the shared vertex (corner point)
                Vector3 cornerPos = GetSharedVertex(currentEdge, nextEdge, mesh);
                
                // Calculate the angle between the two edges
                Vector3 dir1 = GetEdgeDirection(currentEdge, mesh, cornerPos);
                Vector3 dir2 = GetEdgeDirection(nextEdge, mesh, cornerPos);
                
                float angle = Vector3.Angle(dir1, dir2);
                
                // If the angle deviates from 180° (straight line) enough, it's a corner
                // For an L-shape, we want 90° angles (interior) or 270° (exterior)
                // Vector3.Angle returns 0-180, so we check if it's NOT close to 180 (straight)
                if (angle < angleThreshold)
                {
                    // Additional filtering: only consider angles that are significantly different from 180°
                    // This helps avoid detecting corners on nearly straight edges
                    float angleDeviation = Mathf.Abs(angle - 180f);
                    if (angleDeviation > 30f) // Only corners with >30° deviation from straight
                    {
                        // Calculate the outward-pointing normal by using the corner position relative to room center
                        // This gives us the direction from room center to corner, which is the outward normal
                        Vector3 outwardNormal = (cornerPos - roomCenter).normalized;
                        // Project to XZ plane to get the 2D direction for compass detection
                        outwardNormal = new Vector3(outwardNormal.x, 0, outwardNormal.z).normalized;
                        
                        detectedCorners.Add(new DetectedCorner(cornerPos, angle, outwardNormal));
                    }
                }
            }
            
            // Create walls between consecutive corners
            float minWallLength = 0.1f; // Minimum wall length to avoid interior artifacts
            
            for (int i = 0; i < detectedCorners.Count; i++)
            {
                int nextIdx = (i + 1) % detectedCorners.Count;
                var corner1 = detectedCorners[i];
                var corner2 = detectedCorners[nextIdx];
                // Calculate face normal for this wall segment by finding the corresponding face normal from the mesh
                Vector3 wallDirection = (corner2.position - corner1.position).normalized;
                Vector3 wallNormal = GetFaceNormalForEdgeFromMesh(corner1.position, corner2.position, mesh);
                var wall = new DetectedWall(corner1.position, corner2.position, wallNormal);
                
                // Only add walls that are long enough (filter out short interior segments)
                if (wall.length >= minWallLength)
                {
                    detectedWalls.Add(wall);
                }
                else
                {
                }
            }
            
            return (detectedCorners, detectedWalls);
        }
        
        /// <summary>
        /// Gets bottom perimeter edges by taking edges from side-facing faces (normals not up/down)
        /// whose both vertices lie at the bottom plane. This avoids diagonals and respects concavity.
        /// </summary>
        private System.Collections.Generic.List<UnityEngine.ProBuilder.Edge> GetBottomPerimeterEdges(ProBuilderMesh mesh)
        {
            var perimeterEdges = new System.Collections.Generic.List<UnityEngine.ProBuilder.Edge>();
            var vertices = mesh.GetVertices();
            
            float tolerance = 0.01f;
            var positions = mesh.positions;
            
            // Find minY for bottom plane
            float minY = float.MaxValue;
            for (int i = 0; i < vertices.Length; i++)
                if (vertices[i].position.y < minY) minY = vertices[i].position.y;
            
            // Deduplicate using undirected edge keys
            System.Func<int, int, (int, int)> keyOf = (a, b) => (Mathf.Min(a, b), Mathf.Max(a, b));
            var added = new System.Collections.Generic.HashSet<(int, int)>();
            
            // Iterate faces; pick side faces (|normal.y| small)
            for (int f = 0; f < mesh.faces.Count; f++)
            {
                var face = mesh.faces[f];
                Vector3 fn = ComputeFaceNormal(face, positions);
                if (Mathf.Abs(fn.y) > 0.3f) // skip faces facing mostly up or down
                    continue;
                
                var edges = face.edges;
                for (int e = 0; e < edges.Count; e++)
                {
                    var edge = edges[e];
                    var a = edge.a;
                    var b = edge.b;
                    // We only want the bottom rim of side faces
                    if (Mathf.Abs(positions[a].y - minY) < tolerance && Mathf.Abs(positions[b].y - minY) < tolerance)
                    {
                        var k = keyOf(a, b);
                        if (added.Add(k))
                        {
                            perimeterEdges.Add(new UnityEngine.ProBuilder.Edge(k.Item1, k.Item2));
                        }
                    }
                }
            }
            
            // Sort into loop
            perimeterEdges = SortEdgesIntoLoop(perimeterEdges, mesh);
            
            return perimeterEdges;
        }
        
        /// <summary>
        /// Calculates the convex hull of a set of vertices using Graham scan algorithm
        /// </summary>
        private System.Collections.Generic.List<Vector3> CalculateConvexHull(System.Collections.Generic.List<Vector3> points)
        {
            if (points.Count < 3) return points;
            
            // Find the bottom-most point (and leftmost in case of tie)
            Vector3 bottomMost = points[0];
            int bottomMostIndex = 0;
            for (int i = 1; i < points.Count; i++)
            {
                if (points[i].z < bottomMost.z || 
                    (points[i].z == bottomMost.z && points[i].x < bottomMost.x))
                {
                    bottomMost = points[i];
                    bottomMostIndex = i;
                }
            }
            
            // Sort points by polar angle with respect to bottomMost
            var sortedPoints = new System.Collections.Generic.List<Vector3>(points);
            sortedPoints.RemoveAt(bottomMostIndex); // Remove the pivot point
            
            sortedPoints.Sort((a, b) => {
                Vector3 dirA = a - bottomMost;
                Vector3 dirB = b - bottomMost;
                
                // Calculate cross product to determine angle
                float cross = dirA.x * dirB.z - dirA.z * dirB.x;
                if (Mathf.Abs(cross) < 0.001f)
                {
                    // Points are collinear, sort by distance
                    float distA = dirA.sqrMagnitude;
                    float distB = dirB.sqrMagnitude;
                    return distA.CompareTo(distB);
                }
                return cross > 0 ? -1 : 1; // Counter-clockwise order
            });
            
            // Graham scan
            var hull = new System.Collections.Generic.List<Vector3>();
            hull.Add(bottomMost); // Start with the bottom-most point
            
            for (int i = 0; i < sortedPoints.Count; i++)
            {
                // Remove points from the hull while the angle formed by the last three points
                // makes a non-left turn (right turn or collinear)
                while (hull.Count > 1 && 
                       GetOrientation(hull[hull.Count - 2], hull[hull.Count - 1], sortedPoints[i]) <= 0)
                {
                    hull.RemoveAt(hull.Count - 1);
                }
                hull.Add(sortedPoints[i]);
            }
            
            return hull;
        }
        
        /// <summary>
        /// Determines the orientation of three points (clockwise, counter-clockwise, or collinear)
        /// Returns: 0 = collinear, 1 = clockwise, 2 = counter-clockwise
        /// </summary>
        private int GetOrientation(Vector3 p, Vector3 q, Vector3 r)
        {
            float val = (q.z - p.z) * (r.x - q.x) - (q.x - p.x) * (r.z - q.z);
            
            if (Mathf.Abs(val) < 0.001f) return 0; // Collinear
            return (val > 0) ? 1 : 2; // Clockwise or counter-clockwise
        }
        
        /// <summary>
        /// Finds the ProBuilder edge between two vertices
        /// </summary>
        private UnityEngine.ProBuilder.Edge? FindEdgeBetweenVertices(Vector3 v1, Vector3 v2, UnityEngine.ProBuilder.Vertex[] vertices, float tolerance)
        {
            // Find vertex indices
            int index1 = -1, index2 = -1;
            for (int i = 0; i < vertices.Length; i++)
            {
                if (Vector3.Distance(vertices[i].position, v1) < tolerance)
                    index1 = i;
                if (Vector3.Distance(vertices[i].position, v2) < tolerance)
                    index2 = i;
            }
            
            if (index1 != -1 && index2 != -1)
            {
                return new UnityEngine.ProBuilder.Edge(index1, index2);
            }
            
            return null;
        }

        /// <summary>
        /// Computes an approximate face normal by averaging edge vectors from the first three unique vertices.
        /// Works for quads and triangles.
        /// </summary>
        private Vector3 DetermineWallNormalFromPosition(Vector3 wallCenter, Vector3 wallDirection)
        {
            // Determine the wall normal based on the wall's position and direction
            // For a wall facing North, the normal should point North (0, 0, 1)
            // For a wall facing East, the normal should point East (1, 0, 0)
            // For a wall facing South, the normal should point South (0, 0, -1)
            // For a wall facing West, the normal should point West (-1, 0, 0)
            
            // Calculate the wall normal by taking the cross product of wall direction and up vector
            Vector3 wallNormal = Vector3.Cross(wallDirection, Vector3.up).normalized;
            
            // Determine which direction this wall is facing based on its position
            // We'll use the wall center position to determine the outward direction
            
            // For walls that are primarily horizontal (wallDirection.z is dominant)
            if (Mathf.Abs(wallDirection.z) > Mathf.Abs(wallDirection.x))
            {
                // Horizontal wall - normal should point North or South
                if (wallDirection.z > 0) // Wall runs from West to East
                {
                    wallNormal = Vector3.forward; // North
                }
                else // Wall runs from East to West
                {
                    wallNormal = -Vector3.forward; // South
                }
            }
            else
            {
                // Vertical wall - normal should point East or West
                if (wallDirection.x > 0) // Wall runs from South to North
                {
                    wallNormal = Vector3.right; // East
                }
                else // Wall runs from North to South
                {
                    wallNormal = -Vector3.right; // West
                }
            }
            
            return wallNormal;
        }
        
        private Vector3 GetFaceNormalForEdgeFromMesh(Vector3 start, Vector3 end, ProBuilderMesh mesh)
        {
            float tolerance = 0.01f;
            var positions = mesh.positions;
            
            // Calculate room center for outward normal determination
            Vector3 roomCenter = Vector3.zero;
            for (int i = 0; i < positions.Count; i++)
            {
                roomCenter += mesh.transform.TransformPoint(positions[i]);
            }
            roomCenter /= positions.Count;
            
            // Find the face that contains this edge
            for (int f = 0; f < mesh.faces.Count; f++)
            {
                var face = mesh.faces[f];
                var faceNormal = ComputeFaceNormal(face, positions);
                
                // Skip faces that are mostly up or down
                if (Mathf.Abs(faceNormal.y) > 0.3f)
                    continue;
                
                var edges = face.edges;
                for (int e = 0; e < edges.Count; e++)
                {
                    var edge = edges[e];
                    var a = mesh.transform.TransformPoint(positions[edge.a]);
                    var b = mesh.transform.TransformPoint(positions[edge.b]);
                    
                    // Check if this edge matches our wall edge (either direction)
                    bool matches = (Vector3.Distance(a, start) < tolerance && Vector3.Distance(b, end) < tolerance) ||
                                  (Vector3.Distance(a, end) < tolerance && Vector3.Distance(b, start) < tolerance);
                    
                    if (matches)
                    {
                        // Convert face normal to world space
                        Vector3 worldFaceNormal = mesh.transform.TransformDirection(faceNormal);
                        
                        // Calculate face center in world space
                        Vector3 faceCenter = Vector3.zero;
                        for (int i = 0; i < edges.Count; i++)
                        {
                            faceCenter += mesh.transform.TransformPoint(positions[edges[i].a]);
                        }
                        faceCenter /= edges.Count;
                        
                        // Check if the normal is pointing outward (away from room center)
                        Vector3 toRoomCenter = (roomCenter - faceCenter).normalized;
                        float dot = Vector3.Dot(worldFaceNormal, toRoomCenter);
                        
                        // If dot product is positive, normal is pointing inward, so flip it
                        if (dot > 0)
                        {
                            worldFaceNormal = -worldFaceNormal;
                        }
                        
                        return worldFaceNormal;
                    }
                }
            }
            
            // Fallback: calculate normal from wall direction
            Vector3 wallDirection = (end - start).normalized;
            Vector3 fallbackNormal = Vector3.Cross(wallDirection, Vector3.up).normalized;
            
            // Ensure the fallback normal points outward by checking against room center
            Vector3 wallCenter = (start + end) * 0.5f;
            Vector3 fallbackToRoomCenter = (roomCenter - wallCenter).normalized;
            float fallbackDot = Vector3.Dot(fallbackNormal, fallbackToRoomCenter);
            
            // If dot product is positive, normal is pointing inward, so flip it
            if (fallbackDot > 0)
            {
                fallbackNormal = -fallbackNormal;
            }
            
            return fallbackNormal;
        }
        
        private Vector3 GetFaceNormalForEdge(Vector3 start, Vector3 end, ProBuilderMesh mesh)
        {
            float tolerance = 0.01f;
            var positions = mesh.positions;
            
            // Find the face that contains this edge
            for (int f = 0; f < mesh.faces.Count; f++)
            {
                var face = mesh.faces[f];
                var faceNormal = ComputeFaceNormal(face, positions);
                
                // Skip faces that are mostly up or down
                if (Mathf.Abs(faceNormal.y) > 0.3f)
                    continue;
                
                var edges = face.edges;
                for (int e = 0; e < edges.Count; e++)
                {
                    var edge = edges[e];
                    var a = positions[edge.a];
                    var b = positions[edge.b];
                    
                    // Check if this edge matches our wall edge (either direction)
                    bool matches = (Vector3.Distance(a, start) < tolerance && Vector3.Distance(b, end) < tolerance) ||
                                  (Vector3.Distance(a, end) < tolerance && Vector3.Distance(b, start) < tolerance);
                    
                    if (matches)
                    {
                        // Convert face normal to world space
                        Vector3 worldFaceNormal = mesh.transform.TransformDirection(faceNormal);
                        return worldFaceNormal;
                    }
                }
            }
            
            // If we can't find the face, let's try a different approach
            // Calculate the wall direction and determine the normal based on the wall's orientation
            Vector3 wallDirection = (end - start).normalized;
            
            // Determine the wall normal based on the wall's direction
            // For a wall running along the Z-axis (horizontal), the normal should point along the X-axis
            // For a wall running along the X-axis (vertical), the normal should point along the Z-axis
            
            Vector3 wallNormal;
            if (Mathf.Abs(wallDirection.z) > Mathf.Abs(wallDirection.x))
            {
                // Wall is primarily horizontal (runs along Z-axis)
                // The normal should point along the X-axis
                wallNormal = wallDirection.z > 0 ? Vector3.right : -Vector3.right;
            }
            else
            {
                // Wall is primarily vertical (runs along X-axis)
                // The normal should point along the Z-axis
                wallNormal = wallDirection.x > 0 ? -Vector3.forward : Vector3.forward;
            }
            
            return wallNormal;
        }
        
        private Vector3 ComputeFaceNormal(Face face, System.Collections.Generic.IList<Vector3> positions)
        {
            // Gather unique indices
            var edges = face.edges;
            var unique = new System.Collections.Generic.List<int>(4);
            for (int i = 0; i < edges.Count && unique.Count < 4; i++)
            {
                if (!unique.Contains(edges[i].a)) unique.Add(edges[i].a);
                if (!unique.Contains(edges[i].b)) unique.Add(edges[i].b);
            }
            if (unique.Count < 3)
                return Vector3.up; // fallback
            Vector3 a = positions[unique[0]];
            Vector3 b = positions[unique[1]];
            Vector3 c = positions[unique[2]];
            var n = Vector3.Cross(b - a, c - a).normalized;
            return n;
        }

        // Custom comparer for Edge to treat (a,b) and (b,a) as equal
        private class EdgeComparer : System.Collections.Generic.IEqualityComparer<UnityEngine.ProBuilder.Edge>
        {
            public bool Equals(UnityEngine.ProBuilder.Edge x, UnityEngine.ProBuilder.Edge y)
            {
                return (x.a == y.a && x.b == y.b) || (x.a == y.b && x.b == y.a);
            }

            public int GetHashCode(UnityEngine.ProBuilder.Edge obj)
            {
                // Hash code should be consistent for (a,b) and (b,a)
                return UnityEngine.Mathf.Min(obj.a, obj.b).GetHashCode() ^ UnityEngine.Mathf.Max(obj.a, obj.b).GetHashCode();
            }
        }
        
        /// <summary>
        /// Sorts edges into a continuous loop (handles duplicate vertices at same positions)
        /// </summary>
        private System.Collections.Generic.List<UnityEngine.ProBuilder.Edge> SortEdgesIntoLoop(
            System.Collections.Generic.List<UnityEngine.ProBuilder.Edge> edges, ProBuilderMesh mesh)
        {
            
            if (edges.Count == 0) return edges;
            
            var vertices = mesh.GetVertices();
            float positionTolerance = 0.001f;
            
            // First, remove duplicate edges (same vertices at same positions)
            var uniqueEdges = new System.Collections.Generic.List<UnityEngine.ProBuilder.Edge>();
            var addedPositions = new System.Collections.Generic.HashSet<string>();
            
            foreach (var edge in edges)
            {
                Vector3 v1 = vertices[edge.a].position;
                Vector3 v2 = vertices[edge.b].position;
                
                // Create a position-based key for this edge
                string key1 = $"{v1.x:F3},{v1.y:F3},{v1.z:F3}-{v2.x:F3},{v2.y:F3},{v2.z:F3}";
                string key2 = $"{v2.x:F3},{v2.y:F3},{v2.z:F3}-{v1.x:F3},{v1.y:F3},{v1.z:F3}";
                
                // Only add if we haven't seen this edge (or its reverse) before
                if (!addedPositions.Contains(key1) && !addedPositions.Contains(key2))
                {
                    uniqueEdges.Add(edge);
                    addedPositions.Add(key1);
                }
                else
                {
                }
            }
            
            // Try to find the perimeter by starting from the most extreme point
            var sorted = new System.Collections.Generic.List<UnityEngine.ProBuilder.Edge>();
            var remaining = new System.Collections.Generic.List<UnityEngine.ProBuilder.Edge>(uniqueEdges);
            
            // Find the edge that starts from the most extreme position (back from center)
            Vector3 center = Vector3.zero;
            foreach (var edge in uniqueEdges)
            {
                center += vertices[edge.a].position;
                center += vertices[edge.b].position;
            }
            center /= (uniqueEdges.Count * 2);
            
            float maxDistance = 0f;
            int startEdgeIndex = 0;
            for (int i = 0; i < uniqueEdges.Count; i++)
            {
                var edge = uniqueEdges[i];
                Vector3 v1 = vertices[edge.a].position;
                Vector3 v2 = vertices[edge.b].position;
                float dist1 = Vector3.Distance(v1, center);
                float dist2 = Vector3.Distance(v2, center);
                float maxDist = Mathf.Max(dist1, dist2);
                if (maxDist > maxDistance)
                {
                    maxDistance = maxDist;
                    startEdgeIndex = i;
                }
            }
            
            // Start with the edge back from center
            var firstEdge = uniqueEdges[startEdgeIndex];
            sorted.Add(firstEdge);
            remaining.RemoveAt(startEdgeIndex);
            
            Vector3 currentPosition = vertices[firstEdge.b].position; // Track by position, not index
            
            int iteration = 0;
            // Keep finding connected edges until we complete the loop
            while (remaining.Count > 0 && iteration < 100) // Safety limit
            {
                iteration++;
                
                bool foundNext = false;
                
                // Try to find the edge that continues the perimeter (prefer edges that keep us on the outside)
                for (int i = 0; i < remaining.Count; i++)
                {
                    var candidate = remaining[i];
                    Vector3 candidateStart = vertices[candidate.a].position;
                    Vector3 candidateEnd = vertices[candidate.b].position;
                    
                    // Check if this edge's start position matches current position
                    if (Vector3.Distance(candidateStart, currentPosition) < positionTolerance)
                    {
                        sorted.Add(candidate);
                        currentPosition = candidateEnd; // Move to the other end
                        remaining.RemoveAt(i);
                        foundNext = true;
                        break;
                    }
                    // Check if this edge's end position matches current position
                    else if (Vector3.Distance(candidateEnd, currentPosition) < positionTolerance)
                    {
                        // Add edge but swap a and b to maintain direction
                        var swappedEdge = new UnityEngine.ProBuilder.Edge(candidate.b, candidate.a);
                        sorted.Add(swappedEdge);
                        currentPosition = candidateStart; // Move to the other end
                        remaining.RemoveAt(i);
                        foundNext = true;
                        break;
                    }
                }
                
                if (!foundNext)
                {
                    
                    // Log remaining edges
                    for (int i = 0; i < remaining.Count; i++)
                    {
                        var edge = remaining[i];
                    }
                    break;
                }
                
                // Safety check to prevent infinite loops
                if (sorted.Count > uniqueEdges.Count)
                {
                    break;
                }
            }
            
            // Log the final sorted edges
            for (int i = 0; i < sorted.Count; i++)
            {
                var edge = sorted[i];
                Vector3 v1 = vertices[edge.a].position;
                Vector3 v2 = vertices[edge.b].position;
            }
            
            return sorted;
        }
        
        /// <summary>
        /// Gets the shared vertex between two edges (handles duplicate vertices at same positions)
        /// </summary>
        private Vector3 GetSharedVertex(UnityEngine.ProBuilder.Edge edge1, UnityEngine.ProBuilder.Edge edge2, ProBuilderMesh mesh)
        {
            var positions = mesh.positions;
            float tolerance = 0.001f;
            
            // Check all combinations of edge vertices for position matches (in local space)
            Vector3 e1a = positions[edge1.a];
            Vector3 e1b = positions[edge1.b];
            Vector3 e2a = positions[edge2.a];
            Vector3 e2b = positions[edge2.b];
            
            // Check if edge1.a matches either edge2 vertex
            if (Vector3.Distance(e1a, e2a) < tolerance)
                return mesh.transform.TransformPoint(e1a); // Convert to world space
            if (Vector3.Distance(e1a, e2b) < tolerance)
                return mesh.transform.TransformPoint(e1a); // Convert to world space
            
            // Check if edge1.b matches either edge2 vertex
            if (Vector3.Distance(e1b, e2a) < tolerance)
                return mesh.transform.TransformPoint(e1b); // Convert to world space
            if (Vector3.Distance(e1b, e2b) < tolerance)
                return mesh.transform.TransformPoint(e1b); // Convert to world space
            
            // No shared vertex found, return midpoint of first edge as fallback
            return mesh.transform.TransformPoint((e1a + e1b) * 0.5f); // Convert to world space
        }
        
        /// <summary>
        /// Gets the direction of an edge pointing away from a specific vertex
        /// </summary>
        private Vector3 GetEdgeDirection(UnityEngine.ProBuilder.Edge edge, ProBuilderMesh mesh, Vector3 fromVertex)
        {
            var positions = mesh.positions;
            // Convert local positions to world positions
            Vector3 v1 = mesh.transform.TransformPoint(positions[edge.a]);
            Vector3 v2 = mesh.transform.TransformPoint(positions[edge.b]);
            
            // Determine which vertex is the "from" vertex and return direction to the other
            float dist1 = Vector3.Distance(v1, fromVertex);
            float dist2 = Vector3.Distance(v2, fromVertex);
            
            // Use a small tolerance for vertex matching
            float tolerance = 0.001f;
            
            if (dist1 < tolerance)
            {
                // v1 is the from vertex, direction is towards v2
                return (v2 - v1).normalized;
            }
            else if (dist2 < tolerance)
            {
                // v2 is the from vertex, direction is towards v1
                return (v1 - v2).normalized;
            }
            else
            {
                // Neither vertex matches exactly, use closest one
                if (dist1 < dist2)
                    return (v2 - v1).normalized;
                else
                    return (v1 - v2).normalized;
            }
        }
        
        private void OnEnable()
        {
            // Subscribe to selection change events
            Selection.selectionChanged += OnSelectionChanged;
        }
        
        private void OnDisable()
        {
            // Unsubscribe from events
            Selection.selectionChanged -= OnSelectionChanged;
        }
        
        private void Update()
        {
            // Real-time update when editing a room
            var selectedObject = Selection.activeGameObject;
            if (selectedObject != null)
            {
                var room = selectedObject.GetComponent<Room>();
                if (room != null && room.isInEditMode)
                {
                    room.UpdateRoomComponents();
                }
            }
            
            // Check if settings have changed and refresh UI if needed
            if (currentSettings != null && settingsEditor != null)
            {
                if (settingsEditor.serializedObject != null && settingsEditor.serializedObject.hasModifiedProperties)
                {
                    settingsEditor.serializedObject.Update();
                    settingsEditor.serializedObject.ApplyModifiedProperties();
                    Repaint();
                }
            }
        }
        
        /// <summary>
        /// Gets the isometric view direction vector based on the BackDirection setting
        /// This is the direction FROM the camera position TOWARDS the scene center
        /// </summary>
        private Vector3 GetIsometricViewDirection(BackDirection backDirection)
        {
            switch (backDirection)
            {
                case BackDirection.NorthEast:
                    return new Vector3(-1, -1, -1).normalized; // Looking from NE (high +X,+Z) towards SW (low -X,-Z)
                case BackDirection.NorthWest:
                    return new Vector3(1, -1, -1).normalized; // Looking from NW (high -X,+Z) towards SE (low +X,-Z)
                case BackDirection.SouthEast:
                    return new Vector3(-1, -1, 1).normalized; // Looking from SE (high +X,-Z) towards NW (low -X,+Z)
                case BackDirection.SouthWest:
                    return new Vector3(1, -1, 1).normalized; // Looking from SW (high -X,-Z) towards NE (low +X,+Z)
                default:
                    return new Vector3(-1, -1, -1).normalized;
            }
        }
        
        /// <summary>
        /// Determines if a wall is a "back wall" from the isometric viewing direction.
        /// A wall is considered "back" only if it would NOT visually overlap the floor from that view.
        /// </summary>
        private bool IsBackWall(DetectedWall wall, Vector3 viewDirection, float wallHeight, GameObject roomParent, int wallIndex)
        {
            // Get the floor of this specific room
            GameObject roomFloorObject = null;
            if (roomParent != null)
            {
                var room = roomParent.GetComponent<Room>();
                if (room != null && room.floor != null)
                {
                    roomFloorObject = room.floor;
                }
                else
                {
                    // Try to find floor as a child
                    var floorTransform = roomParent.transform.Find("Floor");
                    if (floorTransform != null)
                    {
                        roomFloorObject = floorTransform.gameObject;
                    }
                }
            }
            
            // If we can't find the room's floor, we can't determine if it's a back wall
            if (roomFloorObject == null)
            {
                return false;
            }
            
            // Calculate top edge endpoints
            Vector3 bottomA = wall.start;
            Vector3 bottomB = wall.end;
            Vector3 topA = bottomA + Vector3.up * wallHeight;
            Vector3 topB = bottomB + Vector3.up * wallHeight;
            
            // Calculate the room center for camera positioning
            Vector3 wallCenter = (topA + topB) * 0.5f;
            
            // Position the isometric camera far above and behind the room in the viewing direction
            float cameraDistance = 1000f;
            Vector3 cameraPosition = wallCenter - viewDirection * cameraDistance;
            
            // Sample multiple points along the top edge
            int sampleCount = Mathf.Max(3, Mathf.CeilToInt(wall.length / 0.5f));
            
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)(sampleCount - 1);
                Vector3 samplePoint = Vector3.Lerp(topA, topB, t);
                
                // Cast ray FROM camera position THROUGH the sample point on the wall top
                Vector3 rayDirection = (samplePoint - cameraPosition).normalized;
                Ray ray = new Ray(cameraPosition, rayDirection);
                
                // Cast for a reasonable distance to check for floor hits
                float maxDistance = cameraDistance + 100f;
                RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance);
                
                foreach (var hit in hits)
                {
                    // Check if hit object is THIS room's floor specifically
                    if (hit.collider.gameObject == roomFloorObject)
                    {
                        return false; // Wall would overlap THIS room's floor from this view
                    }
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Determines which walls are back based on isometric view simulation.
        /// A wall is considered "back" if it would NOT visually overlap the floor from the specified isometric direction.
        /// </summary>
        private System.Collections.Generic.HashSet<DetectedWall> DetermineBackWallsFromPerimeter(System.Collections.Generic.List<DetectedWall> walls)
        {
            var backWalls = new System.Collections.Generic.HashSet<DetectedWall>();
            
            if (currentSettings == null)
            {
                return backWalls;
            }
            
            // Get the isometric viewing direction from settings
            Vector3 viewDirection = GetIsometricViewDirection(currentSettings.backDirection);
            
            // Get the room's base height from the ProBuilder mesh bounds
            GameObject roomParent = Selection.activeGameObject;
            float baseHeight = currentSettings.wallHeight; // Default to settings
            if (roomParent != null)
            {
                var renderer = roomParent.GetComponent<Renderer>();
                if (renderer != null)
                {
                    baseHeight = renderer.bounds.size.y;
                }
            }
            
            // Use BACK wall height for raycast detection (base height + additional back height)
            float wallHeight = baseHeight + currentSettings.wallBackHeight;
            
            int wallIndex = 0;
            foreach (var wall in walls)
            {
                bool isBack = IsBackWall(wall, viewDirection, wallHeight, roomParent, wallIndex);
                
                if (isBack)
                {
                    backWalls.Add(wall);
                }
                
                wallIndex++;
            }
            
            return backWalls;
        }
        
        /// <summary>
        /// Filters walls to only include those on the true exterior boundary
        /// Uses silhouette-based algorithm to find walls that are not recessed behind others
        /// </summary>
        private System.Collections.Generic.List<DetectedWall> FilterToExteriorWalls(System.Collections.Generic.List<DetectedWall> walls)
        {
            if (walls.Count == 0) return new System.Collections.Generic.List<DetectedWall>();
            
            // Step 1: Extract the full perimeter loop (all walls are already perimeter walls)
            
            // Step 2: Classify each perimeter wall by facing direction
            var wallsByDirection = new System.Collections.Generic.Dictionary<WallDirection, System.Collections.Generic.List<DetectedWall>>();
            wallsByDirection[WallDirection.North] = new System.Collections.Generic.List<DetectedWall>();
            wallsByDirection[WallDirection.East] = new System.Collections.Generic.List<DetectedWall>();
            wallsByDirection[WallDirection.South] = new System.Collections.Generic.List<DetectedWall>();
            wallsByDirection[WallDirection.West] = new System.Collections.Generic.List<DetectedWall>();
            
            foreach (var wall in walls)
            {
                WallDirection direction = DetermineWallDirection(wall.faceNormal);
                wallsByDirection[direction].Add(wall);
            }
            
            // Step 3: For each direction, find walls that are not recessed behind others
            var backWalls = new System.Collections.Generic.List<DetectedWall>();
            
            foreach (var direction in new[] { WallDirection.North, WallDirection.East, WallDirection.South, WallDirection.West })
            {
                var wallsInDirection = wallsByDirection[direction];
                
                foreach (var wall in wallsInDirection)
                {
                    bool isRecessed = IsWallRecessed(wall, wallsInDirection, direction);
                    
                    if (!isRecessed)
                    {
                        backWalls.Add(wall);
                    }
                    else
                    {
                    }
                }
            }
            
            
            return backWalls;
        }
        
        /// <summary>
        /// Checks if a wall is recessed behind another wall in the same direction
        /// A wall is recessed if another wall faces the same direction, overlaps in perpendicular range, and extends further outward
        /// </summary>
        private bool IsWallRecessed(DetectedWall wall, System.Collections.Generic.List<DetectedWall> wallsInSameDirection, WallDirection direction)
        {
            Vector2 wallStart = new Vector2(wall.start.x, wall.start.z);
            Vector2 wallEnd = new Vector2(wall.end.x, wall.end.z);
            
            // Get the perpendicular range of this wall
            Vector2 wallPerpStart, wallPerpEnd;
            float wallOutwardCoord;
            
            switch (direction)
            {
                case WallDirection.North:
                    wallPerpStart = new Vector2(Mathf.Min(wallStart.x, wallEnd.x), 0);
                    wallPerpEnd = new Vector2(Mathf.Max(wallStart.x, wallEnd.x), 0);
                    wallOutwardCoord = wall.center.z; // North = +Z
                    break;
                case WallDirection.East:
                    wallPerpStart = new Vector2(0, Mathf.Min(wallStart.y, wallEnd.y));
                    wallPerpEnd = new Vector2(0, Mathf.Max(wallStart.y, wallEnd.y));
                    wallOutwardCoord = wall.center.x; // East = +X
                    break;
                case WallDirection.South:
                    wallPerpStart = new Vector2(Mathf.Min(wallStart.x, wallEnd.x), 0);
                    wallPerpEnd = new Vector2(Mathf.Max(wallStart.x, wallEnd.x), 0);
                    wallOutwardCoord = wall.center.z; // South = -Z
                    break;
                case WallDirection.West:
                    wallPerpStart = new Vector2(0, Mathf.Min(wallStart.y, wallEnd.y));
                    wallPerpEnd = new Vector2(0, Mathf.Max(wallStart.y, wallEnd.y));
                    wallOutwardCoord = wall.center.x; // West = -X
                    break;
                default:
                    return false;
            }
            
            // Check against all other walls in the same direction
            foreach (var otherWall in wallsInSameDirection)
            {
                if (AreWallsEqual(wall, otherWall)) continue; // Skip self
                
                Vector2 otherStart = new Vector2(otherWall.start.x, otherWall.start.z);
                Vector2 otherEnd = new Vector2(otherWall.end.x, otherWall.end.z);
                
                // Get the perpendicular range of the other wall
                Vector2 otherPerpStart, otherPerpEnd;
                float otherOutwardCoord;
                
                switch (direction)
                {
                    case WallDirection.North:
                        otherPerpStart = new Vector2(Mathf.Min(otherStart.x, otherEnd.x), 0);
                        otherPerpEnd = new Vector2(Mathf.Max(otherStart.x, otherEnd.x), 0);
                        otherOutwardCoord = otherWall.center.z;
                        break;
                    case WallDirection.East:
                        otherPerpStart = new Vector2(0, Mathf.Min(otherStart.y, otherEnd.y));
                        otherPerpEnd = new Vector2(0, Mathf.Max(otherStart.y, otherEnd.y));
                        otherOutwardCoord = otherWall.center.x;
                        break;
                    case WallDirection.South:
                        otherPerpStart = new Vector2(Mathf.Min(otherStart.x, otherEnd.x), 0);
                        otherPerpEnd = new Vector2(Mathf.Max(otherStart.x, otherEnd.x), 0);
                        otherOutwardCoord = otherWall.center.z;
                        break;
                    case WallDirection.West:
                        otherPerpStart = new Vector2(0, Mathf.Min(otherStart.y, otherEnd.y));
                        otherPerpEnd = new Vector2(0, Mathf.Max(otherStart.y, otherEnd.y));
                        otherOutwardCoord = otherWall.center.x;
                        break;
                    default:
                        continue;
                }
                
                // Check if the other wall overlaps in perpendicular range
                bool overlaps = false;
                switch (direction)
                {
                    case WallDirection.North:
                    case WallDirection.South:
                        overlaps = wallPerpStart.x < otherPerpEnd.x && wallPerpEnd.x > otherPerpStart.x;
                        break;
                    case WallDirection.East:
                    case WallDirection.West:
                        overlaps = wallPerpStart.y < otherPerpEnd.y && wallPerpEnd.y > otherPerpStart.y;
                        break;
                }
                
                if (overlaps)
                {
                    // Check if the other wall extends further outward
                    bool otherIsFurtherOut = false;
                    switch (direction)
                    {
                        case WallDirection.North:
                            otherIsFurtherOut = otherOutwardCoord > wallOutwardCoord;
                            break;
                        case WallDirection.East:
                            otherIsFurtherOut = otherOutwardCoord > wallOutwardCoord;
                            break;
                        case WallDirection.South:
                            otherIsFurtherOut = otherOutwardCoord < wallOutwardCoord;
                            break;
                        case WallDirection.West:
                            otherIsFurtherOut = otherOutwardCoord < wallOutwardCoord;
                            break;
                    }
                    
                    if (otherIsFurtherOut)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Determines which corners are back based on connection to back walls.
        /// A corner is back only if it's connected to a wall that's already determined to be back.
        /// </summary>
        private System.Collections.Generic.HashSet<DetectedCorner> DetermineBackCornersFromPerimeter(System.Collections.Generic.List<DetectedCorner> corners, System.Collections.Generic.HashSet<DetectedWall> backWalls)
        {
            var backCorners = new System.Collections.Generic.HashSet<DetectedCorner>();
            
            // A corner is back only if it's connected to a back wall
            foreach (var corner in corners)
            {
                // Check if this corner is connected to any back wall
                bool isConnectedToBackWall = IsCornerConnectedToBackWalls(corner, backWalls);
                
                if (isConnectedToBackWall)
                {
                    backCorners.Add(corner);
                }
            }
            return backCorners;
        }
        
        /// <summary>
        /// Checks if a corner is connected to any of the back walls
        /// A corner is connected if it shares an endpoint with a back wall
        /// </summary>
        private bool IsCornerConnectedToBackWalls(DetectedCorner corner, System.Collections.Generic.HashSet<DetectedWall> backWalls)
        {
            float tolerance = 0.1f;
            Vector2 cornerPos = new Vector2(corner.position.x, corner.position.z);
            
            foreach (var wall in backWalls)
            {
                Vector2 wallStart = new Vector2(wall.start.x, wall.start.z);
                Vector2 wallEnd = new Vector2(wall.end.x, wall.end.z);
                
                // Check if the corner position is close to either wall endpoint
                bool connectedToStart = Vector2.Distance(cornerPos, wallStart) < tolerance;
                bool connectedToEnd = Vector2.Distance(cornerPos, wallEnd) < tolerance;
                
                if (connectedToStart || connectedToEnd)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Filters corners to only include those connected to back walls
        /// A corner is back only if it's connected to a wall that's already determined to be back
        /// </summary>
        private System.Collections.Generic.List<DetectedCorner> FilterToExteriorCorners(System.Collections.Generic.List<DetectedCorner> corners)
        {
            if (corners.Count == 0) return new System.Collections.Generic.List<DetectedCorner>();
            
            // Get the back walls that were already determined
            // Note: This method should be called after FilterToExteriorWalls
            // For now, we'll assume all corners are connected to back walls
            // The actual filtering will happen in the calling method based on back walls
            
            var exteriorCorners = new System.Collections.Generic.List<DetectedCorner>();
            
            foreach (var corner in corners)
            {
                CornerDirection direction = DetermineCornerDirection(corner.normal);
                
                // All corners are considered exterior for now
                // The actual filtering based on back walls will be done in the calling method
                exteriorCorners.Add(corner);
            }
            
            return exteriorCorners;
        }
        
        
        
        /// <summary>
        /// Compares two DetectedWall structs for equality
        /// </summary>
        private bool AreWallsEqual(DetectedWall wall1, DetectedWall wall2)
        {
            return Vector3.Distance(wall1.center, wall2.center) < 0.001f && 
                   Vector3.Distance(wall1.direction, wall2.direction) < 0.001f &&
                   Mathf.Abs(wall1.length - wall2.length) < 0.001f;
        }
        
        
        /// <summary>
        /// Checks if back override is enabled for the specified wall direction
        /// Walls adjacent to the back corner direction are considered "back"
        /// </summary>
        private bool GetBackOverrideEnabled(WallDirection direction)
        {
            if (currentSettings == null) return false;
            
            // Determine which walls are adjacent to the back corner
            switch (currentSettings.backDirection)
            {
                case BackDirection.NorthEast:
                    return direction == WallDirection.North || direction == WallDirection.East;
                case BackDirection.SouthEast:
                    return direction == WallDirection.South || direction == WallDirection.East;
                case BackDirection.SouthWest:
                    return direction == WallDirection.South || direction == WallDirection.West;
                case BackDirection.NorthWest:
                    return direction == WallDirection.North || direction == WallDirection.West;
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Gets the back override height for the specified wall direction
        /// </summary>
        private float GetBackWallHeight(float roomHeight, WallDirection direction)
        {
            if (currentSettings == null) return roomHeight;
            // Back height is additive: base height + additional back height
            return roomHeight + currentSettings.wallBackHeight;
        }
        
        /// <summary>
        /// Gets the back override width for the specified wall direction
        /// </summary>
        private float GetBackWallWidth(WallDirection direction)
        {
            if (currentSettings == null) return 0.2f;
            return currentSettings.wallWidth;
        }
        
        /// <summary>
        /// Gets the back override depth for the specified wall direction
        /// </summary>
        private float GetBackWallDepth(WallDirection direction)
        {
            if (currentSettings == null) return 0.2f;
            return currentSettings.wallDepth;
        }
        
        
        /// <summary>
        /// Checks if back override is enabled for the specified corner direction
        /// Only the corner matching the backDirection setting is considered "back"
        /// </summary>
        private bool GetBackCornerOverrideEnabled(CornerDirection direction)
        {
            if (currentSettings == null) return false;
            
            // Check if this corner direction matches the back direction
            switch (currentSettings.backDirection)
            {
                case BackDirection.NorthEast:
                    return direction == CornerDirection.NorthEast;
                case BackDirection.SouthEast:
                    return direction == CornerDirection.SouthEast;
                case BackDirection.SouthWest:
                    return direction == CornerDirection.SouthWest;
                case BackDirection.NorthWest:
                    return direction == CornerDirection.NorthWest;
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Gets the back override height for the specified corner direction
        /// </summary>
        private float GetBackCornerHeight(float roomHeight, CornerDirection direction)
        {
            if (currentSettings == null) return roomHeight;
            // Back height is additive: base height + additional back height
            return roomHeight + currentSettings.cornerBackHeight;
        }
        
        /// <summary>
        /// Gets the back override width for the specified corner direction
        /// </summary>
        private float GetBackCornerWidth(CornerDirection direction)
        {
            if (currentSettings == null) return 0.2f;
            return currentSettings.cornerWidth;
        }
        
        /// <summary>
        /// Gets the back override depth for the specified corner direction
        /// </summary>
        private float GetBackCornerDepth(CornerDirection direction)
        {
            if (currentSettings == null) return 0.2f;
            return currentSettings.cornerDepth;
        }
        
        private void OnSelectionChanged()
        {
            // Update status based on selection
            var selectedObject = Selection.activeGameObject;
            if (selectedObject != null)
            {
                var probuilderMesh = selectedObject.GetComponent<ProBuilderMesh>();
                var room = selectedObject.GetComponent<Room>();
                
                if (room != null)
                {
                    UpdateStatus($"Selected: {selectedObject.name} - Room (use Edit/Reset buttons)");
                }
                else if (probuilderMesh != null)
                {
                    UpdateStatus($"Selected: {selectedObject.name} - Ready to build room");
                }
                else
                {
                    UpdateStatus($"Selected: {selectedObject.name} - Not a ProBuilder mesh or room");
                }
            }
            else
            {
                UpdateStatus("No object selected. Please select a ProBuilder cube or room.");
            }

            RefreshSelectionWarning();
            RefreshButtonStates();
        }
    }
}
