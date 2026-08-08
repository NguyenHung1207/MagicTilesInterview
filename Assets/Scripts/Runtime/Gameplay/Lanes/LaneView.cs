using UnityEngine;

public class LaneView : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform hitPoint;
    public Transform SpawnPoint => spawnPoint;
    public Transform HitPoint => hitPoint;
}