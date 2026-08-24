using UnityEditor;
using UnityEngine;

namespace SG03.Editor
{
    [CustomEditor(typeof(ClientActions))]
    public class ClientActionsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            this.DrawDefaultInspector();
            EditorGUILayout.Space();

            ClientActions clientActions = (ClientActions)this.target;
            string status = clientActions.IsProcessingActions ? "Processing" : "Paused";
            MessageType statusType = clientActions.IsProcessingActions ? MessageType.Info : MessageType.Warning;
            EditorGUILayout.HelpBox("Client action processing: " + status, statusType);

            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            {
                string buttonLabel = clientActions.IsProcessingActions
                    ? "Pause Client Actions"
                    : "Resume Client Actions";

                if (GUILayout.Button(buttonLabel, GUILayout.Height(30f)))
                {
                    clientActions.ToggleActionProcessing();
                    this.Repaint();
                }
            }

            if (!EditorApplication.isPlaying)
                EditorGUILayout.HelpBox("Enter Play Mode to control the client action queue.", MessageType.None);
        }
    }
}
