using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Attack : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private Animator animator;
    [SerializeField] private float attackCooldown = 1f;

    private InputAction attackAction;
    private bool canAttack = true;

    void Start()
    {
        attackAction = playerInput.actions.FindAction("Attack");

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Update()
    {
        AttackMethod();
    }

    private void AttackMethod()
    {
        if (attackAction == null) return;

        if (attackAction.triggered && canAttack)
        {
            animator.SetTrigger("IsAttack");
            StartCoroutine(AttackCooldownRoutine());
        }
    }

    private IEnumerator AttackCooldownRoutine()
    {
        canAttack = false;

        yield return new WaitForSeconds(attackCooldown);

        canAttack = true;
    }
}