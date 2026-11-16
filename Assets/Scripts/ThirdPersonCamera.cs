using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Third-person orbit camera that smoothly follows a target.
/// Hold the right mouse button and drag to orbit, scroll to zoom.
/// Gamepad right stick also orbits. The camera pulls in when a wall blocks the view.
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [Tooltip("Point the camera looks at, relative to the target's position.")]
    [SerializeField] private Vector3 lookOffset = new Vector3(0f, 5f, 0f);

    [Header("Distance")]
    [SerializeField] private float distance = 40f;
    [SerializeField] private float minDistance = 15f;
    [SerializeField] private float maxDistance = 80f;
    [SerializeField] private float zoomSpeed = 0.002f;

    [Header("Orbit")]
    [SerializeField] private float yaw = 0f;
    [SerializeField] private float pitch = 25f;
    [SerializeField] private float minPitch = -5f;
    [SerializeField] private float maxPitch = 70f;
    [SerializeField] private float mouseSensitivity = 0.2f;
    [SerializeField] private float gamepadSensitivity = 120f;

    [Header("Smoothing")]
    [Tooltip("Lower = snappier follow.")]
    [SerializeField] private float followSmoothTime = 0.12f;

    [Header("Collision")]
    [SerializeField] private float collisionRadius = 1f;
    [SerializeField] private LayerMask collisionMask = ~0;

    private Vector3 smoothedFocus;
    private Vector3 focusVelocity;

    private void Start()
    {
        if (target != null)
            smoothedFocus = target.position + lookOffset;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        ReadOrbitInput();

        // Smoothly follow the target.
        Vector3 desiredFocus = target.position + lookOffset;
        smoothedFocus = Vector3.SmoothDamp(smoothedFocus, desiredFocus, ref focusVelocity, followSmoothTime);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 back = rotation * Vector3.back;

        // Pull the camera in if something is between it and the target.
        float actualDistance = distance;
        if (Physics.SphereCast(smoothedFocus, collisionRadius, back, out RaycastHit hit, distance,
                               collisionMask, QueryTriggerInteraction.Ignore)
            && !hit.transform.IsChildOf(target) && hit.transform != target)
        {
            actualDistance = Mathf.Max(hit.distance, 1f);
        }

        transform.SetPositionAndRotation(smoothedFocus + back * actualDistance, rotation);
    }

    private void ReadOrbitInput()
    {
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                yaw += delta.x * mouseSensitivity;
                pitch -= delta.y * mouseSensitivity;
            }

            float scroll = mouse.scroll.ReadValue().y;
            float zoom = Mathf.Clamp(scroll * zoomSpeed, -0.25f, 0.25f);
            distance = Mathf.Clamp(distance * (1f - zoom), minDistance, maxDistance);
        }

        Gamepad pad = Gamepad.current;
        if (pad != null)
        {
            Vector2 stick = pad.rightStick.ReadValue();
            yaw += stick.x * gamepadSensitivity * Time.deltaTime;
            pitch -= stick.y * gamepadSensitivity * Time.deltaTime;
        }

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }
}
