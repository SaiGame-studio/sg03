using UnityEngine.UIElements;

namespace SG03.UI
{
    // Constrains LobbyViewport to a strict 16:9 aspect ratio, centered inside
    // LobbyRoot. Reacts to panel resize via GeometryChangedEvent so every
    // screen-size change (including Editor Game View rescaling) is handled
    // automatically — no polling required.
    public class LobbyAspectRatioKeeper
    {
        private const float Aspect = 16f / 9f;

        private readonly VisualElement lobbyRoot;
        private readonly VisualElement lobbyViewport;

        public LobbyAspectRatioKeeper(VisualElement lobbyRoot, VisualElement lobbyViewport)
        {
            this.lobbyRoot     = lobbyRoot;
            this.lobbyViewport = lobbyViewport;

            this.lobbyRoot.RegisterCallback<GeometryChangedEvent>(_ => this.Apply());
            this.Apply();
        }

        private void Apply()
        {
            float sw = this.lobbyRoot.resolvedStyle.width;
            float sh = this.lobbyRoot.resolvedStyle.height;

            if (sw <= 0 || sh <= 0) return;

            float screenAspect = sw / sh;
            float w, h;

            if (screenAspect > Aspect)
            {
                // Screen wider than 16:9 → pillarbox (black bars on sides)
                h = sh;
                w = h * Aspect;
            }
            else
            {
                // Screen taller than 16:9 → letterbox (black bars on top/bottom)
                w = sw;
                h = w / Aspect;
            }

            this.lobbyViewport.style.position = Position.Absolute;
            this.lobbyViewport.style.width    = w;
            this.lobbyViewport.style.height   = h;
            this.lobbyViewport.style.left     = (sw - w) * 0.5f;
            this.lobbyViewport.style.top      = (sh - h) * 0.5f;
        }
    }
}
