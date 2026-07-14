using UnityEditor;
using UnityEngine.Rendering;

/// <summary>
/// Automatically disables Unity's runtime Rendering Debugger UI
/// whenever the Unity Editor loads or scripts are recompiled.
/// </summary>
[InitializeOnLoad]
public static class DisableRenderingDebugManager
{
    /// <summary>
    /// The static constructor is called automatically by Unity
    /// because this class uses the InitializeOnLoad attribute.
    /// </summary>
    static DisableRenderingDebugManager()
    {
        // Prevents the Rendering Debugger runtime UI from being opened.
        DebugManager.instance.enableRuntimeUI = false;
    }
}