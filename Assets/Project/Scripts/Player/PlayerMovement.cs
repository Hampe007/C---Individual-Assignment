using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public sealed class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)]
    private float moveSpeed = 5f;

    [SerializeField, Min(0f)]
    private float rotationSpeed = 12f;

    [Header("Gravity")]
    [SerializeField, Min(0f)]
    private float gravityMultiplier = 1f;

    [Header("Animation")]
    [SerializeField]
    private Animator animator;

    [SerializeField, Min(0f)]
    private float animationDampTime = 0.1f;

    private static readonly int SpeedHash =
        Animator.StringToHash("Speed");

    private CharacterController characterController;
    private PlayerInput playerInput;
    private InputAction moveAction;

    private float verticalVelocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        moveAction = playerInput.actions.FindAction(
            "Move",
            throwIfNotFound: true
        );

        // Animator lives on the visual child, not the Player root.
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator == null)
        {
            Debug.LogWarning(
                "PlayerMovement could not find an Animator in the Player hierarchy.",
                this
            );
        }
    }

    private void Update()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();

        Vector3 horizontalMovement = new Vector3(
            input.x,
            0f,
            input.y
        );

        horizontalMovement =
            Vector3.ClampMagnitude(horizontalMovement, 1f);

        ApplyGravity();

        Vector3 velocity =
            horizontalMovement * moveSpeed +
            Vector3.up * verticalVelocity;

        characterController.Move(
            velocity * Time.deltaTime
        );

        RotateTowardsMovement(horizontalMovement);
        UpdateAnimation(horizontalMovement);
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded &&
            verticalVelocity < 0f)
        {
            // Small downward force keeps the controller grounded.
            verticalVelocity = -2f;
            return;
        }

        verticalVelocity +=
            Physics.gravity.y *
            gravityMultiplier *
            Time.deltaTime;
    }

    private void RotateTowardsMovement(Vector3 movement)
    {
        if (movement.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                movement,
                Vector3.up
            );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void UpdateAnimation(Vector3 movement)
    {
        if (animator == null)
            return;

        float speed = movement.magnitude;

        animator.SetFloat(
            SpeedHash,
            speed,
            animationDampTime,
            Time.deltaTime
        );
    }
}