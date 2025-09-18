using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArraySetupUI : MonoBehaviour
{
    [Header("References")]
    public ArrayVisualizer arrayVisualizer;

    [Header("Panels")]
    public GameObject settingsPanel;
    public GameObject valuesPanel;

    [Header("Settings Panel UI")]
    public TMP_InputField arraySizeInput;
    public Button createButton;

    [Header("Values Panel UI")]
    public Transform valuesContainer;
    public GameObject valueInputPrefab; // prefab with TMP_InputField
    public Button applyValuesButton;

    private List<TMP_InputField> valueInputs = new List<TMP_InputField>();

    private void Start()
    {
        // Show settings panel at start
        settingsPanel.SetActive(true);
        valuesPanel.SetActive(false);

        createButton.onClick.AddListener(OnCreateArrayClicked);
        applyValuesButton.onClick.AddListener(OnApplyValuesClicked);
    }

    private void OnCreateArrayClicked()
    {
        // parse size
        int size = 0;
        int.TryParse(arraySizeInput.text, out size);
        if (size <= 0) return;

        // Create the array in ArrayVisualizer
        arrayVisualizer.CreateArray(size);

        // spawn input fields for values
        foreach (Transform child in valuesContainer)
            Destroy(child.gameObject);
        valueInputs.Clear();

        for (int i = 0; i < size; i++)
        {
            GameObject inputObj = Instantiate(valueInputPrefab, valuesContainer);
            TMP_InputField inputField = inputObj.GetComponent<TMP_InputField>();
            inputField.placeholder.GetComponent<TMP_Text>().text = $"Index {i}";
            valueInputs.Add(inputField);
        }

        settingsPanel.SetActive(false);
        valuesPanel.SetActive(true);
    }

    private void OnApplyValuesClicked()
    {
        for (int i = 0; i < valueInputs.Count; i++)
        {
            int val = 0;
            int.TryParse(valueInputs[i].text, out val);
            arrayVisualizer.UpdateCell(i, val);
        }

        valuesPanel.SetActive(false);
    }
}
