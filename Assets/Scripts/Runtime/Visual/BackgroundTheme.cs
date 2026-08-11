using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "BackgroundTheme", menuName = "Magic Tiles/Visual/Background Theme")]
public class BackgroundTheme : ScriptableObject
{
    [Header("Video")]
    [SerializeField] private VideoClip videoClip;
    [SerializeField] private Color videoOverlayColor = new(0.02f, 0.03f, 0.12f, 0.28f);

    [Header("Procedural Overlay")]
    [SerializeField] private Color primaryColor = Color.blue;
    [SerializeField] private Color secondaryColor = Color.cyan;
    [SerializeField] private Color accentColor = Color.white;

    [SerializeField] private float driftSpeed = 0.25f;
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private float pulseSpeed = 1f;
    [SerializeField] private float pulseAmount = 0.05f;

    public VideoClip VideoClip => videoClip;
    public Color VideoOverlayColor => videoOverlayColor;
    public Color PrimaryColor => primaryColor;
    public Color SecondaryColor => secondaryColor;
    public Color AccentColor => accentColor;

    public float DriftSpeed => driftSpeed;
    public float RotationSpeed => rotationSpeed;
    public float PulseSpeed => pulseSpeed;
    public float PulseAmount => pulseAmount;
}
