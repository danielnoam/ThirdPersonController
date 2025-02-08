using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CustomAttribute
{
    [System.Serializable]
    public class SceneField
    {
        [SerializeField]
        private Object m_SceneAsset;
        [SerializeField]
        private string m_SceneName = "";
        [SerializeField]
        private string m_ScenePath = "";

        public string SceneName => m_SceneName;
        public string ScenePath => m_ScenePath;

        public int BuildIndex
        {
            get
            {
                if (string.IsNullOrEmpty(m_SceneName))
                {
                    Debug.LogWarning("Scene name is empty!");
                    return -1;
                }

                #if UNITY_EDITOR
                // Get all scenes in build settings
                EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
                
                for (int i = 0; i < scenes.Length; i++)
                {

                    if (scenes[i].enabled && scenes[i].path == m_ScenePath)
                    {
                        return i;
                    }
                }
                Debug.LogWarning($"Scene '{m_SceneName}' not found in build settings or is disabled!");
                return -1;
                #else
                int buildIndex = SceneUtility.GetBuildIndexByScenePath(m_ScenePath);
                if (buildIndex == -1)
                {
                    Debug.LogWarning($"Runtime: Scene '{m_ScenePath}' not found in build settings!");
                }
                return buildIndex;
                #endif
            }
        }

        public static implicit operator string(SceneField sceneField)
        {
            return sceneField.SceneName;
        }

        public static implicit operator int(SceneField sceneField)
        {
            return sceneField.BuildIndex;
        }
    }

    #if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(SceneField))]
    public class SceneFieldPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, GUIContent.none, property);
            SerializedProperty sceneAsset = property.FindPropertyRelative("m_SceneAsset");
            SerializedProperty sceneName = property.FindPropertyRelative("m_SceneName");
            SerializedProperty scenePath = property.FindPropertyRelative("m_ScenePath");
            
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            
            if (sceneAsset != null)
            {
                EditorGUI.BeginChangeCheck();
                sceneAsset.objectReferenceValue = EditorGUI.ObjectField(position, sceneAsset.objectReferenceValue, typeof(SceneAsset), false);
                
                if (EditorGUI.EndChangeCheck())
                {
                    if (sceneAsset.objectReferenceValue != null)
                    {
                        SceneAsset scene = sceneAsset.objectReferenceValue as SceneAsset;
                        sceneName.stringValue = scene.name;
                        scenePath.stringValue = AssetDatabase.GetAssetPath(scene);
                        
                        
                        // Validate if scene is in build settings
                        bool sceneInBuild = false;
                        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
                        
                        foreach (var buildScene in scenes)
                        {
                            if (buildScene.path == scenePath.stringValue)
                            {
                                sceneInBuild = true;
                                break;
                            }
                        }
                        
                        if (!sceneInBuild)
                        {
                            Debug.LogWarning($"Scene '{sceneName.stringValue}' is not in build settings! Please add it to your build settings.");
                        }
                    }
                    else
                    {
                        sceneName.stringValue = "";
                        scenePath.stringValue = "";
                    }
                }
            }
            EditorGUI.EndProperty();
        }
    }
    #endif
}