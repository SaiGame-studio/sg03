using UnityEditor;
using UnityEngine;

namespace SG03.Editor
{
    [CustomPropertyDrawer(typeof(CardDefinitionData))]
    public class CardDefinitionDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty itemCodeProp = property.FindPropertyRelative("item_code");
            string itemCode = itemCodeProp != null ? itemCodeProp.stringValue : string.Empty;
            string displayLabel = string.IsNullOrEmpty(itemCode) ? label.text : itemCode;

            EditorGUI.PropertyField(position, property, new GUIContent(displayLabel), true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
