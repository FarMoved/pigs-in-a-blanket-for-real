using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Disables the scene's main camera and its Audio Listener once the player's camera is active.
/// This avoids the "no cameras rendering" flash at startup (scene camera stays on until
/// the local player spawns and their camera is enabled).
/// Only runs in the game scene so the lobby camera is never touched.
/// If the Editor Game view shows "No cameras rendering" and blocks clicks: use the Game tab
/// menu (three dots) and uncheck "Warn if no camera rendering".
/// </summary>
[RequireComponent(typeof(Camera))]
public class DisableSceneCamera : MonoBehaviour
{
    [Tooltip("Scene name where this script should run (e.g. game scene). Ignored in all other scenes.")]
    [SerializeField] private string onlyRunInScene = "ParkPicnic";

    private Camera _cam;
    private AudioListener _listener;
    private bool _disabled;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        _listener = GetComponent<AudioListener>();
    }

    private void LateUpdate()
    {
        if (_disabled) return;

        // Only run in the game scene; never disable the lobby (or other) camera.
        if (string.IsNullOrEmpty(onlyRunInScene) || gameObject.scene.name != onlyRunInScene)
            return;

        // Disable once another camera is active (e.g. player's camera after spawn).
        // This avoids "no cameras rendering" at startup.
        foreach (Camera c in Camera.allCameras)
        {
            if (c != _cam && c.enabled)
            {
                _cam.enabled = false;
                if (_listener != null)
                    _listener.enabled = false;
                _disabled = true;
                return;
            }
        }
    }
}
