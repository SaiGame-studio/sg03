using UnityEditor;
using UnityEngine;

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
            base.OnInspectorGUI();

            EditorGUILayout.Space(6);
            if (GUILayout.Button("Update Parent", GUILayout.Height(28)))
            {
                WorldSpaceHpBarCtrl hpBar = (WorldSpaceHpBarCtrl)this.target;
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

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Refresh UI", GUILayout.Height(28)))
                {
                    ((WorldSpaceHpBarCtrl)this.target).RefreshUi();
                }
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to refresh the runtime UI.", MessageType.Info);
            }
        }
    }
}
