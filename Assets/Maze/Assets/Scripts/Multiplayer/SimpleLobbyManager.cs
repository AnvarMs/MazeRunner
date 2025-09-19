using UnityEngine;
using UnityEngine.UI;
using Fusion;
using TMPro;

public class SimpleLobbyManager : MonoBehaviour
{
    [Header("Lobby UI")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_InputField sessionNameField;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI playersListText;

    private NetworkRunner networkRunner;

    private void Start()
    {
        // Setup button listeners
        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);

        // Set default session name
        if (sessionNameField != null)
            sessionNameField.text = "MazeRoom";

        UpdateStatus("Ready to connect");
    }

    public void OnHostClicked()
    {
        networkRunner = FindFirstObjectByType<NetworkRunner>();
        if (networkRunner != null)
        {
            // The Prototype Network Start should handle hosting
            // Just update our UI
            UpdateStatus("Hosting game...");
            hostButton.interactable = false;
            joinButton.interactable = false;
        }
    }

    public void OnJoinClicked()
    {
        networkRunner = FindFirstObjectByType<NetworkRunner>();
        if (networkRunner != null)
        {
            // The Prototype Network Start should handle joining
            UpdateStatus("Joining game...");
            hostButton.interactable = false;
            joinButton.interactable = false;
        }
    }

    public void OnConnectedToSession()
    {
        UpdateStatus("Connected! Waiting for players...");
        lobbyPanel.SetActive(false);
    }

    public void OnDisconnected()
    {
        UpdateStatus("Disconnected");
        hostButton.interactable = true;
        joinButton.interactable = true;
        lobbyPanel.SetActive(true);
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void Update()
    {
        UpdatePlayersList();
    }

    private void UpdatePlayersList()
    {
        if (playersListText != null && networkRunner != null)
        {
            string playersList = "Players:\n";
            foreach (var player in networkRunner.ActivePlayers)
            {
                playersList += $"- Player {player}\n";
            }
            playersListText.text = playersList;
        }
    }
}