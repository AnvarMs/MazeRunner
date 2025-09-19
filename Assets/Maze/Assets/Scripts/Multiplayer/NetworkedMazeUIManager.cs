using Fusion;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
public class NetworkedMazeUIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject panelMainMenu;
    [SerializeField] private GameObject panelWin;
    [SerializeField] private GameObject panelPause;
    [SerializeField] private GameObject panelLobby;

    [Header("Lobby UI")]
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private Button startGameButton;
    [SerializeField] private TextMeshProUGUI gameStatusText;

    [Header("Controls")]
    [SerializeField] private Slider difficultySlider;
    [SerializeField] private NetworkedMazeManager mazeManager;
    [SerializeField] private HorrorNPC horrorNPC;

    [Header("Win Panel")]
    [SerializeField] private TextMeshProUGUI winnerText;

    private bool isPaused = false;
    private bool gameStarted = false;
    private NetworkRunner networkRunner;

    private void Start()
    {
        panelMainMenu.SetActive(true);
        panelWin.SetActive(false);
        panelPause.SetActive(false);
        panelLobby.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        isPaused = false;
        Time.timeScale = 1f; // Don't pause time in multiplayer

        // Find network runner
        networkRunner = FindFirstObjectByType<NetworkRunner>();
        if (networkRunner == null)
        {
            // If no runner exists, we're probably in menu
            gameStatusText.text = "Not Connected";
        }
    }

    public void OnStartGame()
    {
        Debug.Log("Start Game button clicked   "+ gameStarted);
        if (!gameStarted)
        {
            gameStarted = true;

            // Set maze size based on slider
            int size = Mathf.RoundToInt(difficultySlider.value);
            mazeManager.SetMazeSize(size, size);
            mazeManager.GenerateMazePublic();

            if (horrorNPC != null)
                horrorNPC.StartNPC();

            panelMainMenu.SetActive(false);
            panelLobby.SetActive(false);

            // Don't lock cursor here - let individual players handle it
            gameStatusText.text = "Game Started!";
        }
    }

    public void ShowWinPanel(PlayerRef winner)
    {
        panelWin.SetActive(true);

        if (winner == PlayerRef.None)
        {
            winnerText.text = "Time's Up! No Winner";
        }
        else if (networkRunner != null && networkRunner.LocalPlayer == winner)
        {
            winnerText.text = "You Won!";
        }
        else
        {
            winnerText.text = $"Player {winner} Won!";
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnReplay()
    {
        if (mazeManager.Object.HasStateAuthority)
        {
            mazeManager.GenerateMazePublic();
        }

        panelWin.SetActive(false);

        // Lock cursor for local player
       // Cursor.lockState = CursorLockMode.Locked;
       // Cursor.visible = false;
    }

    public void OnQuit()
    {
        if (networkRunner != null)
        {
            networkRunner.Shutdown();
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        ShowMainMenu();
#else
        Application.Quit();
#endif
    }

    private void ShowMainMenu()
    {
        panelMainMenu.SetActive(true);
        panelWin.SetActive(false);
        panelPause.SetActive(false);
        panelLobby.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        gameStarted = false;
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame && gameStarted)
        {
            TogglePause();
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (networkRunner != null && playerCountText != null)
        {
            int playerCount = networkRunner.ActivePlayers.Count();
            playerCountText.text = $"Players: {playerCount}";

            // Only host can start the game
            //if (startGameButton != null)
            //    startGameButton.interactable = networkRunner.IsServer && !gameStarted;
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        if (isPaused)
        {
            panelPause.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            panelPause.SetActive(false);
           // Cursor.lockState = CursorLockMode.Locked;
            //Cursor.visible = false;
        }
    }

    public void OnResume()
    {
        TogglePause();
    }

    // Called when players join/leave lobby
    public void ShowLobby()
    {
        panelMainMenu.SetActive(false);
        panelLobby.SetActive(true);
        gameStatusText.text = "In Lobby - Waiting for players...";
    }
}