using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SongCatalog", menuName = "Magic Tiles/Songs/Song Catalog")]
public class SongCatalog : ScriptableObject
{
    [SerializeField] private List<SongDefinition> songs = new();
    public IReadOnlyList<SongDefinition> Songs => songs;
}