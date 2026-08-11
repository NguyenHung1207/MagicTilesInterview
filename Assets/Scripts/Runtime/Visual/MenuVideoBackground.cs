using UnityEngine;
using UnityEngine.Video;

[DisallowMultipleComponent]
public sealed class MenuVideoBackground : MonoBehaviour
{
    [SerializeField] private VideoClip videoClip;
    [SerializeField] private Camera targetCamera;

    private void Awake()
    {
        if (videoClip == null)
        {
            Debug.LogError("MenuVideoBackground requires a VideoClip.", this);
            enabled = false;
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            Debug.LogError("MenuVideoBackground requires a target Camera.", this);
            enabled = false;
            return;
        }

        VideoPlayer videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = true;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = true;
        videoPlayer.renderMode = VideoRenderMode.CameraFarPlane;
        videoPlayer.aspectRatio = VideoAspectRatio.FitOutside;
        videoPlayer.targetCamera = targetCamera;
        videoPlayer.targetCameraAlpha = 1f;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        videoPlayer.clip = videoClip;
        videoPlayer.Play();
    }
}
