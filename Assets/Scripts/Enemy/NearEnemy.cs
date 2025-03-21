using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NearEnemy : Enemy
{
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float slashCooldown = 2f;
    [SerializeField] private float thrustCooldown = 3f;


    private bool canAttackSlash = true;
    private bool canAttackThrust = true;
    private bool isAttacking = false;

    protected override void Awake()
    {
        base.Awake();

    }

    protected override void Update()
    {
        base.Update();
        if(!isAttacking)MoveTowardsPlayer();
        Attack();
    }

    private void MoveTowardsPlayer()
    {
        transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, moveSpeed * Time.deltaTime);
        Vector2 direction = playerTransform.position - transform.position;
        transform.up = direction;
    }

    private void Attack()
    {
        if (Vector2.Distance(transform.position, playerTransform.position) <= attackRange)
        {
            isAttacking = true;
            if (canAttackSlash)
            {
                StartCoroutine(SlashAttack());
            }
            else if (canAttackThrust)
            {
                StartCoroutine(ThrustAttack());
            }
        }
    }

    private IEnumerator SlashAttack()
    {
        canAttackSlash = false;
        animator.SetTrigger("Slash");
        yield return new WaitForSeconds(slashCooldown);
        slashCooldown = Random.Range(3f, 7f);
        canAttackSlash = true;
        isAttacking = false;
    }

    private IEnumerator ThrustAttack()
    {
        canAttackThrust = false;
        animator.SetTrigger("Thrust");
        yield return new WaitForSeconds(thrustCooldown);
        thrustCooldown = Random.Range(3f, 7f);
        canAttackThrust = true;
        isAttacking = false;
    }
}