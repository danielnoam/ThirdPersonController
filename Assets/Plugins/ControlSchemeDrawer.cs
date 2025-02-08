using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using System.Linq;

namespace InputBindingSystem
{    
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ControlSchemeReference))]
    public class ControlSchemeDrawer : PropertyDrawer
    {
        // Cache for the total property height
        private float propertyHeight = EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Get the properties
            var inputAssetProp = property.FindPropertyRelative("inputAsset");
            var selectedSchemeProp = property.FindPropertyRelative("selectedScheme");

            // Create layout rects
            Rect fieldRect = position;
            fieldRect.height = EditorGUIUtility.singleLineHeight;

            // Draw the Input Asset field
            EditorGUI.PropertyField(fieldRect, inputAssetProp, new GUIContent("Input Asset"));

            // Move down to next line
            fieldRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Get and validate input asset
            var inputAsset = inputAssetProp.objectReferenceValue as InputActionAsset;
            if (inputAsset != null)
            {
                var schemes = inputAsset.controlSchemes.Select(s => s.name).ToArray();
                if (schemes.Length > 0)
                {
                    int currentIndex = Array.IndexOf(schemes, selectedSchemeProp.stringValue);
                    if (currentIndex < 0) currentIndex = 0;

                    EditorGUI.BeginChangeCheck();
                    var newIndex = EditorGUI.Popup(fieldRect, "Control Scheme", currentIndex, schemes);
                    if (EditorGUI.EndChangeCheck())
                    {
                        selectedSchemeProp.stringValue = schemes[newIndex];
                    }
                }
                else
                {
                    EditorGUI.LabelField(fieldRect, "Control Scheme", "No schemes available");
                }
            }
            else
            {
                EditorGUI.LabelField(fieldRect, "Control Scheme", "Select an Input Asset");
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Return height for two lines plus spacing
            return EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;
        }
    }
#endif

    [Serializable]
    public class ControlSchemeReference
    {
        public InputActionAsset inputAsset;
        public string selectedScheme;

        public string ControlSchemeName => selectedScheme;
    }
}