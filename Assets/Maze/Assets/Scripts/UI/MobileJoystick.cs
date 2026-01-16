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

    private Vector2 input = Vector2.zero;
    private Vector2 centerPoint;

    // Public properties to get movement values
    public float Horizontal => input.x;  // Maps to X axis
    public float Vertical => input.y;    // Maps to Z axis
    public Vector2 Direction => input;

    void Start()
    {
        centerPoint = joystickBackground.position;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground,
            eventData.position,
            eventData.pressEventCamera,
            out position
        );

        // Normalize the position
        position = Vector2.ClampMagnitude(position, handleRange);

        // Update handle position
        joystickHandle.anchoredPosition = position;

        // Calculate input (-1 to 1 range)
        input = position / handleRange;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (returnToCenter)
        {
            joystickHandle.anchoredPosition = Vector2.zero;
        }
        input = Vector2.zero;
    }

    // Get movement for first-person controller (x, z plane)
    public Vector3 GetMovementInput()
    {
        return new Vector3(input.x, 0f, input.y);
    }
}