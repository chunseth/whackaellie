using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// Light/medium/heavy taptic feedback on iOS (via Assets/Plugins/iOS/HapticFeedback.mm).
/// Does nothing in the editor or on other platforms. Players can turn it off with Haptics.Enabled.
/// </summary>
public static class Haptics
{
    public enum Style
    {
        Light = 0,
        Medium = 1,
        Heavy = 2,
        Error = 3
    }

    private const string PrefKey = "WAE_HapticsEnabled";

    public static bool Enabled
    {
        get { return PlayerPrefs.GetInt(PrefKey, 1) == 1; }
        set { PlayerPrefs.SetInt(PrefKey, value ? 1 : 0); }
    }

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void _WAE_PlayHaptic(int style);
#endif

    public static void Play(Style style)
    {
        if (!Enabled) return;
#if UNITY_IOS && !UNITY_EDITOR
        _WAE_PlayHaptic((int)style);
#endif
    }
}
