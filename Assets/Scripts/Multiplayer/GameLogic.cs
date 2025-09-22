using Fusion;
using UnityEngine;

public class GameLogic : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField] private NetworkPrefabRef player;
    [Networked, Capacity(10)] private NetworkDictionary<PlayerRef, Player> Players => default;

    public void PlayerJoined(PlayerRef playerRef)
    {
        if (Object.HasStateAuthority)
        {
            NetworkObject playerObject = Runner.Spawn(player, Vector3.up, Quaternion.identity, playerRef);
            Players.Add(playerRef, playerObject.GetComponent<Player>());
        }
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (Object.HasStateAuthority) return; 
    }
}
