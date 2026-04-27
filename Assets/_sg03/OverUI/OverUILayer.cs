namespace SG03
{
    // Shared layer name used by OverUICameraSetup and any 3D object
    // that should render on top of UI Toolkit panels.
    // Assign this layer to the object in the Inspector, then the
    // OverUICameraSetup will automatically cull & render only that layer.
    public static class OverUILayer
    {
        public const string Name = "OverUI";
    }
}
