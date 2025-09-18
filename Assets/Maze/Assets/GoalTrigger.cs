using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    private MazeUIManager uiManager;

    private void Start()
    {
        uiManager = FindAnyObjectByType<MazeUIManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            uiManager.ShowWinPanel();
        }
    }
}
