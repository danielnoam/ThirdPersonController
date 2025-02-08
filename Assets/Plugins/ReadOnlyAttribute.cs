using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CustomAttribute
{
    
        public class ReadOnlyAttribute : PropertyAttribute 
        {
            // Empty constructor, no parameters needed
            public ReadOnlyAttribute() {}
        }

    #if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
        public class ReadOnlyDrawer : PropertyDrawer
        {
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                GUI.enabled = false;
            
                // Check if it's a list
                if (property.isArray && property.propertyType == SerializedPropertyType.Generic)
                {
                    EditorGUI.PropertyField(position, property, label, true);
                }
                else
                {
                    EditorGUI.PropertyField(position, property, label);
                }
            
                GUI.enabled = true;
            }
        }
    #endif
    
}
