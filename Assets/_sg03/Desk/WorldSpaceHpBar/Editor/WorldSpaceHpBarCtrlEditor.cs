using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG03.Editor
{
    [CustomEditor(typeof(WorldSpaceHpBarCtrl))]
    public sealed class WorldSpaceHpBarCtrlEditor : UnityEditor.Editor
    {
        private SerializedProperty parentProperty;

        private void OnEnable()
        {
            this.parentProperty = this.serializedObject.FindProperty("parent");
        }

        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();
            DrawPropertiesExcluding(this.serializedObject, "m_Script", "miniMode");
            WorldSpaceHpBarCtrl hpBar = (WorldSpaceHpBarCtrl)this.target;
            if (this.serializedObject.ApplyModifiedProperties()) hpBar.RefreshUi();

            this.DrawRuntimeUiElements(hpBar);
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Display mode", EditorStyles.boldLabel);
            string toggleButtonLabel = this.serializedObject.FindProperty("miniMode").boolValue
                ? "Switch to Full Mode (bar + values)"
                : "Switch to Mini Mode (bar only)";
            if (GUILayout.Button(toggleButtonLabel, GUILayout.Height(28)))
            {
                Undo.RecordObject(hpBar, "Toggle HP Bar Display Mode");
                hpBar.ToggleDisplayMode();
                EditorUtility.SetDirty(hpBar);
            }

            EditorGUILayout.Space(6);
            if (GUILayout.Button("Update Parent", GUILayout.Height(28)))
            {
                Transform parent = this.parentProperty.objectReferenceValue as Transform;
                if (parent == null)
                {
                    hpBar.UpdateParent();
                    return;
                }

                Undo.SetTransformParent(hpBar.transform, parent, "Update HP Bar Parent");
                Undo.RecordObject(hpBar.transform, "Update HP Bar Parent");
                hpBar.UpdateParent();
                EditorUtility.SetDirty(hpBar.transform);
            }

            if (GUILayout.Button("Refresh UI", GUILayout.Height(28)))
            {
                ((WorldSpaceHpBarCtrl)this.target).RefreshUi();
                EditorUtility.SetDirty(this.target);
            }
        }

        private void DrawRuntimeUiElements(WorldSpaceHpBarCtrl hpBar)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Runtime UI Elements", EditorStyles.boldLabel);
            DrawVisualElement("Fill", hpBar.FillElement);
            DrawVisualElement("Health Preview", hpBar.HealthPreviewElement);
            DrawVisualElement("Root", hpBar.RootElement);
            DrawVisualElement("Track", hpBar.TrackElement);
        }

        private static void DrawVisualElement(string label, VisualElement element)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(label, element == null ? "Not bound" : element.name);
            }
        }
    }
}
