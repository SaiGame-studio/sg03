using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG03.UI.Components
{
    // ─────────────────────────────────────────────────────────────────────────
    //  PopupMenu
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reusable floating dropdown menu.
    ///
    /// Inspired by how websites handle dropdowns (React portal / position:fixed):
    ///  • The popup is appended directly to the panel root — never to the trigger
    ///    element — so it is never clipped by any parent overflow setting.
    ///  • Position is computed from the trigger's worldBound at show-time.
    ///  • Only one PopupMenu per panel is needed; pass different anchor buttons
    ///    and item lists to reuse for every trigger.  Only one popup can be open
    ///    at a time: showing a new one automatically closes the previous one.
    ///  • Size is determined by content (flex auto-height, min-width 160px).
    ///
    /// Usage:
    /// <code>
    ///   // Create once in BindFromRoot:
    ///   _menu = new PopupMenu(rootVisualElement);
    ///
    ///   // Wire a trigger button:
    ///   btn.RegisterCallback&lt;MouseEnterEvent&gt;(_ => _menu.Show(btn, items));
    ///   btn.RegisterCallback&lt;ClickEvent&gt;(    _ => _menu.Toggle(btn, items));
    ///
    ///   // Items are rebuilt every Show() call, so IsActive can vary:
    ///   PopupMenuItem[] GetItems() => new[]
    ///   {
    ///       new PopupMenuItem { Label = "Option A", IsActive = _current == A,
    ///                           OnClick = () => Select(A) },
    ///       new PopupMenuItem { Label = "Option B", IsActive = _current == B,
    ///                           OnClick = () => Select(B) },
    ///   };
    /// </code>
    /// </summary>
    public sealed class PopupMenu
    {
        // ── Static instance registry — one popup open at a time per panel ──
        // Key: rootVisualElement, Value: currently open PopupMenu on that root
        private static readonly Dictionary<VisualElement, PopupMenu> s_openMenus =
            new Dictionary<VisualElement, PopupMenu>();

        // ── Core elements ─────────────────────────────────────────────────
        private readonly VisualElement _root;
        private readonly VisualElement _container;

        // ── Anchor tracking ───────────────────────────────────────────────
        private VisualElement _anchor;
        private EventCallback<MouseEnterEvent> _anchorEnterCb;
        private EventCallback<MouseLeaveEvent> _anchorLeaveCb;

        // ── Hide scheduling ───────────────────────────────────────────────
        private bool _hidePending;

        // ── State ─────────────────────────────────────────────────────────
        public bool IsVisible { get; private set; }

        // ── Construction ──────────────────────────────────────────────────

        public PopupMenu(VisualElement root)
        {
            _root = root;

            _container = new VisualElement();
            _container.AddToClassList("popup-menu");

            // Mouse inside the popup → cancel any pending hide.
            _container.RegisterCallback<MouseEnterEvent>(_ => CancelHide());

            // Mouse leaving the popup → schedule hide.
            _container.RegisterCallback<MouseLeaveEvent>(_ => ScheduleHide());
        }

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// Show the popup below <paramref name="anchor"/> with the given items.
        /// Rebuilds the item list on every call so IsActive states are fresh.
        /// Any other open popup on the same root is closed first.
        /// </summary>
        public void Show(VisualElement anchor, IEnumerable<PopupMenuItem> items)
        {
            // Close any other open popup on the same root first.
            if (s_openMenus.TryGetValue(_root, out PopupMenu other) && other != this)
                other.HideInternal();

            BuildItems(items);
            AttachToRoot();
            SetAnchor(anchor);
            PositionUnder(anchor);
            IsVisible = true;
            s_openMenus[_root] = this;
        }

        /// <summary>
        /// Toggle: if this popup is already visible for <paramref name="anchor"/>,
        /// hide it; otherwise show it.
        /// </summary>
        public void Toggle(VisualElement anchor, IEnumerable<PopupMenuItem> items)
        {
            if (IsVisible && _anchor == anchor)
                Hide();
            else
                Show(anchor, items);
        }

        /// <summary>Hide the popup immediately.</summary>
        public void Hide()
        {
            HideInternal();
        }

        // ── Private ───────────────────────────────────────────────────────

        private void HideInternal()
        {
            _hidePending = false;
            DetachFromRoot();
            UnsetAnchor();
            IsVisible = false;
            if (s_openMenus.TryGetValue(_root, out PopupMenu current) && current == this)
                s_openMenus.Remove(_root);
        }

        private void BuildItems(IEnumerable<PopupMenuItem> items)
        {
            _container.Clear();
            foreach (PopupMenuItem item in items)
            {
                PopupMenuItem captured = item;   // capture loop variable
                var btn = new Button(() =>
                {
                    HideInternal();
                    captured.OnClick?.Invoke();
                });
                btn.text = captured.Label;
                btn.AddToClassList("popup-menu__item");
                if (captured.IsActive)
                    btn.AddToClassList("popup-menu__item--active");
                _container.Add(btn);
            }
        }

        // ── Root attachment ───────────────────────────────────────────────

        private void AttachToRoot()
        {
            if (!_root.Contains(_container))
                _root.Add(_container);

            // Unregister before register to avoid duplicate listeners.
            _root.UnregisterCallback<PointerDownEvent>(OnPointerDownOutside);
            _root.RegisterCallback<PointerDownEvent>(OnPointerDownOutside);
        }

        private void DetachFromRoot()
        {
            if (_root.Contains(_container))
                _root.Remove(_container);
            _root.UnregisterCallback<PointerDownEvent>(OnPointerDownOutside);
        }

        // ── Anchor hover events ───────────────────────────────────────────

        private void SetAnchor(VisualElement anchor)
        {
            UnsetAnchor();
            _anchor = anchor;

            // Keep the popup open while cursor is over the trigger button.
            _anchorEnterCb = _ => CancelHide();
            _anchorLeaveCb = _ => ScheduleHide();
            _anchor.RegisterCallback(_anchorEnterCb);
            _anchor.RegisterCallback(_anchorLeaveCb);
        }

        private void UnsetAnchor()
        {
            if (_anchor == null) return;
            if (_anchorEnterCb != null) _anchor.UnregisterCallback(_anchorEnterCb);
            if (_anchorLeaveCb != null) _anchor.UnregisterCallback(_anchorLeaveCb);
            _anchor        = null;
            _anchorEnterCb = null;
            _anchorLeaveCb = null;
        }

        // ── Positioning ───────────────────────────────────────────────────

        /// <summary>
        /// Position the container directly below the anchor.
        /// Uses worldBound (panel-space coordinates) so it works regardless of
        /// where in the hierarchy the anchor lives.
        /// </summary>
        private void PositionUnder(VisualElement anchor)
        {
            if (anchor == null) return;

            Rect b = anchor.worldBound;
            if (b.width > 0f)
            {
                // Layout already resolved — position immediately.
                QueueApplyPosition(b.xMin, b.yMax);
                return;
            }

            // Layout not yet resolved — wait for the first geometry pass.
            void OnLayout(GeometryChangedEvent evt)
            {
                anchor.UnregisterCallback<GeometryChangedEvent>(OnLayout);
                if (!IsVisible) return;
                Rect resolved = anchor.worldBound;
                QueueApplyPosition(resolved.xMin, resolved.yMax);
            }
            anchor.RegisterCallback<GeometryChangedEvent>(OnLayout);
        }

        private void QueueApplyPosition(float left, float top)
        {
            _root.schedule.Execute(() =>
            {
                if (!IsVisible) return;
                ApplyPosition(left, top);
            });
        }

        private void ApplyPosition(float left, float top)
        {
            _container.style.left = left;
            _container.style.top  = top;
        }

        // ── Hide scheduling ───────────────────────────────────────────────

        /// <summary>
        /// Schedule hiding after <paramref name="delayMs"/> ms.
        /// The delay gives the mouse time to travel from the anchor into the popup
        /// without a gap causing the popup to disappear prematurely.
        /// </summary>
        private void ScheduleHide(int delayMs = 300)
        {
            if (!IsVisible) return;
            _hidePending = true;
            _container.schedule.Execute(() =>
            {
                if (_hidePending) HideInternal();
            }).ExecuteLater(delayMs);
        }

        private void CancelHide()
        {
            _hidePending = false;
        }

        // ── Click-outside to close ────────────────────────────────────────

        private void OnPointerDownOutside(PointerDownEvent evt)
        {
            var pos = new Vector2(evt.position.x, evt.position.y);

            // Clicks on the anchor itself are handled by Toggle — ignore here.
            if (_anchor != null && _anchor.worldBound.Contains(pos)) return;

            // Clicks inside the popup are handled by item buttons — ignore here.
            if (_container.worldBound.Contains(pos)) return;

            HideInternal();
        }
    }
}
