using UnityEditor;
using UnityEngine;
using Moths.Inputs.Attributes;

namespace Moths.Inputs.Editor.Attributes
{

    [CustomPropertyDrawer(typeof(Inputs.Attributes.ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position,
                                   SerializedProperty property,
                                   GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
}