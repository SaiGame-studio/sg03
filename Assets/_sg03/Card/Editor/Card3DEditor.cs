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

            if (GUILayout.Button("Setup Materials"))
                SetupMaterials(card);

            if (GUILayout.Button("Apply Size"))
            {
                Undo.RecordObject(card.transform, "Apply Card Size");
                foreach (Renderer r in card.GetComponentsInChildren<Renderer>())
                    Undo.RecordObject(r.transform, "Apply Card Size");
                card.ApplySize();
                EditorUtility.SetDirty(card);
            }

            if (GUILayout.Button("Apply Textures"))
            {
                card.ApplyTextures();
                EditorUtility.SetDirty(card);
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

            GUI.enabled = Application.isPlaying;

            if (GUILayout.Button("Show Front"))
                card.ShowFront();

            if (GUILayout.Button("Show Back"))
                card.ShowBack();

            if (GUILayout.Button("Flip"))
                card.Flip();

            GUI.enabled = true;
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

            AutoFillCardDefaults(so);

            card.ApplySize();
            SetupMaterials(card);
            card.ApplyDefaults();

            Undo.CollapseUndoOperations(undoGroup);
        }

        // Assigns the first CardDefaults asset found in the project to the cardDefaults
        // field on Card3D if it is not already set.
        private static void AutoFillCardDefaults(SerializedObject so)
        {
            SerializedProperty prop = so.FindProperty("cardDefaults");
            if (prop.objectReferenceValue != null) return;

            string[] guids = AssetDatabase.FindAssets("t:CardDefaults");
            if (guids.Length == 0) return;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            prop.objectReferenceValue = AssetDatabase.LoadAssetAtPath<CardDefaults>(path);
            so.ApplyModifiedProperties();
        }

        // Assigns a one-sided transparent material (URP Unlit, Cull Back) to each quad
        // renderer so alpha channels work correctly and quads are invisible from behind.
        private static void SetupMaterials(Card3D card)
        {
            SerializedObject so = new SerializedObject(card);
            AssignNewMaterial(so.FindProperty("frontFrameRenderer"), "Card_Frame");
            AssignNewMaterial(so.FindProperty("characterRenderer"),  "Card_Character");
            AssignNewMaterial(so.FindProperty("backRenderer"),       "Card_Back");
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(card);
        }

        private static void AssignNewMaterial(SerializedProperty rendererProp, string matName)
        {
            if (rendererProp.objectReferenceValue is not Renderer r) return;

            Material mat = CreateCardMaterial(matName);
            Undo.RegisterCreatedObjectUndo(mat, "Create " + matName);
            Undo.RecordObject(r, "Assign Material " + matName);
            r.sharedMaterial = mat;
        }

        // Creates a transparent, one-sided (Cull Back) material using URP Unlit.
        // Falls back to Sprites/Default if URP Unlit is unavailable.
        private static Material CreateCardMaterial(string matName)
        {
            Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (urpUnlit == null)
            {
                Material fallback = new Material(Shader.Find("Sprites/Default"));
                fallback.name = matName;
                return fallback;
            }

            Material mat = new Material(urpUnlit);
            mat.name = matName;

            mat.SetFloat("_Surface",      1f);  // Transparent
            mat.SetFloat("_Blend",        0f);  // Alpha
            mat.SetFloat("_Cull",         2f);  // Back
            mat.SetFloat("_ZWrite",       0f);
            mat.SetFloat("_SrcBlend",     5f);  // SrcAlpha
            mat.SetFloat("_DstBlend",    10f);  // OneMinusSrcAlpha
            mat.SetFloat("_SrcBlendAlpha", 1f); // One
            mat.SetFloat("_DstBlendAlpha", 0f); // Zero
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            return mat;
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
