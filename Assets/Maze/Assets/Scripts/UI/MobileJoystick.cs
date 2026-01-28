using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Joystick Components")]
    [SerializeField] private RectTransform joystickBackground;
    [SerializeField] private RectTransform joystickHandle;

    [Header("Settings")]
    [SerializeField] private float handleRange = 50f;
    [SerializeField] private bool returnToCenter = true;
    [SerializeField] private float deadZone = 0.1f; // Minimum input threshold

    private Vector2 input = Vector2.zero;
    private Vector2 centerPoint;
    private bool isDragging = false;
    private int currentPointerId = -1;

    // Public properties to get movement values
    public float Horizontal => Mathf.Abs(input.x) > deadZone ? input.x : 0f;
    public float Vertical => Mathf.Abs(input.y) > deadZone ? input.y : 0f;
    public Vector2 Direction => input;
    public bool IsDragging => isDragging;

    void Start()
    {
        centerPoint = joystickBackground.position;

        // Ensure the joystick blocks raycasts properly
        if (joystickBackground.GetComponent<Image>() != null)
        {
            joystickBackground.GetComponent<Image>().raycastTarget = true;
        }
        if (joystickHandle.GetComponent<Image>() != null)
        {
            joystickHandle.GetComponent<Image>().raycastTarget = true;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Only accept if we don't have an active pointer
        if (currentPointerId == -1)
        {
            currentPointerId = eventData.pointerId;
            isDragging = true;
            OnDrag(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Only process if this is our tracked pointer
        if (eventData.pointerId != currentPointerId)
        {
            return;
        }

        Vector2 position;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground,
            eventData.position,
            eventData.pressEventCamera,
            out position))
        {
            // Normalize the position
            position = Vector2.ClampMagnitude(position, handleRange);

            // Update handle position
            joystickHandle.anchoredPosition = position;

            // Calculate input (-1 to 1 range)
            input = position / handleRange;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Only process if this is our tracked pointer
        if (eventData.pointerId != currentPointerId)
        {
            return;
        }

        currentPointerId = -1;
        isDragging = false;

        if (returnToCenter)
        {
            joystickHandle.anchoredPosition = Vector2.zero;
        }
        input = Vector2.zero;
    }

    // Optional: Reset joystick if needed
    public void ResetJoystick()
    {
        currentPointerId = -1;
        isDragging = false;
        joystickHandle.anchoredPosition = Vector2.zero;
        input = Vector2.zero;
    }

    // Get movement for first-person controller (x, z plane)
    public Vector3 GetMovementInput()
    {
        return new Vector3(Horizontal, 0f, Vertical);
    }

    // Check if a screen position is within the joystick area
    public bool IsWithinJoystickArea(Vector2 screenPosition)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
            joystickBackground,
            screenPosition,
            null
        );
    }
}