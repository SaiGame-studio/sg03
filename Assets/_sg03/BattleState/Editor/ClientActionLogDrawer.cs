using UnityEditor;
using UnityEngine;

namespace SG03.Editor
{
    [CustomPropertyDrawer(typeof(ClientActionLog))]
    public class ClientActionLogDrawer : PropertyDrawer
    {
        private const float DotSize    = 10f;
        private const float DotPadding = 4f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUI.GetPropertyHeight(property, label, true);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty executedProp = property.FindPropertyRelative("executed");
            bool executed = executedProp != null && executedProp.boolValue;

            // Draw dot on the header row (first line only)
            Rect headerLine = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            // Reserve space at the right end of the foldout label for the dot
            float dotX = headerLine.xMax - DotSize - DotPadding;
            float dotY = headerLine.y + (headerLine.height - DotSize) * 0.5f;
            Rect dotRect = new Rect(dotX, dotY, DotSize, DotSize);

            // Draw the default property field normally
            EditorGUI.PropertyField(position, property, label, true);

            // Overlay the dot on top (drawn after so it sits above the foldout arrow area)
            Color dotColor = executed
                ? new Color(0.20f, 0.65f, 1.00f)   // blue  - executed
                : new Color(0.35f, 0.35f, 0.35f);   // grey  - pending

            DrawDot(dotRect, dotColor);
        }

        private static void DrawDot(Rect rect, Color color)
        {
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, GetDotTexture(), ScaleMode.ScaleToFit, true);
            GUI.color = prev;
        }

        private static Texture2D dotTexture;

        private static Texture2D GetDotTexture()
        {
            if (dotTexture != null) return dotTexture;

            int size = 32;
            dotTexture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            dotTexture.hideFlags = HideFlags.HideAndDontSave;
            float center = size * 0.5f;
            float radius = size * 0.5f - 1f;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            dotTexture.SetPixels(pixels);
            dotTexture.Apply();
            return dotTexture;
        }
    }
}
