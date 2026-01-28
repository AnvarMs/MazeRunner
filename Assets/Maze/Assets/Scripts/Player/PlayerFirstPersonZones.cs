using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class PlayerFirstPersonZones : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float gravity = -9.81f;

    [Header("Mouse Look Settings")]
    public float mouseSensitivity = 2f;
    public float mobileSensitivity = 0.5f;

    [Header("Screen Zones")]
    [Range(0.3f, 0.7f)]
    public float screenSplitRatio = 0.5f; // 0.5 = 50% left is joystick zone, 50% right is look zone
    public bool showZoneDebug = false; // Draw visual debug lines

    [Header("Footstep Settings")]
    public AudioClip footstep1;
    public AudioClip footstep2;
    public float stepDistance = 2.0f;

    [Header("References")]
    public Transform cameraTransform;
    public MobileJoystick joystick;

    [Header("Settings")]
    public bool isCanMove;

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

    // Zone-based touch tracking
    private class TouchInfo
    {
        public int fingerId;
        public Vector2 lastPosition;
        public bool isInLookZone; // Right side of screen
    }

    private Dictionary<int, TouchInfo> activeTouches = new Dictionary<int, TouchInfo>();
    private float screenSplitX; // Calculated split position in pixels

    private void Awake()
    {
        isCanMove = false;
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        isMobile = Application.isMobilePlatform;

        CalculateScreenSplit();
    }

    private void CalculateScreenSplit()
    {
        screenSplitX = Screen.width * screenSplitRatio;
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

        // Recalculate split if screen size changes
        if (Mathf.Abs(screenSplitX - (Screen.width * screenSplitRatio)) > 1f)
        {
            CalculateScreenSplit();
        }

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
    }

    private void OnGUI()
    {
        // Debug visualization of screen zones
        if (showZoneDebug && isMobile)
        {
            // Draw split line
            Texture2D lineTex = new Texture2D(1, 1);
            lineTex.SetPixel(0, 0, Color.yellow);
            lineTex.Apply();

            GUI.DrawTexture(new Rect(screenSplitX - 2, 0, 4, Screen.height), lineTex);

            // Draw labels
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 24;
            style.alignment = TextAnchor.MiddleCenter;

            GUI.Label(new Rect(0, 50, screenSplitX, 50), "JOYSTICK ZONE", style);
            GUI.Label(new Rect(screenSplitX, 50, Screen.width - screenSplitX, 50), "LOOK ZONE", style);

            // Show active touches
            foreach (var touch in activeTouches.Values)
            {
                Color touchColor = touch.isInLookZone ? Color.cyan : Color.green;
                lineTex.SetPixel(0, 0, touchColor);
                lineTex.Apply();
                GUI.DrawTexture(new Rect(touch.lastPosition.x - 25, Screen.height - touch.lastPosition.y - 25, 50, 50), lineTex);
            }
        }
    }

    private void ProcessTouches()
    {
        if (Touchscreen.current == null) return;

        var touches = Touchscreen.current.touches;

        // Update existing and add new touches
        for (int i = 0; i < touches.Count; i++)
        {
            var touch = touches[i];
            if (!touch.isInProgress) continue;

            int fingerId = touch.touchId.ReadValue();
            Vector2 position = touch.position.ReadValue();
            var phase = touch.phase.ReadValue();

            // NEW TOUCH
            if (phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                // Determine zone based on X position
                bool isInLookZone = position.x > screenSplitX;

                TouchInfo info = new TouchInfo
                {
                    fingerId = fingerId,
                    lastPosition = position,
                    isInLookZone = isInLookZone
                };

                activeTouches[fingerId] = info;

                // If touch starts in joystick zone, activate joystick
                if (!isInLookZone && joystick != null)
                {
                    // Let joystick handle it naturally through its own event system
                }
            }
            // MOVING TOUCH
            else if (activeTouches.ContainsKey(fingerId))
            {
                TouchInfo info = activeTouches[fingerId];
                Vector2 delta = position - info.lastPosition;

                // ONLY touches in the LOOK ZONE can rotate camera
                if (info.isInLookZone && delta.magnitude > 0.1f)
                {
                    float rotX = delta.x * mobileSensitivity;
                    float rotY = delta.y * mobileSensitivity;

                    xRotation -= rotY;
                    xRotation = Mathf.Clamp(xRotation, -90f, 90f);

                    cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
                    transform.Rotate(Vector3.up * rotX);
                }

                info.lastPosition = position;
            }
        }

        // Remove ended touches
        List<int> touchesToRemove = new List<int>();
        foreach (var kvp in activeTouches)
        {
            bool stillActive = false;
            for (int i = 0; i < touches.Count; i++)
            {
                if (touches[i].isInProgress && touches[i].touchId.ReadValue() == kvp.Key)
                {
                    stillActive = true;
                    break;
                }
            }
            if (!stillActive)
            {
                touchesToRemove.Add(kvp.Key);
            }
        }

        foreach (int id in touchesToRemove)
        {
            activeTouches.Remove(id);
        }
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

    // Public method to check which zone a position is in
    public bool IsInLookZone(Vector2 screenPosition)
    {
        return screenPosition.x > screenSplitX;
    }

    // Public method to change split ratio at runtime
    public void SetScreenSplit(float ratio)
    {
        screenSplitRatio = Mathf.Clamp(ratio, 0.3f, 0.7f);
        CalculateScreenSplit();
    }
}