using TMPro;
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

            if (GUILayout.Button("Setup Card Text"))
                SetupCardText(card);

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

            // --- Text elements on the front face ---
            SetupCardText(card);

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
            return mat;
        }

        /// <summary>
        /// Creates (or re-configures) all TMP text children on FrontFace and wires them
        /// into the Card3D serialized fields. Safe to call multiple times (idempotent).
        /// </summary>
        private static void SetupCardText(Card3D card)
        {
            Undo.SetCurrentGroupName("Setup Card Text");
            int undoGroup = Undo.GetCurrentGroup();

            // Card is 7.5 x 10.5 world units (750 x 1050 px at 100 PPU).
            // Z = -0.003f keeps text in front of Character (z=0) and Frame (z=-0.001f).
            Transform frontFace = card.transform.Find("FrontFace");
            if (frontFace == null)
            {
                Debug.LogWarning("[Card3DEditor] FrontFace child not found. Run Setup Card Structure first.", card);
                return;
            }

            // Defaults sourced from scene 1-lobby.unity (anchoredPosition, sizeDelta, alignment, wrapping).
            // CardNameText : pos=(0,4.2)      size=(6,0.8)   Left/Middle  wrap=on   overflow=Ellipsis
            // StarsText    : pos=(0.96,4.35)  size=(4,0.6)   Right/Middle wrap=on   overflow=Ellipsis
            // AtkText      : pos=(-2.01,-4.6) size=(2.5,0.5) Left/Middle  wrap=on   overflow=Ellipsis
            // DefText      : pos=(1.84,-4.48) size=(2.5,0.5) Right/Middle wrap=on   overflow=Ellipsis
            // DescriptionText: pos=(0.54,-3.69) size=(6,2.5) Left/Top     wrap=off  overflow=Overflow
            GameObject cardNameGO    = GetOrCreateTMPText("CardNameText",    frontFace, new Vector3( 0f,     4.2f,  -0.003f), 6.0f, 0.8f, 3f, TextAlignmentOptions.Left,    true,  TextOverflowModes.Ellipsis);
            GameObject starsGO       = GetOrCreateTMPText("StarsText",       frontFace, new Vector3( 0.96f,  4.35f, -0.003f), 4.0f, 0.6f, 3f, TextAlignmentOptions.Right,   true,  TextOverflowModes.Ellipsis);
            GameObject atkGO         = GetOrCreateTMPText("AtkText",         frontFace, new Vector3(-2.01f, -4.6f,  -0.003f), 2.5f, 0.5f, 3f, TextAlignmentOptions.Left,    true,  TextOverflowModes.Ellipsis);
            GameObject defGO         = GetOrCreateTMPText("DefText",         frontFace, new Vector3( 1.84f, -4.48f, -0.003f), 2.5f, 0.5f, 3f, TextAlignmentOptions.Right,   true,  TextOverflowModes.Ellipsis);
            GameObject descriptionGO = GetOrCreateTMPText("DescriptionText", frontFace, new Vector3( 0.54f, -3.69f, -0.003f), 6.0f, 2.5f, 3f, TextAlignmentOptions.TopLeft, false, TextOverflowModes.Overflow);

            SerializedObject so = new SerializedObject(card);
            so.FindProperty("cardNameText").objectReferenceValue    = cardNameGO.GetComponent<TextMeshPro>();
            so.FindProperty("starsText").objectReferenceValue       = starsGO.GetComponent<TextMeshPro>();
            so.FindProperty("atkText").objectReferenceValue         = atkGO.GetComponent<TextMeshPro>();
            so.FindProperty("defText").objectReferenceValue         = defGO.GetComponent<TextMeshPro>();
            so.FindProperty("descriptionText").objectReferenceValue = descriptionGO.GetComponent<TextMeshPro>();
            so.ApplyModifiedProperties();

            Undo.CollapseUndoOperations(undoGroup);
            EditorUtility.SetDirty(card);
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

        // Creates or fully reconfigures a world-space TextMeshPro child.
        // Always applies position, RectTransform size, and TMP settings
        // so the method is safe to call again on already-existing objects.
        private static GameObject GetOrCreateTMPText(
            string               objName,
            Transform            parent,
            Vector3              localPosition,
            float                width,
            float                height,
            float                fontSize,
            TextAlignmentOptions alignment,
            bool                 wordWrap,
            TextOverflowModes    overflowMode)
        {
            Transform existing = parent.Find(objName);

            GameObject go;
            TextMeshPro tmp;

            if (existing != null)
            {
                go  = existing.gameObject;
                tmp = go.GetComponent<TextMeshPro>();
                if (tmp == null)
                    tmp = Undo.AddComponent<TextMeshPro>(go);
            }
            else
            {
                go = new GameObject(objName);
                Undo.RegisterCreatedObjectUndo(go, "Create " + objName);
                Undo.SetTransformParent(go.transform, parent, "Parent " + objName);
                tmp = Undo.AddComponent<TextMeshPro>(go);
            }

            // Position & scale
            Undo.RecordObject(go.transform, "Configure " + objName);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale    = Vector3.one;

            // RectTransform size — critical for world-space TMP to render
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                Undo.RecordObject(rt, "Set RectTransform " + objName);
                rt.sizeDelta = new Vector2(width, height);
            }

            // TMP settings
            Undo.RecordObject(tmp, "Configure TMP " + objName);
            tmp.fontSize           = fontSize;
            tmp.alignment          = alignment;
            tmp.textWrappingMode   = wordWrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            tmp.overflowMode       = overflowMode;

            // Raise sorting order so text renders on top of the card quad materials
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Undo.RecordObject(mr, "Set SortingOrder " + objName);
                mr.sortingOrder = 1;
            }

            return go;
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
