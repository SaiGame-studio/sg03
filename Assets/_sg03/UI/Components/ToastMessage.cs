using UnityEngine;
using UnityEngine.UIElements;

namespace SG03.UI.Components
{
    /// <summary>Reusable UI Toolkit toast notifications. Call <see cref="ShowError"/> from any UI code.</summary>
    public static class ToastMessage
    {
        private const string ToastName = "SG03ToastMessage";
        private const int ErrorDurationMilliseconds = 6000;

        private class ToastState
        {
            public int messageVersion;
        }

        public static void ShowError(string message, VisualElement source = null)
        {
            Show(message, "toast-message--error", source, ErrorDurationMilliseconds);
        }

        public static void Show(string message, string styleClass = null, VisualElement source = null, int durationMilliseconds = 3500)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            VisualElement root = GetRoot(source);
            if (root == null) return;

            Label toast = root.Q<Label>(ToastName);
            if (toast == null)
            {
                toast = new Label { name = ToastName };
                toast.AddToClassList("toast-message");
                toast.userData = new ToastState();
                toast.style.position = Position.Absolute;
                toast.style.left = 24;
                toast.style.bottom = 24;
                toast.style.maxWidth = 420;
                toast.style.paddingLeft = 14;
                toast.style.paddingRight = 14;
                toast.style.paddingTop = 10;
                toast.style.paddingBottom = 10;
                toast.style.borderTopLeftRadius = 7;
                toast.style.borderTopRightRadius = 7;
                toast.style.borderBottomLeftRadius = 7;
                toast.style.borderBottomRightRadius = 7;
                toast.style.whiteSpace = WhiteSpace.Normal;
                root.Add(toast);
            }

            toast.RemoveFromClassList("toast-message--error");
            if (!string.IsNullOrWhiteSpace(styleClass)) toast.AddToClassList(styleClass);
            bool isError = styleClass == "toast-message--error";
            toast.style.color = isError ? new Color(1f, 0.75f, 0.75f) : Color.white;
            toast.style.backgroundColor = isError ? new Color(0.32f, 0.11f, 0.14f) : new Color(0.08f, 0.1f, 0.16f);
            toast.style.borderTopColor = isError ? new Color(0.9f, 0.41f, 0.44f) : new Color(0.45f, 0.6f, 0.9f);
            toast.style.borderRightColor = toast.style.borderBottomColor = toast.style.borderLeftColor = toast.style.borderTopColor;
            toast.text = message;
            toast.style.display = DisplayStyle.Flex;

            ToastState state = toast.userData as ToastState ?? new ToastState();
            toast.userData = state;
            int messageVersion = ++state.messageVersion;
            toast.schedule.Execute(() =>
                {
                    if (state.messageVersion == messageVersion) toast.style.display = DisplayStyle.None;
                })
                .StartingIn(durationMilliseconds);
        }

        private static VisualElement GetRoot(VisualElement source)
        {
            if (source?.panel?.visualTree != null) return source.panel.visualTree;

            UIDocument document = Object.FindFirstObjectByType<UIDocument>();
            return document?.rootVisualElement;
        }
    }
}
