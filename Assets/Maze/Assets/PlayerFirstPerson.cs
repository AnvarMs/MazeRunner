using UnityEngine;

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

    private float footstepTimer = 0f;
    private bool toggleStep = false;
    private Vector3 lastFootstepPosition;
private float distanceSinceStep = 0f;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleLook();
        HandleMovement();
        HandleFootsteps();
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

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
    float inputMagnitude = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).magnitude;

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
