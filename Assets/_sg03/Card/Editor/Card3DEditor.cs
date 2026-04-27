using UnityEditor;
using UnityEngine;

namespace SG03
{
    [CustomEditor(typeof(Card3D))]
    public class Card3DEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Card3D card = (Card3D)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Card Setup", EditorStyles.boldLabel);

            if (GUILayout.Button("Setup Card Structure"))
                SetupCardStructure(card);

            if (GUILayout.Button("Apply Textures"))
            {
                card.ApplyTextures();
                EditorUtility.SetDirty(card);
            }

            if (!Application.isPlaying) return;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

            if (GUILayout.Button("Show Front"))
                card.ShowFront();

            if (GUILayout.Button("Show Back"))
                card.ShowBack();

            if (GUILayout.Button("Flip"))
                card.Flip();
        }

        // ─── Setup helpers ────────────────────────────────────────────────────────

        /// <summary>
        /// Builds the FrontFace / BackFace child hierarchy and wires the serialized
        /// Renderer references. Safe to call multiple times (idempotent).
        /// </summary>
        private static void SetupCardStructure(Card3D card)
        {
            Undo.SetCurrentGroupName("Setup Card Structure");
            int undoGroup = Undo.GetCurrentGroup();

            // --- Front face: Character (background) + Frame (transparent overlay) ---
            Transform frontFace   = GetOrCreateChild(card.transform, "FrontFace");
            GameObject characterGO = GetOrCreateQuad("Character", frontFace);
            GameObject frameGO     = GetOrCreateQuad("Frame",     frontFace);

            // Offset frame slightly forward to avoid z-fighting with the character quad.
            Undo.RecordObject(frameGO.transform, "Offset Frame quad");
            frameGO.transform.localPosition = new Vector3(0f, 0f, -0.001f);

            // --- Back face: rotated 180° on Y so it faces away from the camera ---
            // If the back texture appears mirrored, flip it horizontally in Texture Import Settings.
            Transform backFace = GetOrCreateChild(card.transform, "BackFace");
            Undo.RecordObject(backFace, "Rotate BackFace");
            backFace.localEulerAngles = new Vector3(0f, 180f, 0f);

            GameObject backGO = GetOrCreateQuad("Back", backFace);

            // Wire the renderers into the serialized fields via SerializedObject so
            // the assignment is visible in the Inspector and supports undo.
            SerializedObject so = new SerializedObject(card);
            so.FindProperty("characterRenderer").objectReferenceValue  = characterGO.GetComponent<Renderer>();
            so.FindProperty("frontFrameRenderer").objectReferenceValue = frameGO.GetComponent<Renderer>();
            so.FindProperty("backRenderer").objectReferenceValue       = backGO.GetComponent<Renderer>();
            so.ApplyModifiedProperties();

            Undo.CollapseUndoOperations(undoGroup);
        }

        private static Transform GetOrCreateChild(Transform parent, string childName)
        {
            Transform existing = parent.Find(childName);
            if (existing != null) return existing;

            GameObject go = new GameObject(childName);
            Undo.RegisterCreatedObjectUndo(go, "Create " + childName);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static GameObject GetOrCreateQuad(string quadName, Transform parent)
        {
            Transform existing = parent.Find(quadName);
            if (existing != null) return existing.gameObject;

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = quadName;
            Undo.RegisterCreatedObjectUndo(quad, "Create " + quadName);

            // Remove the auto-generated MeshCollider — card face quads do not need physics.
            MeshCollider col = quad.GetComponent<MeshCollider>();
            if (col != null)
                Undo.DestroyObjectImmediate(col);

            Undo.SetTransformParent(quad.transform, parent, "Parent " + quadName);
            quad.transform.localPosition = Vector3.zero;
            quad.transform.localRotation = Quaternion.identity;
            quad.transform.localScale    = Vector3.one;

            return quad;
        }
    }
}
