using UnityEditor;
using UnityEngine;

namespace SQT
{
    [CustomPropertyDrawer(typeof(DisplacementSettings))]
    public class DisplacementSettingsEditor : PropertyDrawer
    {
        string[] properties = new string[]
        {
            "computeShader",
            "seed",
            "strength",
            "frequency",
            "lacunarity",
            "persistence",
            "octaves"
        };

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.isExpanded)
            {
                return EditorGUIUtility.singleLineHeight * (properties.Length + 1) + 2 * properties.Length;
            }
            else
            {
                return EditorGUIUtility.singleLineHeight;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                for (int i = 0; i < properties.Length; i++)
                {
                    Rect computeShaderRect = new Rect(position.x, position.y + (EditorGUIUtility.singleLineHeight + 2) * (i + 1), position.width, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(computeShaderRect, property.FindPropertyRelative(properties[i]));
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }
    }
}
