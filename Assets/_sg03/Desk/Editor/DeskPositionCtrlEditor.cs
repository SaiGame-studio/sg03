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
            if (GUILayout.Button("Toggle Show/Hide Test Cards"))
            {
                Undo.RecordObjects(this.GetTestCardObjects(ctrl), "Toggle Test Cards");
                ctrl.ToggleTestCards();
            }
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
