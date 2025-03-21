using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class FarEnemy : Enemy
{
    [SerializeField] private float longRange = 15f;
    [SerializeField] private float shortRange = 5f;
    [SerializeField] private float reloadTime = 2f;
    [SerializeField] private int maxAmmo = 10;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private Transform firePoint;

    private float distanceFromPlayer;
    private float currentAmmo;
    private bool isReloading;
    private const int FAR_RANGE = 1;
    private const int CLOSE_RANGE = 2;
    private int FIRE_MODE;
    
    private const int STAND_STILL = 0;
    private const int BACK_AWAY = 1;
    private const int CLOSE_IN = 2;
    private int MOVE_AXIS;

    // Implement the tracking and movement towards the player
    protected void TrackAndMove()
    {
        transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, moveSpeed * Time.deltaTime);
        Vector2 direction = playerTransform.position - transform.position;
        transform.up = direction * MOVE_AXIS;
    }

    

    // Implement the firing behavior
    public void Fire()
    {
        int damage;
        int fireRate;
       
        switch (FIRE_MODE)
        {
            case FAR_RANGE:

                damage = 20;
                fireRate = 1;
                break;

            case CLOSE_RANGE:

                damage = 5;
                fireRate = 3;
                break;
        }



        currentAmmo--;
    }
    

    // Implement the reloading behavior
    private IEnumerator Reload()
    {
        // TODO: Implement reloading behavior

        yield return new WaitForSeconds(reloadTime);
    }

    // Implement the FarEnemy's Update method
    protected override void Update()
    {
        base.Update();

        if (!isReloading)
        { 
            distanceFromPlayer = Vector2.Distance(transform.position, playerTransform.position);
            if (distanceFromPlayer <= longRange && distanceFromPlayer > shortRange)
            {
                MOVE_AXIS = STAND_STILL;
                FIRE_MODE = FAR_RANGE;
            }
            else if (distanceFromPlayer <= shortRange)
            {
                MOVE_AXIS = BACK_AWAY;
                FIRE_MODE = CLOSE_RANGE;
            }
            else
            {
                MOVE_AXIS = CLOSE_IN;
            }
            
            TrackAndMove();
        }
    }
}