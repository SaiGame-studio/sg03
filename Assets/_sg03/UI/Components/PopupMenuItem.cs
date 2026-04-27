using System;

namespace SG03.UI.Components
{
    /// <summary>One entry in a <see cref="PopupMenu"/>.</summary>
    public sealed class PopupMenuItem
    {
        public string Label    { get; set; }
        public Action OnClick  { get; set; }
        public bool   IsActive { get; set; }
    }
}
