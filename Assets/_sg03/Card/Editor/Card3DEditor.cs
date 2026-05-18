using UnityEditor;
using UnityEngine;

namespace SG03
{
    [CustomEditor(typeof(Card3D))]
    public class Card3DEditor : UnityEditor.Editor
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

            if (GUILayout.Button("Add SortingGroup"))
                AddSortingGroup(card);

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

        // Assigns a depth-writing, alpha-clip, one-sided (Cull Back) material to each
        // quad renderer. Using SG03/CardCharacter (ZWrite On, AlphaTest queue) ensures
        // every opaque card pixel is written to the depth buffer, so TMP text from any
        // other card that is further from the camera fails ZTest and is properly occluded.
        private static void SetupMaterials(Card3D card)
        {
            SerializedObject so = new SerializedObject(card);
            AssignNewMaterial(so.FindProperty("frontFrameRenderer"), "Card_Frame",     "Assets/_sg03/Card/Image/CardFrame/card_front_char_1.png");
            AssignNewMaterial(so.FindProperty("characterRenderer"),  "Card_Character");
            AssignNewMaterial(so.FindProperty("backRenderer"),       "Card_Back",      "Assets/_sg03/Card/Image/CardFrame/card_back.png");
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(card);
        }

        private static void AssignNewMaterial(SerializedProperty rendererProp, string matName, string texturePath = null)
        {
            if (rendererProp.objectReferenceValue is not Renderer r) return;

            Material mat = CreateCardMaterial(matName);
            if (texturePath != null)
                ApplyTextureToMaterial(mat, texturePath);

            Undo.RegisterCreatedObjectUndo(mat, "Create " + matName);
            Undo.RecordObject(r, "Assign Material " + matName);
            r.sharedMaterial = mat;
        }

        // Loads a Texture2D from the given asset path and sets it as the material's main texture.
        private static void ApplyTextureToMaterial(Material mat, string texturePath)
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (tex == null)
            {
                Debug.LogWarning("[Card3DEditor] Texture not found: " + texturePath);
                return;
            }
            mat.mainTexture = tex;
        }

        // Creates a depth-writing, alpha-clip, one-sided material using SG03/CardCharacter.
        // ZWrite On + AlphaTest queue ensures card quads write to the depth buffer so that
        // TextMeshPro text (Transparent queue, ZTest LEqual) from other cards is occluded.
        // Falls back to Legacy Cutout/Diffuse if the custom shader is not found.
        private static Material CreateCardMaterial(string matName)
        {
            Shader shader = Shader.Find("SG03/CardCharacter");
            if (shader == null)
                shader = Shader.Find("Legacy Shaders/Transparent/Cutout/Diffuse");

            Material mat = new Material(shader);
            mat.name = matName;
            mat.SetFloat("_Cull", 2f); // Cull Back — hide the rear face of each card quad
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

        // Adds a SortingGroup component to the card root if one is not already present.
        // SortingGroup ensures all transparent renderers on this card are sorted as a
        // single unit, preventing text or geometry from other cards from bleeding through.
        private static void AddSortingGroup(Card3D card)
        {
            if (card.GetComponent<UnityEngine.Rendering.SortingGroup>() != null)
            {
                Debug.Log("[Card3DEditor] SortingGroup already exists on this card.", card);
                return;
            }

            Undo.AddComponent<UnityEngine.Rendering.SortingGroup>(card.gameObject);
        }
    }
}
