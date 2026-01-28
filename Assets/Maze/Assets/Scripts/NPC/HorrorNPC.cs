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
    public float catchDistance = 1.5f;
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
        audioSource.clip = patrolClip;
        audioSource.loop = true;
    }

    public void StartNPC()
    {
        maze = FindAnyObjectByType<MazeManager>();
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
        if (!audioSource.isPlaying || audioSource.clip != patrolClip)
        {
            audioSource.clip = patrolClip;
            audioSource.loop = true;
            audioSource.Play();
        }

        if (Random.value < 0.05f)
        {
            // currentState = NPCState.Disappearing;
            // yield break;
        }

        Vector2Int currentCell = GetCellFromPosition(transform.position);
        List<Vector2Int> neighbors = maze.GetWalkableNeighbors(currentCell);

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
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            if (distanceToPlayer <= catchDistance)
            {
                currentState = NPCState.Caught;
                yield break;
            }

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

        // Get player controls
        PlayerFirstPerson playerControls = player.GetComponent<PlayerFirstPerson>();

        // CRITICAL: Disable player controls FIRST to stop any input processing
        playerControls.isCanMove = false;

        // Wait one frame to ensure all input is cleared
        yield return null;

        // NOW reset and position camera
        playerControls.ResetCamara();

        // Position the player at offset in front of NPC
        Vector3 offsetPos = transform.position + transform.forward * 0.3f;
        offsetPos.y = transform.position.y + 0.366f;
        player.position = offsetPos;

        // Make the player face the NPC (Y-axis only for horizontal rotation)
        Vector3 lookDir = transform.position - player.position;
        lookDir.y = 0f; // Ignore vertical difference

        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(lookDir);
            // Apply only Y-axis rotation to player transform
            player.rotation = Quaternion.Euler(0, lookRotation.eulerAngles.y, 0);
        }

        // IMPORTANT: Reset camera again after positioning to ensure it's perfectly aligned
        playerControls.ResetCamara();

        // Lock the camera to look straight ahead at NPC
        // The camera should now be at 0,0,0 local rotation (looking forward)
        // and the player is rotated to face the NPC

        // Wait during "caught" stare
        yield return new WaitForSeconds(4f);

        // Reset player
        player.position = maze.StartPos;
        playerControls.isCanMove = true;
        player = null;

        // Reset NPC
        isPlayerCaught = false;
        TeleportToRandomPosition();
        vfx.SetActive(true);

        // Return to patrolling
        currentState = NPCState.Patrolling;
    }

    IEnumerator MoveToPosition(Vector3 targetPos)
    {
        if (isPlayerCaught) yield break;

        Vector3 startPos = transform.position;

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

    public void StopAllMusic()
    {
        audioSource.Stop();
        if (AudioManager.Instance != null)
        {
            // AudioManager.Instance.StopAllMusic();
        }
    }

    private Vector3 gizmoCenter;
    private Vector3 gizmoHalfExtents;
    private Quaternion gizmoRotation;
    private Vector3 gizmoDirection;
    private float gizmoDistance;

    private void Update()
    {
        gizmoHalfExtents = new Vector3(1.5f, 1.5f, 1.5f);
        gizmoDistance = 5f;
        gizmoDirection = transform.forward;
        gizmoRotation = transform.rotation;
        gizmoCenter = transform.position;

        if (currentState == NPCState.Caught) return;

        // Forward detection
        RaycastHit hit;
        if (Physics.BoxCast(gizmoCenter, gizmoHalfExtents, gizmoDirection, out hit, gizmoRotation, gizmoDistance))
        {
            if (hit.collider.CompareTag("Player"))
            {
                player = hit.collider.transform;
                currentState = NPCState.Chasing;

                if (AudioManager.Instance != null)
                    AudioManager.Instance.Play("Scream");
            }
        }

        // Check at NPC position for players approaching from behind/sides
        Collider[] overlapping = Physics.OverlapBox(gizmoCenter, gizmoHalfExtents, gizmoRotation);
        foreach (Collider col in overlapping)
        {
            if (col.CompareTag("Player"))
            {
                player = col.transform;
                currentState = NPCState.Chasing;

                if (AudioManager.Instance != null)
                    AudioManager.Instance.Play("Scream");
                break;
            }
        }
    }

    public void StopNpc()
    {
        StopAllCoroutines();
        audioSource.Stop();
    }
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(gizmoCenter, catchDistance);

        // Draw start box
        Matrix4x4 matrix = Matrix4x4.TRS(gizmoCenter, gizmoRotation, Vector3.one);
        Gizmos.color = Color.red;
        Gizmos.matrix = matrix;
        Gizmos.DrawWireCube(Vector3.zero, gizmoHalfExtents * 2);

        // Draw end box
        Vector3 end = gizmoCenter + gizmoDirection.normalized * gizmoDistance;
        matrix = Matrix4x4.TRS(end, gizmoRotation, Vector3.one);
        Gizmos.matrix = matrix;
        Gizmos.DrawWireCube(Vector3.zero, gizmoHalfExtents * 2);
    }
}