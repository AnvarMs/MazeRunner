using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro; // If you have TextMeshPro, otherwise use UnityEngine.UI.Text

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class PlayerFirstPersonDebug : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float gravity = -9.81f;

    [Header("Mouse Look Settings")]
    public float mouseSensitivity = 2f;
    public float mobileSensitivity = 0.5f;

    [Header("Footstep Settings")]
    public AudioClip footstep1;
    public AudioClip footstep2;
    public float stepDistance = 2.0f;

    [Header("References")]
    public Transform cameraTransform;
    public MobileJoystick joystick;
    public RectTransform joystickArea;

    [Header("Debug UI")]
    public TextMeshProUGUI debugText; // Assign a UI Text element to see debug info

    [Header("Settings")]
    public bool isCanMove;
    public bool showDebug = true;

    private CharacterController controller;
    private AudioSource audioSource;

    private float verticalVelocity;
    private float xRotation = 0f;

    // Footstep
    private bool toggleStep = false;
    private Vector3 lastFootstepPosition;
    private float distanceSinceStep = 0f;

    // Input
    private Vector2 moveInput;
    private bool isMobile = false;

    // Manual touch tracking
    private class TouchInfo
    {
        public int fingerId;
        public Vector2 startPosition;
        public Vector2 lastPosition;
        public bool startedOnUI;
        public bool startedOnJoystick;
        public bool isLookTouch;
    }

    private Dictionary<int, TouchInfo> activeTouches = new Dictionary<int, TouchInfo>();
    private int lookTouchId = -1;

    private void Awake()
    {
        isCanMove = false;
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        isMobile = Application.isMobilePlatform || Application.isEditor; // Force mobile mode in editor for testing
    }

    public void StartGame()
    {
        isCanMove = true;
        if (!isMobile)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        lastFootstepPosition = transform.position;
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (!isCanMove) return;

        if (isMobile)
        {
            ProcessTouches();
        }
        else
        {
            HandleDesktopLook();
        }

        HandleMovement();
        HandleFootsteps();

        UpdateDebugUI();
    }

    private void ProcessTouches()
    {
        if (Touchscreen.current == null)
        {
            if (showDebug) Debug.Log("No Touchscreen detected!");
            return;
        }

        var touches = Touchscreen.current.touches;

        // Track which touch IDs are still active this frame
        HashSet<int> currentActiveTouches = new HashSet<int>();

        // Process all touches
        for (int i = 0; i < touches.Count; i++)
        {
            var touch = touches[i];
            if (!touch.isInProgress) continue;

            int fingerId = touch.touchId.ReadValue();
            Vector2 position = touch.position.ReadValue();
            var phase = touch.phase.ReadValue();

            currentActiveTouches.Add(fingerId);

            // Touch just started
            if (phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                bool onUI = IsPointerOverUI(position);
                bool onJoystick = IsPositionOverJoystick(position);

                TouchInfo info = new TouchInfo
                {
                    fingerId = fingerId,
                    startPosition = position,
                    lastPosition = position,
                    startedOnUI = onUI,
                    startedOnJoystick = onJoystick,
                    isLookTouch = false
                };

                activeTouches[fingerId] = info;

                if (showDebug)
                {
                    Debug.Log($"[TOUCH BEGIN] ID: {fingerId} | Pos: {position} | OnUI: {onUI} | OnJoystick: {onJoystick}");
                }

                // Check if this can be our look touch
                if (lookTouchId == -1 && !onUI && !onJoystick)
                {
                    lookTouchId = fingerId;
                    info.isLookTouch = true;
                    if (showDebug) Debug.Log($"[LOOK TOUCH ASSIGNED] ID: {fingerId}");
                }
            }
            // Touch is moving
            else if (phase == UnityEngine.InputSystem.TouchPhase.Moved && activeTouches.ContainsKey(fingerId))
            {
                TouchInfo info = activeTouches[fingerId];
                Vector2 delta = position - info.lastPosition;

                // Check current UI state for debugging
                bool currentlyOnUI = IsPointerOverUI(position);
                bool currentlyOnJoystick = IsPositionOverJoystick(position);

                if (showDebug && (currentlyOnUI || currentlyOnJoystick))
                {
                    Debug.Log($"[TOUCH MOVE] ID: {fingerId} | IsLookTouch: {info.isLookTouch} | CurrentlyOnUI: {currentlyOnUI} | CurrentlyOnJoystick: {currentlyOnJoystick} | StartedOnUI: {info.startedOnUI} | StartedOnJoystick: {info.startedOnJoystick}");
                }

                // If this is our look touch AND it didn't start on UI/joystick
                if (fingerId == lookTouchId && info.isLookTouch)
                {
                    // Apply camera rotation REGARDLESS of current position
                    float rotX = delta.x * mobileSensitivity;
                    float rotY = delta.y * mobileSensitivity;

                    xRotation -= rotY;
                    xRotation = Mathf.Clamp(xRotation, -90f, 90f);

                    cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
                    transform.Rotate(Vector3.up * rotX);

                    if (showDebug && delta.magnitude > 1f)
                    {
                        Debug.Log($"[CAMERA ROTATED] Delta: {delta} | RotX: {rotX} | RotY: {rotY}");
                    }
                }

                info.lastPosition = position;
            }
            // Touch ended
            else if (phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                     phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                if (showDebug)
                {
                    Debug.Log($"[TOUCH END] ID: {fingerId} | WasLookTouch: {activeTouches.ContainsKey(fingerId) && activeTouches[fingerId].isLookTouch}");
                }
            }
        }

        // Clean up ended touches
        List<int> touchesToRemove = new List<int>();
        foreach (var kvp in activeTouches)
        {
            if (!currentActiveTouches.Contains(kvp.Key))
            {
                touchesToRemove.Add(kvp.Key);

                // Reset look touch if this was it
                if (kvp.Key == lookTouchId)
                {
                    lookTouchId = -1;
                    if (showDebug) Debug.Log($"[LOOK TOUCH RELEASED] ID: {kvp.Key}");
                }
            }
        }

        foreach (int id in touchesToRemove)
        {
            activeTouches.Remove(id);
        }
    }

    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPosition;

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        return results.Count > 0;
    }

    private bool IsPositionOverJoystick(Vector2 screenPosition)
    {
        if (joystickArea == null) return false;

        return RectTransformUtility.RectangleContainsScreenPoint(
            joystickArea,
            screenPosition,
            null
        );
    }

    private void HandleDesktopLook()
    {
        if (Mouse.current == null) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime * 60f;
        float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime * 60f;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleMovement()
    {
        float moveX = moveInput.x + (joystick != null ? joystick.Horizontal : 0);
        float moveZ = moveInput.y + (joystick != null ? joystick.Vertical : 0);

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
        float inputMagnitude = moveInput.magnitude + new Vector2(joystick != null ? joystick.Horizontal : 0, joystick != null ? joystick.Vertical : 0).magnitude;

        if (controller.isGrounded && inputMagnitude > 0.1f)
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

    private void UpdateDebugUI()
    {
        if (debugText != null && showDebug)
        {
            string info = $"Active Touches: {activeTouches.Count}\n";
            info += $"Look Touch ID: {lookTouchId}\n";
            info += $"Joystick: H={joystick?.Horizontal:F2}, V={joystick?.Vertical:F2}\n";
            info += $"Camera Rotation: {xRotation:F1}\n\n";

            foreach (var kvp in activeTouches)
            {
                TouchInfo t = kvp.Value;
                info += $"Touch {t.fingerId}:\n";
                info += $"  IsLook: {t.isLookTouch}\n";
                info += $"  StartUI: {t.startedOnUI}\n";
                info += $"  StartJoy: {t.startedOnJoystick}\n";
                info += $"  Pos: {t.lastPosition}\n\n";
            }

            debugText.text = info;
        }
    }

    public void SpownAt(Vector3 pos)
    {
        transform.position = pos;
    }

    public void ResetCamara()
    {
        cameraTransform.rotation = Quaternion.identity;
        xRotation = 0f;
    }

    public void MobileInput(float x, float z)
    {
        moveInput = new Vector2(x, z);
    }
}