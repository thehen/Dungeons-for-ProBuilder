using UnityEngine;
using UnityEngine.ProBuilder;

namespace DungeonsForProBuilder
{
    /// <summary>
    /// Component that tracks door operations for reset functionality
    /// </summary>
    public class DoorOperation : MonoBehaviour
    {
        [Header("Door Operation Data")]
        [Tooltip("The original door mesh that was used for the boolean operation")]
        public GameObject originalDoorMesh;
        
        [Tooltip("The room this door operation belongs to")]
        public GameObject room;
        
        [Tooltip("Original walls that were disabled during door creation")]
        public GameObject[] originalWalls;
        
        [Tooltip("New wall meshes created from boolean operations")]
        public GameObject[] newWallMeshes;
        
        [Header("Auto-Rebuild Settings")]
        [Tooltip("Automatically rebuild door when moved")]
        public bool autoRebuildOnMove = true;
        
        /// <summary>
        /// Reset the door operation by deleting new meshes and re-enabling original walls
        /// Note: Deletion handling is done by DoorDeletionHandler in the editor
        /// </summary>
        public void ResetDoorOperation()
        {
            // Delete new wall meshes
            if (newWallMeshes != null)
            {
                foreach (var newMesh in newWallMeshes)
                {
                    if (newMesh != null)
                    {
                        DestroyImmediate(newMesh);
                    }
                }
                newWallMeshes = null;
            }
            
            // Re-enable original walls
            if (originalWalls != null)
            {
                foreach (var wall in originalWalls)
                {
                    if (wall != null)
                    {
                        wall.SetActive(true);
                    }
                }
                originalWalls = null;
            }
            
            // Re-enable the door renderer if it exists
            if (originalDoorMesh != null)
            {
                var doorRenderer = originalDoorMesh.GetComponent<MeshRenderer>();
                if (doorRenderer != null)
                {
                    doorRenderer.enabled = true;
                }
            }
            
            // Remove the door operation component
            DestroyImmediate(this);
        }
        
        /// <summary>
        /// Check if this door operation is valid
        /// </summary>
        public bool IsValid()
        {
            // A door is valid if it has the original mesh reference
            // Note: room field is no longer used since doors can span multiple rooms
            return originalDoorMesh != null;
        }
    }
}

