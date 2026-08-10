using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG03.UI.Components
{
    /// <summary>Reusable refresh button matching the Daily Quest toolbar design.</summary>
    public sealed class RefreshButtonComponent
    {
        public Button Button { get; }

        public RefreshButtonComponent(Button button, Sprite icon)
        {
            this.Button = button;
            if (this.Button == null) return;
            this.Button.AddToClassList("refresh-button");
            // The UXML owns the icon at runtime.  Do not rewrite the button's
            // content here, otherwise UI Toolkit removes that visual element.
            if (icon != null)
            {
                this.Button.text = string.Empty;
                this.Button.Clear();
                this.AddIcon(icon);
            }
        }

        public RefreshButtonComponent(VisualElement host, string name, Action onClick, Sprite icon)
        {
            this.Button = new Button(onClick) { name = name, text = "", tooltip = "Refresh" };
            this.Button.AddToClassList("refresh-button");
            this.AddIcon(icon);
            host?.Add(this.Button);
        }

        private void AddIcon(Sprite icon)
        {
            if (icon == null) { this.Button.text = "R"; return; }
            VisualElement iconElement = new VisualElement();
            iconElement.AddToClassList("refresh-button-component__icon");
            iconElement.style.backgroundImage = new StyleBackground(icon);
            this.Button.Add(iconElement);
        }
    }
}
