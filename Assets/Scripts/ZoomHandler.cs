using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Zooms an orthographic camera with the mouse wheel / trackpad scroll, or with a two-finger pinch.
/// Zoom is anchored on the cursor or the pinch centre, and moving two fingers together pans.
/// Put it on the camera that renders the picture.
/// </summary>
[RequireComponent(typeof(Camera))]
public class ZoomHandler : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float minSize = 1f;
    [SerializeField, Min(0.01f)] private float maxSize = 10f;
    [Tooltip("Zoom factor per scroll notch (120 units on most mice).")]
    [SerializeField, Range(0.01f, 0.5f)] private float scrollStep = 0.1f;

    private Camera cam;
    private float prevPinchDist;
    private Vector2 prevPinchMid;
    [Tooltip("Pixels a single pointer must move before it counts as a pan instead of a tap.")]
    [SerializeField, Min(0f)] private float dragThreshold = 12f;

    /// <summary>Last time (unscaled) a pinch or pan was active. Taps started before this are not taps.</summary>
    public static float LastGestureTime { get; private set; } = -1f;

    private bool pinching;
    private bool pressActive, dragging;
    private Vector2 dragStart, dragPrev;
    private Vector3 homePos;
    private float homeSize;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        homePos = transform.position;
        homeSize = Mathf.Min(cam.orthographicSize, maxSize);
    }

    /// <summary>Snap back to the starting full-picture view.</summary>
    public void ResetView()
    {
        transform.position = homePos;
        cam.orthographicSize = homeSize;
        pressActive = dragging = pinching = false;
    }

    private void OnEnable() => EnhancedTouchSupport.Enable();
    private void OnDisable() => EnhancedTouchSupport.Disable();

    private void Update()
    {
        if (HandlePinch())
        {
            pressActive = false; // re-anchor when going back to one finger, so the view doesn't jump
            return;
        }
        HandleDrag();
        HandleScroll();
    }

    private void HandleDrag()
    {
        Vector2 pos;
        var touches = Touch.activeTouches;
        if (touches.Count == 1) pos = touches[0].screenPosition;
        else if (touches.Count == 0 && Mouse.current != null && Mouse.current.leftButton.isPressed)
            pos = Mouse.current.position.ReadValue();
        else
        {
            pressActive = dragging = false;
            return;
        }

        if (!pressActive)
        {
            pressActive = true;
            dragging = false;
            dragStart = dragPrev = pos;
            return;
        }

        // Only pan when zoomed in; at the start size the view is locked anyway.
        if (!dragging && cam.orthographicSize < homeSize - 0.0001f &&
            Vector2.Distance(pos, dragStart) > dragThreshold)
            dragging = true;

        if (dragging)
        {
            LastGestureTime = Time.unscaledTime;
            Vector3 delta = cam.ScreenToWorldPoint(dragPrev) - cam.ScreenToWorldPoint(pos);
            delta.z = 0f;
            transform.position = ClampToHome(transform.position + delta);
        }
        dragPrev = pos;
    }

    private void HandleScroll()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;
        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Approximately(scroll, 0f)) return;

        // Normalise: mice report ~120 per notch, trackpads report small values.
        float notches = Mathf.Abs(scroll) >= 60f ? scroll / 120f : scroll / 10f;
        float factor = Mathf.Pow(1f - scrollStep, notches);
        Vector2 p = mouse.position.ReadValue();
        ZoomAround(p, p, cam.orthographicSize * factor);
    }

    private bool HandlePinch()
    {
        var touches = Touch.activeTouches;
        if (touches.Count < 2)
        {
            pinching = false;
            return false;
        }

        LastGestureTime = Time.unscaledTime;
        Vector2 a = touches[0].screenPosition, b = touches[1].screenPosition;
        float dist = Vector2.Distance(a, b);
        Vector2 mid = (a + b) * 0.5f;

        if (!pinching || dist < 1f)
        {
            pinching = true;
            prevPinchDist = Mathf.Max(dist, 1f);
            prevPinchMid = mid;
            return true;
        }

        ZoomAround(prevPinchMid, mid, cam.orthographicSize * prevPinchDist / dist);
        prevPinchDist = dist;
        prevPinchMid = mid;
        return true;
    }

    /// <summary>Sets the size, keeping the world point under <paramref name="before"/> under <paramref name="after"/>.</summary>
    private void ZoomAround(Vector2 before, Vector2 after, float newSize)
    {
        Vector3 worldBefore = cam.ScreenToWorldPoint(before);
        cam.orthographicSize = Mathf.Clamp(newSize, minSize, maxSize);
        Vector3 worldAfter = cam.ScreenToWorldPoint(after);
        Vector3 delta = worldBefore - worldAfter;
        delta.z = 0f;
        transform.position = ClampToHome(transform.position + delta);
    }

    /// <summary>
    /// Keeps the view inside the starting (full picture) view. The smaller the zoom, the more room
    /// to move; as you zoom back out the camera slides toward its start, landing on it at the start size.
    /// </summary>
    private Vector3 ClampToHome(Vector3 pos)
    {
        float roomY = Mathf.Max(0f, homeSize - cam.orthographicSize);
        float roomX = roomY * cam.aspect;
        pos.x = Mathf.Clamp(pos.x, homePos.x - roomX, homePos.x + roomX);
        pos.y = Mathf.Clamp(pos.y, homePos.y - roomY, homePos.y + roomY);
        return pos;
    }
}
