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
    public float moveSpeed = 1f;
    public float caseSpeed = 2.5f;
    public float disappearMinTime = 3f;
    public float disappearMaxTime = 7f;
    public float stopDistance = .5f;
    public float catchDistance = 1.5f; // Distance to trigger caught state
    public float detectionRadius = 5f; // Sphere radius for detecting player
    public GameObject vfx;

    public AudioSource audioSource;
    public AudioClip patrolClip;
    public AudioClip catchClip;

    private Transform player;
    private MazeManager maze;
    private Vector2Int lastCell;
    private bool isPlayerCaught = false;
    private float speed;
    [SerializeField] private NPCState currentState = NPCState.Disappearing;

    void Start()
    {
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
                    speed = moveSpeed;
                    yield return StartCoroutine(Patrol());
                    break;

                case NPCState.Chasing:
                    speed = caseSpeed;
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

        // 5% chance to disappear
        if (Random.value < 0.05f)
        {
            currentState = NPCState.Disappearing;
            if (AudioManager.Instance != null)
                AudioManager.Instance.Play("Scream");
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
        audioSource.Stop();

        yield return new WaitForSeconds(time);

        TeleportToRandomPosition();
        vfx.SetActive(true);
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
            if (distanceToPlayer > detectionRadius * 2f)
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
        PlayerFirstPerson playerControls = player.GetComponent<PlayerFirstPerson>();
        playerControls.isCanMove = false;
        playerControls.ResetCamara();

        // Position the player at offset
        Vector3 offsetPos = transform.position + transform.forward * 0.3f;
        offsetPos.y = transform.position.y + .366f; // keep player's original Y height
        player.position = offsetPos;

        // Make the player face the NPC but only on Y axis (face-to-face)
        Vector3 lookDir = transform.position - player.position;
        lookDir.y = 0f; // ignore vertical difference
        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(lookDir);
            // Ensure only Y-axis rotation is applied
            Vector3 eulerAngles = lookRotation.eulerAngles;
            player.rotation = Quaternion.Euler(0, eulerAngles.y, 0);
        }

        // Wait during "caught" stare
        yield return new WaitForSeconds(4f);
        playerControls.enabled = false;
        yield return null;
        // Reset player
        player.position = maze.StartPos;
        yield return new WaitForSeconds(1f);
        playerControls.enabled = true;
        
        playerControls.isCanMove = true;
        player = null;

        // Reset NPC
        isPlayerCaught = false;
        vfx.SetActive(true);

        // Return to patrolling
        currentState = NPCState.Disappearing;
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
            journey += speed * Time.deltaTime / distance;
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
        // Convert world pos to cell indices based on cell size
        int x = Mathf.RoundToInt(pos.x / maze.CellSize);
        int z = Mathf.RoundToInt(pos.z / maze.CellSize);

        x = Mathf.Clamp(x, 0, maze.MazeXSize - 1);
        z = Mathf.Clamp(z, 0, maze.MazeZSize - 1);

        return new Vector2Int(x, z);
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

    private void Update()
    {
        // Don't detect player if already caught or disappearing
        if (currentState == NPCState.Caught || currentState == NPCState.Disappearing) 
            return;

        // Find player if not already assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            else
                return; // No player found
        }

        // Sphere-based detection for player from all directions
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (Collider col in hitColliders)
        {
            if (col.CompareTag("Player"))
            {
                player = col.transform;
                
                // Only transition to chasing if we're currently patrolling
                if (currentState == NPCState.Patrolling)
                {
                    currentState = NPCState.Chasing;
                    
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.Play("Catch");
                }
                break; // Exit loop once player is found
            }
        }

        // Additional check: if we're chasing and player gets too close, catch them immediately
        if (currentState == NPCState.Chasing && player != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= catchDistance)
            {
                currentState = NPCState.Caught;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Draw detection sphere (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Draw catch distance sphere (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, catchDistance);

        // Draw forward direction indicator
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }
}