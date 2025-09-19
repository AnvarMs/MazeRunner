using Fusion;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GameConnectionManager : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField] private NetworkedMazeManager mazeManager;
    [SerializeField] private NetworkedMazeUIManager uiManager;

    private Dictionary<PlayerRef, bool> playerReadyStatus = new Dictionary<PlayerRef, bool>();

    public void PlayerJoined(PlayerRef player)
    {
        Debug.Log($"Player {player} joined the game");

        // Spawn player in maze
        if (mazeManager != null && Runner.IsServer)
        {
            mazeManager.SpawnPlayer(player);
        }

        // Update UI
        if (uiManager != null)
        {
            uiManager.ShowLobby();
        }

        playerReadyStatus[player] = false;
    }

    public void PlayerLeft(PlayerRef player)
    {
        Debug.Log($"Player {player} left the game");

        // Despawn player
        if (mazeManager != null && Runner.IsServer)
        {
            mazeManager.DespawnPlayer(player);
        }

        // Remove from ready status
        if (playerReadyStatus.ContainsKey(player))
        {
            playerReadyStatus.Remove(player);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_PlayerReady(PlayerRef player)
    {
        playerReadyStatus[player] = true;
        CheckAllPlayersReady();
    }

    private void CheckAllPlayersReady()
    {
        if (Runner.ActivePlayers.Count() >= 2) // Minimum 2 players
        {
            bool allReady = true;
            foreach (var player in Runner.ActivePlayers)
            {
                if (!playerReadyStatus.ContainsKey(player) || !playerReadyStatus[player])
                {
                    allReady = false;
                    break;
                }
            }

            if (allReady && uiManager != null)
            {
                uiManager.OnStartGame();
            }
        }
    }
}