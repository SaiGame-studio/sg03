using SG03;
using UnityEditor;
using UnityEngine;

namespace SG03.Editor
{
    [CustomEditor(typeof(DeskPositionCtrl))]
    public class DeskPositionCtrlEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DeskPositionCtrl ctrl = (DeskPositionCtrl)this.target;

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Show Test Cards"))
            {
                Undo.RecordObjects(this.GetTestCardObjects(ctrl), "Show Test Cards");
                ctrl.ShowTestCards();
            }
            if (GUILayout.Button("Hide Test Cards"))
            {
                Undo.RecordObjects(this.GetTestCardObjects(ctrl), "Hide Test Cards");
                ctrl.HideTestCards();
            }
            EditorGUILayout.EndHorizontal();
        }

        private Object[] GetTestCardObjects(DeskPositionCtrl ctrl)
        {
            if (ctrl.TestCards == null) return new Object[0];
            Object[] objects = new Object[ctrl.TestCards.Count];
            for (int i = 0; i < ctrl.TestCards.Count; i++)
                objects[i] = ctrl.TestCards[i].gameObject;
            return objects;
        }
    }
}
