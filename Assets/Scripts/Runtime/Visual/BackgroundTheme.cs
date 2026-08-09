using UnityEngine;

[CreateAssetMenu(fileName = "BackgroundTheme", menuName = "Magic Tiles/Visual/Background Theme")]
public class BackgroundTheme : ScriptableObject
{
    [SerializeField] private Color primaryColor = Color.blue;
    [SerializeField] private Color secondaryColor = Color.cyan;
    [SerializeField] private Color accentColor = Color.white;

    [SerializeField] private float driftSpeed = 0.25f;
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private float pulseSpeed = 1f;
    [SerializeField] private float pulseAmount = 0.05f;

    public Color PrimaryColor => primaryColor;
    public Color SecondaryColor => secondaryColor;
    public Color AccentColor => accentColor;

    public float DriftSpeed => driftSpeed;
    public float RotationSpeed => rotationSpeed;
    public float PulseSpeed => pulseSpeed;
    public float PulseAmount => pulseAmount;
}