using UnityEngine;

[CreateAssetMenu(fileName = "SongDefinition", menuName = "Magic Tiles/Songs/Song Definition")]
public class SongDefinition : ScriptableObject
{
    [SerializeField] private string songId;
    [SerializeField] private string displayName;
    [SerializeField] private string artist;
    [SerializeField] private AudioClip audioClip;
    [SerializeField] private TextAsset chartAsset;
    [SerializeField] private Sprite coverSprite;

    public string SongId => songId;
    public string DisplayName => displayName;
    public string Artist => artist;
    public AudioClip AudioClip => audioClip;
    public TextAsset ChartAsset => chartAsset;
    public Sprite CoverSprite => coverSprite;
}