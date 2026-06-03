using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float forwardSpeed = 6f;
    [SerializeField] private float laneDistance = 3f;
    [SerializeField] private float laneChangeSpeed = 12f;
    [SerializeField] private float jumpForce = 7f;

    [Header("Swipe")]
    [SerializeField] private float minSwipeDistance = 50f;

    private Rigidbody rb;

    private int currentLane = 1; // 0 = left, 1 = center, 2 = right
    private bool isGrounded;
    private bool isDead;

    private Vector2 startTouch;
    private Vector2 endTouch;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Фіксуємо обертання куба, щоб він не котився
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Update()
    {
        if (isDead) return;

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.A)) MoveLane(false);
        if (Input.GetKeyDown(KeyCode.D)) MoveLane(true);
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded) Jump();
#endif

        HandleSwipe();
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        // Розрахунок ліній: 0 -> -3, 1 -> 0, 2 -> 3
        float targetX = (currentLane - 1) * laneDistance;

        // Швидкість зміщення вбік
        float diffX = targetX - transform.position.x;
        float moveX = diffX * laneChangeSpeed;

        float moveY = rb.linearVelocity.y;

        // Стабілізація: якщо гравець на землі й не стрибає вгору,
        // фіксуємо швидкість Y, щоб уникнути мікро-відскоків і просідань колайдерів
        if (isGrounded && moveY < 0.1f)
        {
            moveY = 0f;
        }

        rb.linearVelocity = new Vector3(moveX, moveY, forwardSpeed);
    }

    private void HandleSwipe()
    {
        if (Input.touchCount == 0) return;

        Touch t = Input.GetTouch(0);

        if (t.phase == TouchPhase.Began)
            startTouch = t.position;

        if (t.phase == TouchPhase.Ended)
        {
            endTouch = t.position;

            Vector2 swipe = endTouch - startTouch;

            if (swipe.magnitude < minSwipeDistance)
                return;

            if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y))
            {
                MoveLane(swipe.x > 0);
            }
            else
            {
                if (swipe.y > 0 && isGrounded)
                    Jump();
            }
        }
    }

    private void MoveLane(bool right)
    {
        currentLane += right ? 1 : -1;
        currentLane = Mathf.Clamp(currentLane, 0, 2);
    }

    private void Jump()
    {
        // Повністю зануляємо вертикальний імпульс перед стрибком
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        isGrounded = false;
    }

    public void DisableController()
    {
        isDead = true;
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
    }

    // Робота із землею через фізичні колізії плити
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}