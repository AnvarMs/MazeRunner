using System.Collections.Generic;
using UnityEngine;

public class ArrayVisualizer : MonoBehaviour
{
    [Header("Array Settings")]
    public int[] array;                      // stores actual data
    public GameObject cellPrefab;            // prefab that has Cell component
    public Transform arrayContainer;         // parent transform for cells

    private List<Cell> cells = new List<Cell>();

    // build from scratch once
    public void CreateArray(int size)
    {
        // create a new data array
        array = new int[size];

        // destroy old cells
        foreach (var c in cells)
        {
            if (c != null)
                Destroy(c.gameObject);
        }
        cells.Clear();

        // create new cell objects
        for (int i = 0; i < size; i++)
        {
            GameObject cellObj = Instantiate(cellPrefab, arrayContainer);
            Cell cell = cellObj.GetComponent<Cell>();

            // assign index and value
            cell.Index = i;
            cell.Value = array[i];

            cells.Add(cell);
        }
    }

    public void UpdateCell(int index, int value)
    {
        if (index < 0 || index >= array.Length) return;

        array[index] = value;
        cells[index].AnimateValueChange(value); // animate the value change
    }


    public void InsertValue(int index, int value)
    {
        if (index < 0) index = 0;
        if (index > array.Length) index = array.Length;

        int[] newArray = new int[array.Length + 1];
        for (int i = 0, j = 0; i < newArray.Length; i++)
        {
            if (i == index) newArray[i] = value;
            else newArray[i] = array[j++];
        }
        array = newArray;

        GameObject cellObj = Instantiate(cellPrefab, arrayContainer);
        cellObj.transform.SetSiblingIndex(index);
        Cell newCell = cellObj.GetComponent<Cell>();
        cells.Insert(index, newCell);

        for (int i = 0; i < array.Length; i++)
        {
            cells[i].Index = i;
            cells[i].Value = array[i];
        }

        newCell.PopIn();
        newCell.FlashColor(Color.yellow, 0.8f);
    }



    public void RemoveValue(int index)
    {
        if (array.Length <= 0 || index < 0 || index >= array.Length) return;

        // build new array minus one element
        int[] newArray = new int[array.Length - 1];
        for (int i = 0, j = 0; i < array.Length; i++)
        {
            if (i == index) continue;
            newArray[j++] = array[i];
        }

        array = newArray;

        // animate the cell pop out, then destroy
        Cell cellToRemove = cells[index];
        cellToRemove.PopOut(() =>
        {
            Destroy(cellToRemove.gameObject);
            cells.RemoveAt(index);

            // refresh indexes and values
            for (int i = 0; i < array.Length; i++)
            {
                cells[i].Index = i;
                cells[i].Value = array[i];
            }
        });
    }

}
