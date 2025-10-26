using UnityEngine;
using UnityEditor;

namespace DungeonsForProBuilderEditor
{
    /// <summary>
    /// Custom editor for DoorOperation that handles automatic rebuilding when moved
    /// </summary>
    [CustomEditor(typeof(DungeonsForProBuilder.DoorOperation))]
    public class DoorOperationEditor : Editor
    {
        private Vector3 lastPosition;
        private Quaternion lastRotation;
        private Vector3 lastScale;
        private bool wasManipulating = false;
        private const float POSITION_THRESHOLD = 0.0001f;

        private void OnEnable()
        {
            var doorOp = target as DungeonsForProBuilder.DoorOperation;
            if (doorOp != null)
            {
                lastPosition = doorOp.transform.position;
                lastRotation = doorOp.transform.rotation;
                lastScale = doorOp.transform.localScale;
            }

            // Subscribe to scene view and undo/redo events
            SceneView.duringSceneGui += OnSceneGUI;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            SceneView.duringSceneGui -= OnSceneGUI;
            Undo.undoRedoPerformed -= OnUndoRedo;
            
            // Check if door was moved when selection changes
            CheckForChanges();
        }

        private void OnUndoRedo()
        {
            // Rebuild on undo/redo of transform changes
            var doorOp = target as DungeonsForProBuilder.DoorOperation;
            if (doorOp != null && doorOp.autoRebuildOnMove)
            {
                if (HasTransformChanged(doorOp))
                {
                    UpdateTrackedValues(doorOp);
                    RebuildDoor(doorOp);
                }
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            var doorOp = target as DungeonsForProBuilder.DoorOperation;
            if (doorOp == null || !doorOp.autoRebuildOnMove) return;

            // Check if user is manipulating the transform handles
            bool isManipulating = GUIUtility.hotControl != 0;
            
            // Detect when manipulation ends
            if (wasManipulating && !isManipulating)
            {
                // User just finished manipulating - check if transform changed
                if (HasTransformChanged(doorOp))
                {
                    UpdateTrackedValues(doorOp);
                    RebuildDoor(doorOp);
                }
            }
            
            wasManipulating = isManipulating;
        }
        
        private void CheckForChanges()
        {
            var doorOp = target as DungeonsForProBuilder.DoorOperation;
            if (doorOp != null && doorOp.autoRebuildOnMove)
            {
                if (HasTransformChanged(doorOp))
                {
                    UpdateTrackedValues(doorOp);
                    RebuildDoor(doorOp);
                }
            }
        }
        
        private bool HasTransformChanged(DungeonsForProBuilder.DoorOperation doorOp)
        {
            // Use threshold for position to avoid floating point precision issues
            bool positionChanged = Vector3.Distance(doorOp.transform.position, lastPosition) > POSITION_THRESHOLD;
            bool rotationChanged = Quaternion.Angle(doorOp.transform.rotation, lastRotation) > 0.01f;
            bool scaleChanged = Vector3.Distance(doorOp.transform.localScale, lastScale) > POSITION_THRESHOLD;
            
            return positionChanged || rotationChanged || scaleChanged;
        }
        
        private void UpdateTrackedValues(DungeonsForProBuilder.DoorOperation doorOp)
        {
            lastPosition = doorOp.transform.position;
            lastRotation = doorOp.transform.rotation;
            lastScale = doorOp.transform.localScale;
        }

        private void RebuildDoor(DungeonsForProBuilder.DoorOperation doorOp)
        {
            if (!doorOp.IsValid()) return;

            // Find the dungeon editor window (don't focus it, just get reference)
            var window = EditorWindow.GetWindow<DungeonEditorWindow>(false, "Dungeon Editor", false);
            if (window != null)
            {
                // Use reflection to call the private RebuildDoorOperation method
                var method = typeof(DungeonEditorWindow).GetMethod("RebuildDoorOperation",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (method != null)
                {
                    method.Invoke(window, new object[] { doorOp });
                }
            }
        }
    }
}

