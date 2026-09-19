using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public sealed class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 2.5f;
    [SerializeField, Min(0f)] private float rotationSpeed = 12f;

    [Header("Mouse Movement")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private LayerMask groundMask;
    [SerializeField, Min(0f)] private float destinationStopDistance = 0.15f;
    [SerializeField] private GameObject moveMarkerPrefab;

    [Header("Gravity")]
    [SerializeField, Min(0f)] private float gravityMultiplier = 1f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField, Min(0f)] private float animationDampTime = 0.1f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private CharacterController characterController;
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
        characterController = GetComponent<CharacterController>();
        PlayerInput playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions.FindAction("Move", throwIfNotFound: true);
        pointAction = playerInput.actions.FindAction("Point", throwIfNotFound: true);
        moveToCursorAction = playerInput.actions.FindAction("MoveToCursor", throwIfNotFound: true);

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (worldCamera == null)
            worldCamera = Camera.main;

        CreateMoveMarker();
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
            return;

        HandleMouseDestination();
        Vector3 movement = GetMovementDirection();
        ApplyGravity();

        Vector3 velocity = movement * moveSpeed + Vector3.up * verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);
        RotateTowardsMovement(movement);

        if (animator != null)
            animator.SetFloat(SpeedHash, movement.magnitude, animationDampTime, Time.deltaTime);
    }

    private void HandleMouseDestination()
    {
        if (!moveToCursorAction.IsPressed() || worldCamera == null)
            return;

        Ray ray = worldCamera.ScreenPointToRay(pointAction.ReadValue<Vector2>());

        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundMask, QueryTriggerInteraction.Ignore))
            return;

        destination = hit.point;
        destination.y = transform.position.y;
        hasDestination = true;

        if (moveMarker == null)
            return;

        moveMarker.transform.position = hit.point + Vector3.up * 0.02f;
        moveMarker.SetActive(true);

        if (moveToCursorAction.WasPressedThisFrame() && clickBurst != null)
        {
            clickBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            clickBurst.Play(true);
        }
    }

    private Vector3 GetMovementDirection()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();

        if (input.sqrMagnitude > 0.01f)
        {
            CancelDestination();
            return Vector3.ClampMagnitude(new Vector3(input.x, 0f, input.y), 1f);
        }

        if (!hasDestination)
            return Vector3.zero;

        Vector3 direction = destination - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= destinationStopDistance * destinationStopDistance)
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
            moveMarker.SetActive(false);
    }

    private void CreateMoveMarker()
    {
        if (moveMarkerPrefab == null)
            return;

        moveMarker = Instantiate(moveMarkerPrefab);
        Transform burstTransform = moveMarker.transform.Find("ClickBurst");

        if (burstTransform != null)
            clickBurst = burstTransform.GetComponent<ParticleSystem>();

        if (clickBurst == null)
            Debug.LogWarning("MoveMarker does not contain a ClickBurst ParticleSystem.", moveMarker);

        moveMarker.SetActive(false);
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        else
            verticalVelocity += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
    }

    private void RotateTowardsMovement(Vector3 movement)
    {
        if (movement.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(movement, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    public void AddMoveSpeed(float amount)
    {
        moveSpeed += amount;
    }
}
