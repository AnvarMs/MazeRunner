using UnityEngine;
using UnityEngine.InputSystem; // NEW INPUT SYSTEM
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class PlayerFirstPerson : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float gravity = -9.81f;

    [Header("Mouse Look Settings")]
    public float mouseSensitivity = 2f;

    [Header("Footstep Settings")]
    public AudioClip footstep1;
    public AudioClip footstep2;
    public float stepDistance = 2.0f; // meters between steps

    [Header("References")]
    public Transform cameraTransform;

    private CharacterController controller;
    private AudioSource audioSource;

    private float verticalVelocity;
    private float xRotation = 0f;

    // Footstep
    private bool toggleStep = false;
    private Vector3 lastFootstepPosition;
    private float distanceSinceStep = 0f;

    // Input System
    private PlayerControls controls;  // auto-generated from Input Actions
    private Vector2 moveInput;
    private Vector2 lookInput;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();

        controls = new PlayerControls();

        // Subscribe to input actions
        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        controls.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        controls.Player.Look.canceled += ctx => lookInput = Vector2.zero;
    }

    private void OnEnable()
    {
        controls.Player.Enable();
    }

    private void OnDisable()
    {
        controls.Player.Disable();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        lastFootstepPosition = transform.position;
    }

    private void Update()
    {
        HandleLook();
        HandleMovement();
        HandleFootsteps();
    }

    private void HandleLook()
    {
        // New input system look
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime * 60f; 
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime * 60f;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleMovement()
    {
        // New input system move
        float moveX = moveInput.x;
        float moveZ = moveInput.y;

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        move *= walkSpeed;

        if (controller.isGrounded && verticalVelocity < 0)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);
    }

    private void HandleFootsteps()
    {
        // Use input magnitude to decide if walking
        float inputMagnitude = moveInput.magnitude;

        if (controller.isGrounded && inputMagnitude > 0.1f)
        {
            // Measure distance moved on horizontal plane
            Vector3 currentPosition = transform.position;
            Vector3 flatCurrentPos = new Vector3(currentPosition.x, 0, currentPosition.z);
            Vector3 flatLastPos = new Vector3(lastFootstepPosition.x, 0, lastFootstepPosition.z);

            float moved = Vector3.Distance(flatCurrentPos, flatLastPos);
            distanceSinceStep += moved;

            if (distanceSinceStep >= stepDistance)
            {
                // Play the footstep
                AudioClip clipToPlay = toggleStep ? footstep1 : footstep2;
                audioSource.pitch = Random.Range(0.95f, 1.05f);
                audioSource.PlayOneShot(clipToPlay);
                toggleStep = !toggleStep;

                distanceSinceStep = 0f; // reset distance after step
            }

            lastFootstepPosition = currentPosition;
        }
        else
        {
            // Reset when not moving or not grounded
            lastFootstepPosition = transform.position;
            distanceSinceStep = 0f;
        }
    }
}
