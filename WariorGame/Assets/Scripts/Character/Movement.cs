using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    private PlayerInput playerInput;
    private InputAction moveAction;
    private Animator animator;
    private Rigidbody rb;

    [SerializeField] private float speed = 5f;

    void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions.FindAction("Move");
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    void MovePlayer()
    {
        if (moveAction == null) return;

        Vector2 direction = moveAction.ReadValue<Vector2>();
        Vector3 movement = new Vector3(direction.x, 0f, 0f) * speed;
        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, 0f);

        float targetAngle = transform.rotation.eulerAngles.y;

        if (movement.x < 0f)
        {
            targetAngle = -90f;
        }
        else if (movement.x > 0f)
        {
            targetAngle = 90f;
        }

        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, targetAngle, 0f), Time.fixedDeltaTime * 15f);

        if (movement == Vector3.zero)
        {
            animator.SetFloat("Speed", 0f, 0.01f, Time.fixedDeltaTime);
        }
        else if (moveAction.IsPressed())
        {
            animator.SetFloat("Speed", 1f, 0.01f, Time.fixedDeltaTime);
        }
    }
}