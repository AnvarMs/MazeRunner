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
    [SerializeField] private GameObject panelMobileUI;

    [Header("Player")]
    [SerializeField] private GameObject player; 

    [Header("Controls")]
    [SerializeField] private Slider difficultySlider;
    [SerializeField] private MazeManager mazeManager; // Drag your MazeManager here
    [SerializeField] private HorrorNPC horrorNPC;

    [Header("Settings")]
    [SerializeField] private Toggle mobileUIToggle, mobileUIToggle2;
    private bool isPaused = false;
  

    PlayerFirstPerson Pfp;
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
        
        
        mobileUIToggle.onValueChanged.AddListener(ToggleMobileUI);
        mobileUIToggle2.onValueChanged.AddListener(ToggleMobileUI);

        Pfp = player.GetComponent<PlayerFirstPerson>();
    }

    // Called by Start Button
    public void OnStartGame()
    {
        // Set maze size based on slider
        int size = Mathf.RoundToInt(difficultySlider.value);
        mazeManager.SetMazeSize(size, size); // new method we’ll add
        mazeManager.GenerateMazePublic();    // regenerate maze
        horrorNPC.StartNPC();
        panelMainMenu.SetActive(false);
        ToggleMobileUI(mobileUIToggle.isOn);
        // Player spawning
       
        Pfp.SpownAt(mazeManager.StartPos);
        Pfp.StartGame();

      
    }

    // Called when the player reaches the goal
    public void ShowWinPanel()
    {
        Pfp.isCanMove = false;
        panelWin.SetActive(true);
        horrorNPC.StopNpc();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
    }

    public void OnReplay()
    {
        // Lock cursor again
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        panelWin.SetActive(false);

        Pfp.SpownAt(mazeManager.StartPos);
        Pfp.StartGame();
        horrorNPC.StartNPC();
    }

    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
    // Show main menu instead of trying to quit
    if (!panelMainMenu.activeSelf)
    {
        ShowMainMenu();
    }
   
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
        if (Keyboard.current.escapeKey.wasPressedThisFrame&& !panelMainMenu.activeSelf)
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
    public void ToggleMobileUI(bool isMobile)
    {
        panelMobileUI.SetActive(isMobile);
        mobileUIToggle.isOn = isMobile;
        mobileUIToggle2.isOn = isMobile;
    }
}
