#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.InputSystem;

namespace InputBindingSystem
{
    [CustomPropertyDrawer(typeof(BindingDropdownAttribute))]
    public class BindingDropdownDrawer : PropertyDrawer
    {
        private GUIContent[] m_BindingOptions;
        private string[] m_BindingOptionValues;
        private int m_SelectedBindingOption = -1;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var component = property.serializedObject.targetObject as IInputActionReferenceProvider;
            if (component == null)
            {
                EditorGUI.HelpBox(position, "Target script must implement IInputActionReferenceProvider", MessageType.Warning);
                return;
            }

            var action = component.actionReference?.action;
            if (action == null)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            // Refresh binding options if needed
            if (m_BindingOptions == null || m_BindingOptions.Length == 0)
            {
                RefreshBindingOptions(action, property);
            }

            // Draw the dropdown
            EditorGUI.BeginProperty(position, label, property);

            var newSelectedBinding = EditorGUI.Popup(position, new GUIContent(label.text), m_SelectedBindingOption, m_BindingOptions);

            if (newSelectedBinding != m_SelectedBindingOption && 
                newSelectedBinding >= 0 && 
                newSelectedBinding < m_BindingOptionValues.Length)
            {
                property.stringValue = m_BindingOptionValues[newSelectedBinding];
                m_SelectedBindingOption = newSelectedBinding;
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndProperty();
        }

        private void RefreshBindingOptions(InputAction action, SerializedProperty property)
        {
            var bindings = action.bindings;
            var bindingCount = bindings.Count;

            m_BindingOptions = new GUIContent[bindingCount];
            m_BindingOptionValues = new string[bindingCount];
            m_SelectedBindingOption = -1;

            var currentBindingId = property.stringValue;

            for (var i = 0; i < bindingCount; ++i)
            {
                var binding = bindings[i];
                var bindingId = binding.id.ToString();
                var haveBindingGroups = !string.IsNullOrEmpty(binding.groups);

                var displayOptions = InputBinding.DisplayStringOptions.DontUseShortDisplayNames | 
                                   InputBinding.DisplayStringOptions.IgnoreBindingOverrides;

                if (!haveBindingGroups)
                    displayOptions |= InputBinding.DisplayStringOptions.DontOmitDevice;

                var displayString = action.GetBindingDisplayString(i, displayOptions);

                if (binding.isPartOfComposite)
                    displayString = $"{ObjectNames.NicifyVariableName(binding.name)}: {displayString}";

                displayString = displayString.Replace('/', '\\');

                if (haveBindingGroups)
                {
                    var asset = action.actionMap?.asset;
                    if (asset != null)
                    {
                        var controlSchemes = string.Join(", ",
                            binding.groups.Split(InputBinding.Separator)
                                .Select(x => asset.controlSchemes.FirstOrDefault(c => c.bindingGroup == x).name));

                        displayString = $"{displayString} ({controlSchemes})";
                    }
                }

                m_BindingOptions[i] = new GUIContent(displayString);
                m_BindingOptionValues[i] = bindingId;

                if (currentBindingId == bindingId)
                    m_SelectedBindingOption = i;
            }
        }
    }

    [CustomPropertyDrawer(typeof(ControlSchemeInfoAttribute))]
    public class ControlSchemeInfoDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var component = property.serializedObject.targetObject as IInputActionReferenceProvider;
            if (component == null)
            {
                EditorGUI.HelpBox(position, "Target script must implement IInputActionReferenceProvider", MessageType.Warning);
                return;
            }

            var actionReference = component.actionReference;
            var action = actionReference?.action;

            if (action?.actionMap?.asset == null)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var controlSchemes = action.actionMap.asset.controlSchemes;
            if (controlSchemes.Count == 0)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            // Display control scheme information
            EditorGUI.BeginProperty(position, label, property);

            var schemeInfoRect = position;
            schemeInfoRect.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.PropertyField(schemeInfoRect, property, label);

            using (new EditorGUI.IndentLevelScope())
            {
                foreach (var scheme in controlSchemes)
                {
                    position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.LabelField(position, scheme.name, EditorStyles.miniLabel);
                }
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var component = property.serializedObject.targetObject as IInputActionReferenceProvider;
            if (component?.actionReference?.action?.actionMap?.asset == null)
                return EditorGUIUtility.singleLineHeight;

            var controlSchemes = component.actionReference.action.actionMap.asset.controlSchemes;
            var lines = controlSchemes.Count + 1; // +1 for the main property field

            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * lines;
        }
    }
}
#endif