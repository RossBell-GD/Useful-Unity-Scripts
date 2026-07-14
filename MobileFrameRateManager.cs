using System.Collections;
using System.Threading;
using UnityEngine;

public class FrameRateManager : MonoBehaviour
{
    // Original Source: https://youtu.be/k1ds7PnTQsE?si=KH9lghRAYRv9aOW4
    // Used to remove the framerate cap for mobile games.
    [Header("Frame Settings")]
    [SerializeField] private int targetFrameRate = 60;
    private void Awake()
    {
        // Disable VSync so Application.targetFrameRate takes control
        QualitySettings.vSyncCount = 0;

        // Set the framerate cap
        Application.targetFrameRate = targetFrameRate;

        // (Optional) prevent device from sleeping
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }
}