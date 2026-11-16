using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Moves the player with WASD / Arrow keys (or gamepad left stick)
/// and supports jump + double jump with Space (or gamepad South button).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Horizontal movement speed in units per second.")]
    [SerializeField] private float moveSpeed = 15f;

    [Tooltip("Movement is relative to this camera. Uses Camera.main if empty.")]
    [SerializeField] private Transform cameraTransform;

    [Header("Jump")]
    [Tooltip("Height of the first jump in units.")]
    [SerializeField] private float jumpHeight = 8f;
    [Tooltip("Height of the double jump in units.")]
    [SerializeField] private float doubleJumpHeight = 6f;
    [Tooltip("Total jumps allowed before landing (2 = double jump).")]
    [SerializeField] private int maxJumps = 2;
    [Tooltip("Extra gravity multiplier while falling, for a snappier jump.")]
    [SerializeField] private float fallGravityMultiplier = 2f;

    [Header("Ground Check")]
    [Tooltip("Surfaces whose normal is steeper than this angle don't count as ground.")]
    [SerializeField] private float maxGroundAngle = 45f;

    private Rigidbody rb;
    private Vector2 moveInput;
    private bool jumpRequested;
    private bool isGrounded;
    private int jumpsUsed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // keep the cube from tipping over
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Update()
    {
        moveInput = ReadMoveInput();

        if (JumpPressedThisFrame())
            jumpRequested = true;
    }

    private void FixedUpdate()
    {
        // Horizontal movement: set X/Z velocity directly, keep Y from physics.
        Vector3 velocity = rb.linearVelocity;
        Vector3 move = GetCameraRelativeMove(moveInput);
        velocity.x = move.x * moveSpeed;
        velocity.z = move.z * moveSpeed;

        // Reset jumps when standing on the ground.
        if (isGrounded && velocity.y <= 0.01f)
            jumpsUsed = 0;

        // Jump / double jump.
        if (jumpRequested)
        {
            jumpRequested = false;

            // Walked off a ledge without jumping: the first jump is spent.
            if (!isGrounded && jumpsUsed == 0)
                jumpsUsed = 1;

            if (jumpsUsed < maxJumps)
            {
                float height = jumpsUsed == 0 ? jumpHeight : doubleJumpHeight;
                velocity.y = Mathf.Sqrt(2f * height * -Physics.gravity.y);
                jumpsUsed++;
                isGrounded = false;
            }
        }

        rb.linearVelocity = velocity;

        // Heavier gravity while falling.
        if (rb.linearVelocity.y < 0f)
            rb.AddForce(Physics.gravity * (fallGravityMultiplier - 1f), ForceMode.Acceleration);

        // Recomputed by collision callbacks during the next physics step.
        isGrounded = false;
    }

    /// <summary>Converts 2D input into a world direction based on where the camera faces.</summary>
    private Vector3 GetCameraRelativeMove(Vector2 input)
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;
        if (cameraTransform != null)
        {
            forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        }

        Vector3 move = forward * input.y + right * input.x;
        return Vector3.ClampMagnitude(move, 1f);
    }

    private void OnCollisionStay(Collision collision)
    {
        float minUp = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y >= minUp)
            {
                isGrounded = true;
                return;
            }
        }
    }

    private static Vector2 ReadMoveInput()
    {
        Vector2 input = Vector2.zero;

        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  input.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  input.y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    input.y += 1f;
        }

        Gamepad pad = Gamepad.current;
        if (pad != null)
            input += pad.leftStick.ReadValue();

        return Vector2.ClampMagnitude(input, 1f);
    }

    private static bool JumpPressedThisFrame()
    {
        bool pressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
            pressed = true;
        return pressed;
    }
}
