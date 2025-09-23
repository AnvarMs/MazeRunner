using UnityEngine;
using System.Collections.Generic;
using Fusion;

public class MazeManager : MonoBehaviour
{
    [SerializeField] private GameObject mazeCellPrefab,mazeCellprefabSmall,triggerPrefab;
    [SerializeField] private int maze_x_size = 10, maze_z_size = 10;
    [SerializeField] private float cellSize = 3f; // set to 3 if prefab is 3x3

    [SerializeField] private Vector3 startPos, endPos;

    private MazeCell[,] mazeCells;
public int MazeXSize => maze_x_size;
public int MazeZSize => maze_z_size;
    // Inside MazeManager
    public Vector3 EndPos => endPos;
    public Vector3 StartPos => startPos;
    public float CellSize => cellSize;

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

public Vector3 GetCellWorldPos(Vector2Int cell)
{
    return new Vector3(cell.x * cellSize, 0, cell.y * cellSize);
}

public Vector2Int GetRandomNeighborCell(Vector2Int current)
{
    // find open neighbors (no wall between cells)
    List<Vector2Int> open = new List<Vector2Int>();
    if (current.x > 0 && !mazeCells[current.x, current.y].HasWall(Direction.Left))
        open.Add(new Vector2Int(current.x - 1, current.y));
    if (current.x < maze_x_size - 1 && !mazeCells[current.x, current.y].HasWall(Direction.Right))
        open.Add(new Vector2Int(current.x + 1, current.y));
    if (current.y > 0 && !mazeCells[current.x, current.y].HasWall(Direction.Back))
        open.Add(new Vector2Int(current.x, current.y - 1));
    if (current.y < maze_z_size - 1 && !mazeCells[current.x, current.y].HasWall(Direction.Frond))
        open.Add(new Vector2Int(current.x, current.y + 1));

    if (open.Count == 0) return current;
    return open[Random.Range(0, open.Count)];
}
public List<Vector2Int> GetWalkableNeighbors(Vector2Int cell)
{
    List<Vector2Int> neighbors = new List<Vector2Int>();

    // Right
    if (!mazeCells[cell.x, cell.y].HasWall(Direction.Right) && cell.x < maze_x_size - 1)
        neighbors.Add(new Vector2Int(cell.x + 1, cell.y));

    // Left
    if (!mazeCells[cell.x, cell.y].HasWall(Direction.Left) && cell.x > 0)
        neighbors.Add(new Vector2Int(cell.x - 1, cell.y));

    // Front
    if (!mazeCells[cell.x, cell.y].HasWall(Direction.Frond) && cell.y < maze_z_size - 1)
        neighbors.Add(new Vector2Int(cell.x, cell.y + 1));

    // Back
    if (!mazeCells[cell.x, cell.y].HasWall(Direction.Back) && cell.y > 0)
        neighbors.Add(new Vector2Int(cell.x, cell.y - 1));

    return neighbors;
}
    private void GenerateMaze()
    {
        
        // 1. Instantiate the cells

        for (int x = 0; x < maze_x_size; x++)
{
    for (int z = 0; z < maze_z_size; z++)
    {
        Vector3 pos = new Vector3(x * cellSize, 0, z * cellSize);

        // Alternate prefab
        GameObject prefabToUse = (x + z) % 2 == 0 ? mazeCellPrefab : mazeCellprefabSmall;

        GameObject cellObj = Instantiate(prefabToUse, pos, Quaternion.identity, transform);

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
        Vector2Int start = new Vector2Int(Random.Range(0, maze_x_size), 0);
        Vector2Int end= new Vector2Int(Random.Range(0, maze_x_size), maze_z_size - 1);
        // Get the world positions of the start and end cells
        startPos = mazeCells[start.x, start.y].transform.position;
        endPos = mazeCells[end.x, end.y].transform.position;

        // Optional: Adjust Y if needed so player/trigger sits on the floor
        startPos.y += 0.5f;
        endPos.y += 0.5f;

        // Instantiate prefabs at the cell positions
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
