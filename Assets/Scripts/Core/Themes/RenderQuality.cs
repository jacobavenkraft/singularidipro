namespace Singularidi.Themes
{
    /// <summary>
    /// Render-quality tier for the visualizer. Drives adaptive LOD and effect budgets.
    /// </summary>
    public enum RenderQuality
    {
        /// <summary>Interactive playback. Honors `OnsetDensityWindows` lookahead to cap visible-note count under load.</summary>
        Realtime,

        /// <summary>Same shape as Realtime with more permissive thresholds. For capable hardware.</summary>
        RealtimeHQ,

        /// <summary>Offline export. Renders every visible note unconditionally; frame budget is irrelevant.</summary>
        Offline
    }
}
