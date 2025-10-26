using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace DungeonsForProBuilderEditor
{
    /// <summary>
    /// Handles door deletion with proper undo support
    /// Tracks doors and automatically resets them when deleted
    /// </summary>
    [InitializeOnLoad]
    public static class DoorDeletionHandler
    {
        private class DoorData
        {
            public GameObject[] originalWalls;
            public GameObject[] newWallMeshes;
            public GameObject originalDoorMesh;
        }

        private static Dictionary<GameObject, DoorData> trackedDoors = new Dictionary<GameObject, DoorData>();

        static DoorDeletionHandler()
        {
            // Subscribe to events
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private static void OnUndoRedo()
        {
            // Refresh tracking after undo/redo
            trackedDoors.Clear();
            UpdateTracking();
        }

        private static void OnHierarchyChanged()
        {
            UpdateTracking();
        }

        private static void UpdateTracking()
        {
            // Find all current doors
            var allDoors = Object.FindObjectsByType<DungeonsForProBuilder.DoorOperation>(FindObjectsSortMode.None);
            var currentDoors = new HashSet<GameObject>();

            // Track current doors
            foreach (var door in allDoors)
            {
                if (door != null && door.gameObject != null)
                {
                    currentDoors.Add(door.gameObject);
                    
                    if (!trackedDoors.ContainsKey(door.gameObject))
                    {
                        trackedDoors[door.gameObject] = new DoorData
                        {
                            originalWalls = door.originalWalls,
                            newWallMeshes = door.newWallMeshes,
                            originalDoorMesh = door.originalDoorMesh
                        };
                    }
                    else
                    {
                        // Update tracked data
                        trackedDoors[door.gameObject].originalWalls = door.originalWalls;
                        trackedDoors[door.gameObject].newWallMeshes = door.newWallMeshes;
                        trackedDoors[door.gameObject].originalDoorMesh = door.originalDoorMesh;
                    }
                }
            }

            // Check for deleted doors
            var deletedDoors = new List<GameObject>();
            foreach (var kvp in trackedDoors)
            {
                if (!currentDoors.Contains(kvp.Key) || kvp.Key == null)
                {
                    // Door was deleted - reset it
                    ResetDeletedDoor(kvp.Value);
                    deletedDoors.Add(kvp.Key);
                }
            }

            // Clean up deleted entries
            foreach (var door in deletedDoors)
            {
                trackedDoors.Remove(door);
            }
        }

        private static void ResetDeletedDoor(DoorData doorData)
        {
            if (doorData == null) return;

            // Delete new wall meshes
            if (doorData.newWallMeshes != null)
            {
                foreach (var newMesh in doorData.newWallMeshes)
                {
                    if (newMesh != null)
                    {
                        Undo.DestroyObjectImmediate(newMesh);
                    }
                }
            }

            // Re-enable original walls
            if (doorData.originalWalls != null)
            {
                foreach (var wall in doorData.originalWalls)
                {
                    if (wall != null)
                    {
                        Undo.RecordObject(wall, "Delete Door");
                        wall.SetActive(true);
                    }
                }
            }

            // Re-enable the door renderer
            if (doorData.originalDoorMesh != null)
            {
                var doorRenderer = doorData.originalDoorMesh.GetComponent<MeshRenderer>();
                if (doorRenderer != null)
                {
                    Undo.RecordObject(doorRenderer, "Delete Door");
                    doorRenderer.enabled = true;
                }
            }
        }
    }
}


