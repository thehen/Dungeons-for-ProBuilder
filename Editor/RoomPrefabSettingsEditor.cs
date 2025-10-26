using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace DungeonsForProBuilderEditor
{
    [CustomEditor(typeof(DungeonsForProBuilder.RoomPrefabSettings))]
    public class RoomPrefabSettingsEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            try
            {
                // Load UXML
                var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Packages/com.newfangled.dungeons-for-probuilder/Editor/RoomPrefabSettingsEditor.uxml");
                
                if (visualTree == null)
                {
                    return CreateFallbackGUI();
                }
                
                var rootElement = visualTree.CloneTree();
                
                // Load USS
                var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Packages/com.newfangled.dungeons-for-probuilder/Editor/RoomPrefabSettingsEditor.uss");
                if (styleSheet != null)
                {
                    rootElement.styleSheets.Add(styleSheet);
                }
                
                // Bind properties
                rootElement.Bind(serializedObject);
                
                // Setup override toggle callbacks
                try
                {
                    SetupOverrideToggles(rootElement);
                }
                catch (System.Exception)
                {
                }
                
                // Setup mode dropdown callbacks
                try
                {
                    SetupModeDropdowns(rootElement);
                }
                catch (System.Exception)
                {
                }
                
                return rootElement;
            }
            catch (System.Exception)
            {
                return CreateFallbackGUI();
            }
        }

        private VisualElement CreateFallbackGUI()
        {
            var rootElement = new VisualElement();
            rootElement.Add(new Label("Error loading room prefab settings. Please check the UXML file."));
            return rootElement;
        }

        private void SetupOverrideToggles(VisualElement root)
        {
            // Find all override toggles and setup their visibility callbacks
            var overrideToggles = root.Query<Toggle>().Where(t => t.bindingPath.Contains("FurthestOverride")).ToList();
            
            foreach (var toggle in overrideToggles)
            {
                if (toggle == null) continue;
                
                // Find the size container and size label that are siblings of this toggle (same parent)
                var sizeContainer = toggle.parent.Q<VisualElement>(className: "size-container");
                var sizeLabel = toggle.parent.Q<Label>(className: "size-label");
                
                if (sizeContainer != null)
                {
                    toggle.RegisterValueChangedCallback(evt => 
                    {
                        sizeContainer.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                        if (sizeLabel != null)
                        {
                            sizeLabel.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                        }
                        serializedObject.ApplyModifiedProperties();
                    });
                    
                    // Set initial state - delay to ensure toggle is properly initialized
                    root.schedule.Execute(() => 
                    {
                        try
                        {
                            if (toggle != null && sizeContainer != null)
                            {
                                sizeContainer.style.display = toggle.value ? DisplayStyle.Flex : DisplayStyle.None;
                                if (sizeLabel != null)
                                {
                                    sizeLabel.style.display = toggle.value ? DisplayStyle.Flex : DisplayStyle.None;
                                }
                            }
                        }
                        catch (System.Exception)
                        {
                        }
                    }).StartingIn(0);
                }
            }
        }

        private void SetupModeDropdowns(VisualElement root)
        {
            // Setup mode dropdown callbacks for all size fields
            var modeDropdowns = root.Query<EnumField>().Where(d => d.bindingPath.Contains("Mode")).ToList();
            
            foreach (var dropdown in modeDropdowns)
            {
                if (dropdown == null) 
                {
                    continue;
                }
                
                // Find the corresponding size field by searching through all FloatFields
                var sizeFieldPath = dropdown.bindingPath.Replace("Mode", "");
                var sizeFields = root.Query<FloatField>().Where(f => f.bindingPath == sizeFieldPath).ToList();
                var sizeField = sizeFields.Count > 0 ? sizeFields[0] : null;
                
                if (sizeField != null)
                {
                    dropdown.RegisterValueChangedCallback(evt => 
                    {
                        // Enable/disable the size field based on mode (2 = Custom)
                        sizeField.SetEnabled(evt.newValue.GetHashCode() == 2);
                    });
                    
                    // Set initial state - delay to ensure dropdown is properly initialized
                    root.schedule.Execute(() => 
                    {
                        try
                        {
                            if (dropdown != null && sizeField != null)
                            {
                                sizeField.SetEnabled(dropdown.value.GetHashCode() == 2);
                            }
                        }
                        catch (System.Exception)
                        {
                        }
                    }).StartingIn(0);
                }
            }
        }
    }
}