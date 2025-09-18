using Fusion;
using UnityEngine;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    public GameObject PlayerPrefab;
    [SerializeField] Transform spawnPosition;
    public void PlayerJoined(PlayerRef player)
    {
        Debug.Log("Player Joined: " + player);
        if (player == Runner.LocalPlayer)
        {
            Runner.Spawn(PlayerPrefab, spawnPosition.position, Quaternion.identity);
        }
    }
}