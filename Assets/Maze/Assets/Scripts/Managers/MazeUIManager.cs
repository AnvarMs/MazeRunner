using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class MazeUIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject panelMainMenu;
    [SerializeField] private GameObject panelWin;
    [SerializeField] private GameObject panelPause;

    [Header("Controls")]
    [SerializeField] private Slider difficultySlider;
    [SerializeField] private MazeManager mazeManager; // Drag your MazeManager here

    private bool isPaused = false;

    private void Start()
    {
        // Show main menu at start
        panelMainMenu.SetActive(true);
        panelWin.SetActive(false);
        panelPause.SetActive(false);

        // Lock cursor off until game starts
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        isPaused = false;
        Time.timeScale = 0f;
    }

    // Called by Start Button
    public void OnStartGame()
    {
        // Set maze size based on slider
        int size = Mathf.RoundToInt(difficultySlider.value);
        mazeManager.SetMazeSize(size, size); // new method we’ll add
        mazeManager.GenerateMazePublic();    // regenerate maze

        panelMainMenu.SetActive(false);

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;
    }

    // Called when the player reaches the goal
    public void ShowWinPanel()
    {
        panelWin.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    public void OnReplay()
    {
        // Regenerate with same size
        mazeManager.GenerateMazePublic();

        panelWin.SetActive(false);

        // Lock cursor again
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;
    }

     public void OnQuit()
{
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
    // In WebGL – cannot quit app. Show menu or message instead.
    ShowMainMenu(); // create a method to go back to your main menu
#else
    Application.Quit();
#endif
}

    private void ShowMainMenu()
    {
        // Example: reload the first scene or enable your main menu panel
        panelMainMenu.SetActive(true);
        panelWin.SetActive(false);
        panelPause.SetActive(false);

        // Optionally reset Time.timeScale in case of pause
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }


    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            Time.timeScale = 0f;
            panelPause.SetActive(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = 1f;
            panelPause.SetActive(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void OnResume()
    {
        TogglePause();
    }
}
