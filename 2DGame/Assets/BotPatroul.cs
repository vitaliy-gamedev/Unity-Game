using UnityEngine;

public class BBotPatroul : MonoBehaviour
{
    public float detectionRadius = 5f;
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;
    public float pointReachDistance = 0.2f;

    public Vector2 offsetA = new Vector2(-3, 0);
    public Vector2 offsetB = new Vector2(3, 0);

    public float forgetTime = 2f;
    public float recheckDelay = 0.3f; // 👈 важливо!

    public Transform player;

    private Rigidbody2D rb;
    private Vector2 pointA;
    private Vector2 pointB;
    private Vector2 target;

    private float loseTimer;
    private float recheckTimer;

    private enum State { Patrol, Chase, Return }
    private State state;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        Vector2 start = rb.position;
        pointA = start + offsetA;
        pointB = start + offsetB;

        target = pointB;
        state = State.Patrol;
    }

    private void FixedUpdate()
    {
        switch (state)
        {
            case State.Patrol:
                Patrol();
                break;

            case State.Chase:
                Chase();
                break;

            case State.Return:
                ReturnToPatrol();
                break;
        }
    }

    void Patrol()
    {
        if (CanSeePlayer())
        {
            state = State.Chase;
            loseTimer = forgetTime;
            return;
        }

        Move(target, patrolSpeed);

        if (Vector2.Distance(rb.position, target) < pointReachDistance)
            target = (target == pointA) ? pointB : pointA;
    }

    void Chase()
    {
        recheckTimer -= Time.fixedDeltaTime;

        if (recheckTimer <= 0f)
        {
            recheckTimer = 0.3f;

            if (CanSeePlayer())
            {
                loseTimer = forgetTime;
            }
            else
            {
                loseTimer -= 0.3f;
            }
        }

        if (loseTimer <= 0f)
        {
            state = State.Return;
            return;
        }

        if (player != null)
            Move(player.position, chaseSpeed);
    }

    void ReturnToPatrol()
    {
        Move(target, patrolSpeed);

        if (Vector2.Distance(rb.position, target) < pointReachDistance)
            state = State.Patrol;
    }

    void Move(Vector2 pos, float speed)
    {
        rb.MovePosition(
            Vector2.MoveTowards(rb.position, pos, speed * Time.fixedDeltaTime)
        );
    }

    bool CanSeePlayer()
    {
        return player &&
               Vector2.Distance(rb.position, player.position) <= detectionRadius;
    }
}