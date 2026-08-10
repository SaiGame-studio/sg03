using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG03.UI.Components
{
    /// <summary>Reusable refresh button matching the Daily Quest toolbar design.</summary>
    public sealed class RefreshButtonComponent
    {
        public Button Button { get; }

        public RefreshButtonComponent(VisualElement host, string name, Action onClick, Sprite icon)
        {
            this.Button = new Button(onClick) { name = name, text = "R", tooltip = "Refresh" };
            this.Button.AddToClassList("dq-refresh-button");
            if (icon != null)
            {
                this.Button.text = string.Empty;
                VisualElement iconElement = new VisualElement();
                iconElement.AddToClassList("refresh-button-component__icon");
                iconElement.style.backgroundImage = new StyleBackground(icon);
                this.Button.Add(iconElement);
            }
            host?.Add(this.Button);
        }
    }
}
