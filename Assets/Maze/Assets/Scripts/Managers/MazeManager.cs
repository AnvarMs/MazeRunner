using UnityEngine;
using System.Collections.Generic;

public class MazeManager : MonoBehaviour
{
    [SerializeField] private GameObject mazeCellPrefab, playerPrefab, triggerPrefab;
    [SerializeField] private int maze_x_size = 10, maze_z_size = 10;

    private MazeCell[,] mazeCells;
// Inside MazeManager
public void SetMazeSize(int x, int z)
{
    maze_x_size = x;
    maze_z_size = z;
}

public void GenerateMazePublic()
{
    // Clear old maze
    if (mazeCells != null)
    {
        foreach (var cell in mazeCells)
        {
            if (cell != null)
                Destroy(cell.gameObject);
        }
    }

    mazeCells = new MazeCell[maze_x_size, maze_z_size];
    GenerateMaze();
}

    private void GenerateMaze()
    {
        // 1. Instantiate the cells
        for (int x = 0; x < maze_x_size; x++)
        {
            for (int z = 0; z < maze_z_size; z++)
            {
                Vector3 pos = new Vector3(x*.2f+x, 0, z*.2f+z);
                GameObject cellObj = Instantiate(mazeCellPrefab, pos, Quaternion.identity, transform);
                MazeCell cell = cellObj.GetComponent<MazeCell>();
                mazeCells[x, z] = cell;
            }
        }

        // 2. Pick random start cell
        int startX = Random.Range(0, maze_x_size);
        int startZ = Random.Range(0, maze_z_size);

        // 3. Recursive backtracking
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        Vector2Int current = new Vector2Int(startX, startZ);
        mazeCells[current.x, current.y].OnVisit();
        stack.Push(current);

        while (stack.Count > 0)
        {
            current = stack.Pop();
            List<Vector2Int> unvisitedNeighbors = GetUnvisitedNeighbors(current);

            if (unvisitedNeighbors.Count > 0)
            {
                // push current back to stack
                stack.Push(current);

                // pick random neighbor
                Vector2Int next = unvisitedNeighbors[Random.Range(0, unvisitedNeighbors.Count)];

                // remove wall between current and next
                RemoveWalls(current, next);

                // mark next as visited
                mazeCells[next.x, next.y].OnVisit();

                // push next cell to stack
                stack.Push(next);
            }
        }

        // 4. Random start & end cells for gameplay
        Vector2Int startCell = new Vector2Int(Random.Range(0, maze_x_size), 0);
        Vector2Int endCell = new Vector2Int(Random.Range(0, maze_x_size), maze_z_size - 1);
        // Get the world positions of the start and end cells
        Vector3 startPos = mazeCells[startCell.x, startCell.y].transform.position;
        Vector3 endPos = mazeCells[endCell.x, endCell.y].transform.position;

        // Optional: Adjust Y if needed so player/trigger sits on the floor
        startPos.y += 0.5f;
        endPos.y += 0.5f;

        // Instantiate prefabs at the cell positions
        playerPrefab.transform.position = startPos;
        playerPrefab.transform.rotation = Quaternion.identity;
        triggerPrefab.transform.position = endPos;
        triggerPrefab.transform.rotation = Quaternion.identity;

    }

    private List<Vector2Int> GetUnvisitedNeighbors(Vector2Int cell)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();

        // left
        if (cell.x > 0 && !mazeCells[cell.x - 1, cell.y].IsVisited)
            neighbors.Add(new Vector2Int(cell.x - 1, cell.y));

        // right
        if (cell.x < maze_x_size - 1 && !mazeCells[cell.x + 1, cell.y].IsVisited)
            neighbors.Add(new Vector2Int(cell.x + 1, cell.y));

        // back
        if (cell.y > 0 && !mazeCells[cell.x, cell.y - 1].IsVisited)
            neighbors.Add(new Vector2Int(cell.x, cell.y - 1));

        // front
        if (cell.y < maze_z_size - 1 && !mazeCells[cell.x, cell.y + 1].IsVisited)
            neighbors.Add(new Vector2Int(cell.x, cell.y + 1));

        return neighbors;
    }

    private void RemoveWalls(Vector2Int current, Vector2Int next)
    {
        int xDiff = next.x - current.x;
        int yDiff = next.y - current.y;

        // current to next
        if (xDiff == 1) // next is right
        {
            mazeCells[current.x, current.y].DeactivateWall(Direction.Right);
            mazeCells[next.x, next.y].DeactivateWall(Direction.Left);
        }
        else if (xDiff == -1) // next is left
        {
            mazeCells[current.x, current.y].DeactivateWall(Direction.Left);
            mazeCells[next.x, next.y].DeactivateWall(Direction.Right);
        }
        else if (yDiff == 1) // next is front
        {
            mazeCells[current.x, current.y].DeactivateWall(Direction.Frond);
            mazeCells[next.x, next.y].DeactivateWall(Direction.Back);
        }
        else if (yDiff == -1) // next is back
        {
            mazeCells[current.x, current.y].DeactivateWall(Direction.Back);
            mazeCells[next.x, next.y].DeactivateWall(Direction.Frond);
        }
    }
}
