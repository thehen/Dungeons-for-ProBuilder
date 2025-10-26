using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEditor;

namespace DungeonsForProBuilderEditor
{
    public static class ProBuilderBooleanUtility
    {
        /// <summary>
        /// Check if ProBuilder experimental features are enabled
        /// </summary>
        public static bool IsExperimentalFeaturesEnabled()
        {
#if PROBUILDER_EXPERIMENTAL_FEATURES
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// Perform boolean subtraction between two ProBuilder meshes using
        /// ProBuilder's internal BooleanEditor.MenuSubtract via reflection.
        /// Returns the resulting GameObject or null if it fails.
        /// </summary>
        public static GameObject PerformBooleanSubtraction(ProBuilderMesh target, ProBuilderMesh cutter)
        {
#if PROBUILDER_EXPERIMENTAL_FEATURES
            try
            {
                // 1. Get the internal BooleanEditor type from the Unity.ProBuilder.Editor assembly
                var type = Type.GetType("UnityEditor.ProBuilder.BooleanEditor, Unity.ProBuilder.Editor", throwOnError: false);
                if (type == null)
                {
                    return null;
                }

                // 2. Get the MenuSubtract method (public or non-public, static)
                var method = type.GetMethod(
                    "MenuSubtract",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
                );

                if (method == null)
                {
                    return null;
                }

                // 3. Invoke MenuSubtract(target, cutter)
                var result = method.Invoke(null, new object[] { target, cutter });

                // If BooleanEditor behaved as expected, it should have created a new result mesh
                // and set it as the active selection. We can read that if needed.
                if (Selection.activeGameObject != null)
                {
                    return Selection.activeGameObject;
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
#else
            return null;
#endif
        }

        /// <summary>
        /// Check if two ProBuilder meshes are overlapping
        /// </summary>
        public static bool AreMeshesOverlapping(ProBuilderMesh mesh1, ProBuilderMesh mesh2)
        {
            var bounds1 = mesh1.GetComponent<Renderer>().bounds;
            var bounds2 = mesh2.GetComponent<Renderer>().bounds;

            return bounds1.Intersects(bounds2);
        }

        /// <summary>
        /// Get all ProBuilder meshes that overlap with the given mesh
        /// </summary>
        public static ProBuilderMesh[] GetOverlappingMeshes(ProBuilderMesh referenceMesh, ProBuilderMesh[] candidateMeshes)
        {
            var overlapping = new System.Collections.Generic.List<ProBuilderMesh>();

            foreach (var mesh in candidateMeshes)
            {
                if (mesh != referenceMesh && AreMeshesOverlapping(referenceMesh, mesh))
                {
                    overlapping.Add(mesh);
                }
            }

            return overlapping.ToArray();
        }
    }
}