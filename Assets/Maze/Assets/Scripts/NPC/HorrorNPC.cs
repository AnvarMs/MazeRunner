using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum NPCState
{
    Patrolling,
    Chasing,
    Caught,
    Disappearing,
}

public class HorrorNPC : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float disappearMinTime = 3f;
    public float disappearMaxTime = 7f;
    public float stopDistance = .5f;
    public float catchDistance = 1.5f; // Distance to trigger caught state
    public GameObject vfx;
    public Collider trigger;

    public AudioSource audioSource;
    public AudioClip patrolClip;
    public AudioClip catchClip;

    private Transform player;
    private MazeManager maze;
    private Vector2Int lastCell;
    private bool isPlayerCaught = false;

    [SerializeField] private NPCState currentState = NPCState.Disappearing;

    void Start()
    {
        //player = GameObject.FindGameObjectWithTag("Player").transform;
        
        // Initialize audio
        audioSource.clip = patrolClip;
        audioSource.loop = true;
        
      
    }

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
                    yield return StartCoroutine(ChasePlayer());
                    break;

                case NPCState.Caught:
                    yield return StartCoroutine(CaughtRoutine());
                    break;

                case NPCState.Disappearing:
                    yield return StartCoroutine(Disappear());
                    break;
            }
            yield return null;
        }
    }

    IEnumerator Patrol()
    {
        // Play patrol audio if not already playing
        if (!audioSource.isPlaying || audioSource.clip != patrolClip)
        {
            audioSource.clip = patrolClip;
            audioSource.loop = true;
            audioSource.Play();
        }

        // 10% chance to disappear
        if (Random.value < 0.1f)
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
            targetPos.y = transform.position.y;

            lastCell = currentCell;
            yield return StartCoroutine(MoveToPosition(targetPos));
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator Disappear()
    {
        float time = Random.Range(disappearMinTime, disappearMaxTime);
        vfx.SetActive(false);
        trigger.enabled = false;
        audioSource.Stop();

        yield return new WaitForSeconds(time);

        TeleportToRandomPosition();
        vfx.SetActive(true);
        trigger.enabled = true;
        currentState = NPCState.Patrolling;
    }

    void TeleportToRandomPosition()
    {
        List<Vector2Int> validCells = new List<Vector2Int>();

        for (int x = 0; x < maze.MazeXSize; x++)
        {
            for (int z = 0; z < maze.MazeZSize; z++)
            {
                Vector2Int cell = new Vector2Int(x, z);
                if (maze.GetWalkableNeighbors(cell).Count > 0)
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

    IEnumerator ChasePlayer()
    {
        while (currentState == NPCState.Chasing && player != null)
        {
            // Check distance to player
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            
            // Transition to Caught state if close enough
            if (distanceToPlayer <= catchDistance)
            {
                currentState = NPCState.Caught;
                yield break;
            }

            // If player is too far, return to patrolling
            if (distanceToPlayer > 10f)
            {
                currentState = NPCState.Patrolling;
                yield break;
            }

            Vector2Int npcCell = GetCellFromPosition(transform.position);
            Vector2Int playerCell = GetCellFromPosition(player.position);

            List<Vector2Int> path = FindPath(npcCell, playerCell);

            if (path.Count > 1)
            {
                Vector3 nextPos = maze.GetCellWorldPos(path[1]);
                nextPos.y = transform.position.y;
                yield return StartCoroutine(MoveToPosition(nextPos));
            }
            else
            {
                yield return null;
            }
        }
    }

    IEnumerator CaughtRoutine()
    {
        Debug.Log("Player caught!");
        isPlayerCaught = true;

        // Stop patrol sound, play catch sound
        audioSource.Stop();
        audioSource.loop = false;
        audioSource.PlayOneShot(catchClip);

        // Freeze player and NPC
        if (player != null)
        {
            PlayerFirstPerson controls = player.GetComponent<PlayerFirstPerson>();
            if (controls != null) controls.isCanMove = false;

            // Face each other
            Vector3 toPlayer = (player.position - transform.position).normalized;
            toPlayer.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(toPlayer);
            
            float rotationTime = 0.5f;
            float elapsedTime = 0f;
            Quaternion startRotation = transform.rotation;
            
            while (elapsedTime < rotationTime)
            {
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsedTime / rotationTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            transform.rotation = targetRotation;

            // Rotate player to face NPC
            Vector3 toNpc = (transform.position - player.position).normalized;
            toNpc.y = 0;
            player.rotation = Quaternion.LookRotation(toNpc);
        }

        // Wait during "caught" stare
        yield return new WaitForSeconds(3f);

        // Reset player
        if (player != null)
        {
            PlayerFirstPerson controls = player.GetComponent<PlayerFirstPerson>();
            if (controls != null) controls.isCanMove = true;
            player.position = maze.StartPos;
        }

        // Reset NPC
        isPlayerCaught = false;
        TeleportToRandomPosition();
        vfx.SetActive(true);
        trigger.enabled = true;
        
        // Return to patrolling
        currentState = NPCState.Patrolling;
    }

    IEnumerator MoveToPosition(Vector3 targetPos)
    {
        if (isPlayerCaught) yield break; // Don't move if caught

        Vector3 startPos = transform.position;

        // Rotate to face direction
        Vector3 dir = (targetPos - startPos).normalized;
        if (dir != Vector3.zero)
        {
            dir.y = 0;
            Quaternion lookRot = Quaternion.LookRotation(dir);
            transform.rotation = lookRot;
        }

        float journey = 0f;
        float distance = Vector3.Distance(startPos, targetPos);

        while (journey <= 1f && !isPlayerCaught)
        {
            journey += moveSpeed * Time.deltaTime / distance;
            transform.position = Vector3.Lerp(startPos, targetPos, journey);
            yield return null;
        }

        if (!isPlayerCaught)
        {
            transform.position = targetPos;
        }
    }

    Vector2Int GetCellFromPosition(Vector3 pos)
    {
        int x = Mathf.RoundToInt(pos.x);
        int z = Mathf.RoundToInt(pos.z);
        return new Vector2Int(Mathf.Clamp(x, 0, maze.MazeXSize - 1), Mathf.Clamp(z, 0, maze.MazeZSize - 1));
    }

    List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        if (start == goal)
        {
            return new List<Vector2Int> { start };
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
        if (other.CompareTag("Player") && currentState != NPCState.Caught)
        {
            player = other.transform;
            currentState = NPCState.Chasing;

            if (AudioManager.Instance != null)
                AudioManager.Instance.Play("Scream");
        }
    }
    
    // Optional: Add a method to stop all music when in despair
    public void StopAllMusic()
    {
        audioSource.Stop();
        if (AudioManager.Instance != null)
        {
            // Assuming AudioManager has a method to stop all music
           // AudioManager.Instance.StopAllMusic();
        }
    }
}