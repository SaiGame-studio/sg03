using System;
using UnityEngine.UIElements;

namespace SG03.UI
{
    // Constrains LobbyViewport to a strict 16:9 aspect ratio, centered inside
    // LobbyRoot. Geometry writes are deferred so we don't mutate layout during
    // UI Toolkit's render-chain rebuild.
    public class LobbyAspectRatioKeeper
    {
        private const float Aspect = 16f / 9f;
        private const float Epsilon = 0.01f;

        private readonly VisualElement lobbyRoot;
        private readonly VisualElement lobbyViewport;

        private bool applyQueued;
        private float appliedWidth = -1f;
        private float appliedHeight = -1f;
        private float appliedLeft = float.MinValue;
        private float appliedTop = float.MinValue;

        public LobbyAspectRatioKeeper(VisualElement lobbyRoot, VisualElement lobbyViewport)
        {
            this.lobbyRoot = lobbyRoot;
            this.lobbyViewport = lobbyViewport;

            this.lobbyRoot.RegisterCallback<GeometryChangedEvent>(_ => this.QueueApply());
            this.QueueApply();
        }

        private void QueueApply()
        {
            if (this.applyQueued) return;
            this.applyQueued = true;

            this.lobbyRoot.schedule.Execute(() =>
            {
                this.applyQueued = false;
                this.ApplyNow();
            });
        }

        private void ApplyNow()
        {
            float sw = this.lobbyRoot.resolvedStyle.width;
            float sh = this.lobbyRoot.resolvedStyle.height;

            if (sw <= 0f || sh <= 0f) return;

            float screenAspect = sw / sh;
            float w;
            float h;

            if (screenAspect > Aspect)
            {
                h = sh;
                w = h * Aspect;
            }
            else
            {
                w = sw;
                h = w / Aspect;
            }

            float left = (sw - w) * 0.5f;
            float top = (sh - h) * 0.5f;

            if (NearlyEqual(this.appliedWidth, w)
                && NearlyEqual(this.appliedHeight, h)
                && NearlyEqual(this.appliedLeft, left)
                && NearlyEqual(this.appliedTop, top))
                return;

            this.appliedWidth = w;
            this.appliedHeight = h;
            this.appliedLeft = left;
            this.appliedTop = top;

            this.lobbyViewport.style.width = w;
            this.lobbyViewport.style.height = h;
            this.lobbyViewport.style.left = left;
            this.lobbyViewport.style.top = top;
        }

        private static bool NearlyEqual(float a, float b)
        {
            return Math.Abs(a - b) <= Epsilon;
        }
    }
}
