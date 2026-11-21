using UnityEngine;

/// <summary>
/// In release (non-development) builds, only warnings and errors are logged.
/// Debug.Log calls throughout the project are noisy and cost frame time on device.
/// </summary>
public static class LogConfig
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Configure()
    {
        if (!Debug.isDebugBuild)
        {
            Debug.unityLogger.filterLogType = LogType.Warning;
        }
    }
}
