using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;

public struct NetworkInputData : INetworkInput
{
    public Vector2 MovementInput;
    public Vector2 LookInput;
    public NetworkButtons Buttons;
}

[RequireComponent(typeof(NetworkCharacterController))]
[RequireComponent(typeof(AudioSource))]
public class NetworkedPlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;

    [Header("Mouse Look Settings")]
    public float mouseSensitivity = 2f;

    [Header("Footstep Settings")]
    public AudioClip footstep1;
    public AudioClip footstep2;
    public float stepDistance = 2.0f;

    [Header("References")]
    public Transform cameraTransform;
    public GameObject playerVisualModel; // Add this for other players to see

    [Networked] public float XRotation { get; set; }
    [Networked] public Vector3 NetworkPosition { get; set; }
    [Networked] public Quaternion NetworkRotation { get; set; }

    private NetworkCharacterController networkController;
    private AudioSource audioSource;

    // Input handling
    private PlayerControls controls;
    private Vector2 moveInput;
    private Vector2 lookInput;

    // Footstep variables
    private bool toggleStep = false;
    private Vector3 lastFootstepPosition;
    private float distanceSinceStep = 0f;

    // Smoothing for non-authority clients
    private Vector3 smoothPosition;
    private Quaternion smoothRotation;

    private void Awake()
    {
        networkController = GetComponent<NetworkCharacterController>();
        audioSource = GetComponent<AudioSource>();
    }

    public override void Spawned()
    {
        // Set up input only for the local player
        if (Object.HasInputAuthority)
        {
            // This is our local player
            SetupLocalPlayer();
        }
        else
        {
            // This is a remote player
            SetupRemotePlayer();
        }

        lastFootstepPosition = transform.position;
        NetworkPosition = transform.position;
        NetworkRotation = transform.rotation;

        smoothPosition = transform.position;
        smoothRotation = transform.rotation;
    }

    private void SetupLocalPlayer()
    {
        controls = new PlayerControls();

        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        controls.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        controls.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        controls.Player.Enable();

        // Enable camera and hide visual model for first person
        if (cameraTransform != null)
            cameraTransform.gameObject.SetActive(true);

        if (playerVisualModel != null)
            playerVisualModel.SetActive(false);

        // Lock cursor for local player
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SetupRemotePlayer()
    {
        // Disable camera for remote players
        if (cameraTransform != null)
            cameraTransform.gameObject.SetActive(false);

        // Show visual model for other players to see
        if (playerVisualModel != null)
            playerVisualModel.SetActive(true);
    }

    public override void FixedUpdateNetwork()
    {
        // Get input for this tick
        if (GetInput<NetworkInputData>(out var input))
        {
            // Fill input for input authority
            if (Object.HasInputAuthority)
            {
                input.MovementInput = moveInput;
                input.LookInput = lookInput;
            }

            // Handle movement
            Vector3 move = transform.right * input.MovementInput.x + transform.forward * input.MovementInput.y;
            move *= walkSpeed;

            networkController.Move(move * Runner.DeltaTime);

            // Handle look rotation (only for input authority)
            if (Object.HasInputAuthority)
            {
                float mouseX = input.LookInput.x * mouseSensitivity * Runner.DeltaTime * 60f;
                float mouseY = input.LookInput.y * mouseSensitivity * Runner.DeltaTime * 60f;

                XRotation -= mouseY;
                XRotation = Mathf.Clamp(XRotation, -90f, 90f);

                transform.Rotate(Vector3.up * mouseX);
            }

            // Update networked position and rotation
            NetworkPosition = transform.position;
            NetworkRotation = transform.rotation;
        }
    }

    public override void Render()
    {
        // Smooth interpolation for remote players
        if (!Object.HasInputAuthority)
        {
            smoothPosition = Vector3.Lerp(smoothPosition, NetworkPosition, Time.deltaTime * 15f);
            smoothRotation = Quaternion.Lerp(smoothRotation, NetworkRotation, Time.deltaTime * 15f);

            transform.position = smoothPosition;
            transform.rotation = smoothRotation;
        }

        // Update camera rotation for local player
        if (Object.HasInputAuthority && cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(XRotation, 0f, 0f);
        }

        // Handle footsteps (only for local player)
        if (Object.HasInputAuthority)
        {
            HandleFootsteps();
        }
    }

    private void HandleFootsteps()
    {
        float inputMagnitude = moveInput.magnitude;
        if (networkController.Grounded && inputMagnitude > 0.1f)
        {
            Vector3 currentPosition = transform.position;
            Vector3 flatCurrentPos = new Vector3(currentPosition.x, 0, currentPosition.z);
            Vector3 flatLastPos = new Vector3(lastFootstepPosition.x, 0, lastFootstepPosition.z);
            float moved = Vector3.Distance(flatCurrentPos, flatLastPos);
            distanceSinceStep += moved;

            if (distanceSinceStep >= stepDistance)
            {
                AudioClip clipToPlay = toggleStep ? footstep1 : footstep2;
                audioSource.pitch = Random.Range(0.95f, 1.05f);
                audioSource.PlayOneShot(clipToPlay);
                toggleStep = !toggleStep;
                distanceSinceStep = 0f;
            }
            lastFootstepPosition = currentPosition;
        }
        else
        {
            lastFootstepPosition = transform.position;
            distanceSinceStep = 0f;
        }
    }

    private void OnDestroy()
    {
        if (controls != null)
        {
            controls.Player.Disable();
            controls.Dispose();
        }
    }
}