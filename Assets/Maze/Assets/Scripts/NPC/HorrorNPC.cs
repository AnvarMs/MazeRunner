using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum NPCState
{
    Patrolling,
    Chasing,
    Disappearing
}

public class HorrorNPC : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float disappearMinTime = 3f;
    public float disappearMaxTime = 7f;
    public GameObject vfx;
    public Collider trigger;

    private Transform player;
    private MazeManager maze;
    private Vector2Int lastCell;

    private NPCState currentState = NPCState.Disappearing;

    public void StartNPC()
    {
        maze = FindAnyObjectByType<MazeManager>();
        
        // Initialize NPC position to a valid cell
        Vector2Int currentCell = GetCellFromPosition(transform.position);
        Vector3 cellWorldPos = maze.GetCellWorldPos(currentCell);
        transform.position = new Vector3(cellWorldPos.x, transform.position.y, cellWorldPos.z);
        
        lastCell = currentCell;
        currentState = NPCState.Disappearing;
        StartCoroutine(StateMachineRoutine());
    }

    // The main state machine loop
    IEnumerator StateMachineRoutine()
    {
        while (true)
        {
            switch (currentState)
            {
                case NPCState.Patrolling:
                    yield return StartCoroutine(Patrol());
                    break;

                case NPCState.Chasing:
                    yield return StartCoroutine(ChasePlayerForSeconds(5f));
                    currentState = NPCState.Patrolling;
                    break;

                case NPCState.Disappearing:
                    yield return StartCoroutine(Disappear());
                    currentState = NPCState.Patrolling;
                    break;
            }

            yield return null;
        }
    }

    // ---------------- STATES ----------------

    IEnumerator Patrol()
    {
        // 10% chance to disappear (was 0.1% before)
        if (Random.value < 0.01f)
        {
            currentState = NPCState.Disappearing;
            yield break;
        }

        Vector2Int currentCell = GetCellFromPosition(transform.position);
        List<Vector2Int> neighbors = maze.GetWalkableNeighbors(currentCell);

        // Remove last cell to avoid immediate backtracking
        if (neighbors.Contains(lastCell) && neighbors.Count > 1)
            neighbors.Remove(lastCell);

        if (neighbors.Count > 0)
        {
            Vector2Int chosenNeighbor = neighbors[Random.Range(0, neighbors.Count)];
            Vector3 targetPos = maze.GetCellWorldPos(chosenNeighbor);
            targetPos.y = transform.position.y; // Keep same Y position

            lastCell = currentCell;
            yield return StartCoroutine(MoveToPosition(targetPos));
        }
        else
        {
            // No available neighbors - wait and try again
            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator Disappear()
    {
        float time = Random.Range(disappearMinTime, disappearMaxTime);
        vfx.SetActive(false);
        trigger.enabled = false;

        yield return new WaitForSeconds(time);

        // Teleport to a random position while disappeared
        TeleportToRandomPosition();

        vfx.SetActive(true);
        trigger.enabled = true;
    }

    void TeleportToRandomPosition()
    {
        // Find all valid cells and pick one randomly
        List<Vector2Int> validCells = new List<Vector2Int>();
        
        for (int x = 0; x < maze.MazeXSize; x++)
        {
            for (int z = 0; z < maze.MazeZSize; z++)
            {
                Vector2Int cell = new Vector2Int(x, z);
                if (maze.GetWalkableNeighbors(cell).Count > 0) // Cell has at least one walkable neighbor
                {
                    validCells.Add(cell);
                }
            }
        }

        if (validCells.Count > 0)
        {
            Vector2Int randomCell = validCells[Random.Range(0, validCells.Count)];
            Vector3 newPos = maze.GetCellWorldPos(randomCell);
            newPos.y = transform.position.y;
            transform.position = newPos;
            lastCell = randomCell;
        }
    }

    IEnumerator ChasePlayerForSeconds(float seconds)
    {
        float timer = 0;
        while (timer < seconds && player != null)
        {
            Vector2Int npcCell = GetCellFromPosition(transform.position);
            Vector2Int playerCell = GetCellFromPosition(player.position);

            List<Vector2Int> path = FindPath(npcCell, playerCell);

            if (path.Count > 1)
            {
                Vector3 nextPos = maze.GetCellWorldPos(path[1]);
                nextPos.y = transform.position.y;
                
                float moveTime = Vector3.Distance(transform.position, nextPos) / moveSpeed;
                yield return StartCoroutine(MoveToPosition(nextPos));
                
                timer += moveTime;
            }
            else
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }
    }

    // ---------------- MOVEMENT + HELPERS ----------------

    IEnumerator MoveToPosition(Vector3 targetPos)
    {
        Vector3 startPos = transform.position;
        
        // Rotate to face direction
        Vector3 dir = (targetPos - startPos).normalized;
        if (dir != Vector3.zero)
        {
            dir.y = 0; // Keep rotation only on Y axis
            Quaternion lookRot = Quaternion.LookRotation(dir);
            transform.rotation = lookRot;
        }

        float journey = 0f;
        float distance = Vector3.Distance(startPos, targetPos);
        
        while (journey <= 1f)
        {
            journey += moveSpeed * Time.deltaTime / distance;
            transform.position = Vector3.Lerp(startPos, targetPos, journey);
            yield return null;
        }
        
        transform.position = targetPos; // Ensure exact position
    }

    Vector2Int GetCellFromPosition(Vector3 pos)
    {
        int x = Mathf.RoundToInt(pos.x);
        int z = Mathf.RoundToInt(pos.z);
        return new Vector2Int(Mathf.Clamp(x, 0, maze.MazeXSize - 1), Mathf.Clamp(z, 0, maze.MazeZSize - 1));
    }

    // BFS Pathfinding
    List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        if (start == goal)
        {
            List<Vector2Int> singlePath = new List<Vector2Int>();
            singlePath.Add(start);
            return singlePath;
        }

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();

        queue.Enqueue(start);
        cameFrom[start] = start;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            if (current == goal) break;

            foreach (Vector2Int neighbor in maze.GetWalkableNeighbors(current))
            {
                if (!cameFrom.ContainsKey(neighbor))
                {
                    queue.Enqueue(neighbor);
                    cameFrom[neighbor] = current;
                }
            }
        }

        List<Vector2Int> path = new List<Vector2Int>();
        if (!cameFrom.ContainsKey(goal))
        {
            path.Add(start);
            return path;
        }

        Vector2Int c = goal;
        while (c != start)
        {
            path.Add(c);
            c = cameFrom[c];
        }
        path.Add(start);
        path.Reverse();
        return path;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.transform;
            currentState = NPCState.Chasing;

            if (AudioManager.Instance != null)
                AudioManager.Instance.Play("Scream");
        }
    }
}