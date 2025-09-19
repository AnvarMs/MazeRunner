using UnityEngine;
using Fusion;

public class NetworkedWinTrigger : NetworkBehaviour
{
    private NetworkedMazeUIManager uiManager;

    private void Start()
    {
        uiManager = FindObjectOfType<NetworkedMazeUIManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            NetworkedPlayerController player = other.GetComponent<NetworkedPlayerController>();
            if (player != null && player.Object.HasInputAuthority)
            {
                // Only the local player who reached the goal triggers win
                RPC_PlayerWon(player.Object.InputAuthority);
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_PlayerWon(PlayerRef winner)
    {
        if (uiManager != null)
            uiManager.ShowWinPanel(winner);
    }
}