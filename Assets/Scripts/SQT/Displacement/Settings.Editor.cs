using UnityEditor;
using UnityEngine;

namespace SQT.Displacement
{
    [CustomPropertyDrawer(typeof(Settings))]
    public class SettingsEditor : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.isExpanded)
            {
                switch (property.FindPropertyRelative("displacementType").enumValueIndex)
                {
                    case (int)Settings.DisplacementType.None:
                        return GetHeightNone();
                    case (int)Settings.DisplacementType.Perlin:
                        return GetHeightPerlin();
                }
            }
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect currentPosition = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(currentPosition, property.isExpanded, label);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                currentPosition.y += EditorGUIUtility.singleLineHeight + 2;
                EditorGUI.PropertyField(currentPosition, property.FindPropertyRelative("displacementType"));

                switch (property.FindPropertyRelative("displacementType").enumValueIndex)
                {
                    case (int)Settings.DisplacementType.None:
                        OnGUINone();
                        break;
                    case (int)Settings.DisplacementType.Perlin:
                        OnGUIPerlin(currentPosition, property);
                        break;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        void OnGUINone()
        {
        }

        int GetHeightNone()
        {
            return (int)EditorGUIUtility.singleLineHeight * 2 + 2;
        }

        void OnGUIPerlin(Rect currentPosition, SerializedProperty property)
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

            for (int i = 0; i < properties.Length; i++)
            {
                currentPosition.y += EditorGUIUtility.singleLineHeight + 2;
                Rect computeShaderRect = new Rect(currentPosition.x, currentPosition.y, currentPosition.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(computeShaderRect, property.FindPropertyRelative(properties[i]));
            }
        }

        int GetHeightPerlin()
        {
            return (int)EditorGUIUtility.singleLineHeight * (8 + 1) + 2 * 8;
        }
    }
}
