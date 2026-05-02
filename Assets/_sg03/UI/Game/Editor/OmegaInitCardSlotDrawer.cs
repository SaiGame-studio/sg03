using UnityEditor;
using UnityEngine;

namespace SG03.UI
{
    [CustomPropertyDrawer(typeof(OmegaInitCardSlot))]
    public class OmegaInitCardSlotDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty codeName = property.FindPropertyRelative("item_code_name");
            string displayKey = codeName != null && !string.IsNullOrEmpty(codeName.stringValue)
                ? codeName.stringValue
                : label.text;

            label.text = displayKey;
            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
