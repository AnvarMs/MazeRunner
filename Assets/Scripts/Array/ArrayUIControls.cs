using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArrayUIControls : MonoBehaviour
{
    [Header("Reference to Array Visualizer")]
    public ArrayVisualizer arrayVisualizer;

    [Header("Update Cell")]
    public TMP_InputField updateIndexInput;
    public TMP_InputField updateValueInput;
    public Button updateButton;

    [Header("Insert Cell")]
    public TMP_InputField insertIndexInput;
    public TMP_InputField insertValueInput;
    public Button insertButton;

    [Header("Remove Cell")]
    public TMP_InputField removeIndexInput;
    public Button removeButton;

    private void Start()
    {
        // Hook up button clicks
        updateButton.onClick.AddListener(OnUpdateClicked);
        insertButton.onClick.AddListener(OnInsertClicked);
        removeButton.onClick.AddListener(OnRemoveClicked);
    }

    private void OnUpdateClicked()
    {
        int index, value;
        if (int.TryParse(updateIndexInput.text, out index) &&
            int.TryParse(updateValueInput.text, out value))
        {
            arrayVisualizer.UpdateCell(index, value);
        }
    }

    private void OnInsertClicked()
    {
        int index, value;
        if (int.TryParse(insertIndexInput.text, out index) &&
            int.TryParse(insertValueInput.text, out value))
        {
            arrayVisualizer.InsertValue(index, value);
        }
    }

    private void OnRemoveClicked()
    {
        int index;
        if (int.TryParse(removeIndexInput.text, out index))
        {
            arrayVisualizer.RemoveValue(index);
        }
    }
}
