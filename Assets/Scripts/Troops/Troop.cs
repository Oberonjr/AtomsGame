using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Troop : MonoBehaviour
{
    public TroopStats TroopStats;
    [HideInInspector] public int CurrentHealth;
    [HideInInspector] public Troop Target;
    
    private NavMeshAgent _agent;
    private Animator _animator = null;
    
    public virtual void Attack(){}

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        if(TryGetComponent(out _animator))
            _animator = GetComponent<Animator>();
        CurrentHealth = TroopStats.MaxHealth;
    }
    
    public void TakeDamage(int damage)
    {
        CurrentHealth -= damage;
        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    public bool IsInRange(Troop target)
    {
        if (Vector3.Distance(transform.position, target.transform.position) <= TroopStats.AttackRange)
        {
            return true;
        }
        return false;
    }
    
    public void Die()
    {
        Destroy(gameObject);
    }
    
}
