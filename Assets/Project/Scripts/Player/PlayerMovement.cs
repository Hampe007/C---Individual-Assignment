using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public sealed class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)]
    private float moveSpeed = 2.5f;

    [SerializeField, Min(0f)]
    private float rotationSpeed = 12f;

    [Header("Mouse Movement")]
    [SerializeField]
    private Camera worldCamera;

    [SerializeField]
    private LayerMask groundMask;

    [SerializeField, Min(0f)]
    private float destinationStopDistance = 0.15f;

    [SerializeField]
    private GameObject moveMarkerPrefab;

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
    private InputAction pointAction;
    private InputAction moveToCursorAction;

    private GameObject moveMarker;

    private ParticleSystem clickBurst;
    
    private Vector3 destination;
    private bool hasDestination;

    private float verticalVelocity;

    private void Awake()
    {
        characterController =
            GetComponent<CharacterController>();

        playerInput =
            GetComponent<PlayerInput>();

        moveAction = playerInput.actions.FindAction(
            "Move",
            throwIfNotFound: true
        );

        pointAction = playerInput.actions.FindAction(
            "Point",
            throwIfNotFound: true
        );

        moveToCursorAction =
            playerInput.actions.FindAction(
                "MoveToCursor",
                throwIfNotFound: true
            );

        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>();
        }

        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        CreateMoveMarker();
    }

    private void Update()
    {
        HandleMouseDestination();

        Vector3 horizontalMovement =
            GetMovementDirection();

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

    private void HandleMouseDestination()
    {
        if (!moveToCursorAction.IsPressed() || worldCamera == null)
            return;

        Vector2 mousePosition = pointAction.ReadValue<Vector2>();
        Ray ray = worldCamera.ScreenPointToRay(mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundMask, QueryTriggerInteraction.Ignore))
            return;

        destination = hit.point;
        destination.y = transform.position.y;
        hasDestination = true;

        if (moveToCursorAction.WasPressedThisFrame())
        {
            ShowMoveMarker(hit.point);
        }
        else if (moveMarker != null)
        {
            Vector3 markerPosition = hit.point;
            markerPosition.y += 0.02f;

            moveMarker.transform.position = markerPosition;
            moveMarker.SetActive(true);
        }
    }

    private Vector3 GetMovementDirection()
    {
        Vector2 input =
            moveAction.ReadValue<Vector2>();

        // Keyboard/gamepad input overrides click movement.
        if (input.sqrMagnitude > 0.01f)
        {
            CancelDestination();

            Vector3 manualMovement =
                new Vector3(
                    input.x,
                    0f,
                    input.y
                );

            return Vector3.ClampMagnitude(
                manualMovement,
                1f
            );
        }

        if (!hasDestination)
            return Vector3.zero;

        Vector3 direction =
            destination - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <=
            destinationStopDistance *
            destinationStopDistance)
        {
            CancelDestination();
            return Vector3.zero;
        }

        return direction.normalized;
    }

    private void CancelDestination()
    {
        hasDestination = false;

        if (moveMarker != null)
        {
            moveMarker.SetActive(false);
        }
    }

    private void CreateMoveMarker()
    {
        if (moveMarkerPrefab == null)
            return;

        moveMarker = Instantiate(moveMarkerPrefab);

        Transform burstTransform =
            moveMarker.transform.Find("ClickBurst");

        if (burstTransform != null)
        {
            clickBurst =
                burstTransform.GetComponent<ParticleSystem>();
        }

        if (clickBurst == null)
        {
            Debug.LogWarning(
                "MoveMarker does not contain a ClickBurst ParticleSystem.",
                moveMarker
            );
        }

        moveMarker.SetActive(false);
    }

    private void ShowMoveMarker(Vector3 position)
    {
        if (moveMarker == null)
            return;

        position.y += 0.02f;

        moveMarker.transform.position = position;
        moveMarker.SetActive(true);

        if (clickBurst == null)
            return;

        // Restart the burst from the beginning on every click.
        clickBurst.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        clickBurst.Play(true);
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded &&
            verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
            return;
        }

        verticalVelocity +=
            Physics.gravity.y *
            gravityMultiplier *
            Time.deltaTime;
    }

    private void RotateTowardsMovement(
        Vector3 movement
    )
    {
        if (movement.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                movement,
                Vector3.up
            );

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
    }

    private void UpdateAnimation(
        Vector3 movement
    )
    {
        if (animator == null)
            return;

        animator.SetFloat(
            SpeedHash,
            movement.magnitude,
            animationDampTime,
            Time.deltaTime
        );
    }
}