using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Player stats
    [SerializeField] int maxHealth;
    [SerializeField] int currentHealth;

    // Movement
    [SerializeField] float moveSpeed;

    // Fire modes
    //Main fire
    [SerializeField] int mainFireAmmo;
    [SerializeField] int mainDamage;
    [SerializeField] private float nextMainFireTime;
    [SerializeField] private float mainFireDistance;
    private float mainFireCooldown;

    //Alt fire
    [SerializeField] int altFireAmmo;
    [SerializeField] int altDamage;
    [SerializeField] private float nextAltFireTime;
    [SerializeField] private float rocketDuration;
    [SerializeField] private float rocketSpeed;
    [SerializeField] private float maxExplosionRadius;
    [SerializeField] private float maxExplosionDistance;
    private float altFireCooldown;
    public bool rocketExploded = false;
    public bool rocketExplodeOnContact;
    public bool rocketExplodedOnEnemy;

    // References
    [SerializeField] Transform firePoint;
    [SerializeField] Transform rocketPoint;
    [SerializeField] GameObject altProjectile;
    [SerializeField] private LayerMask enemyLayerMask;
    private Rigidbody2D rb;
    private Vector2 facingDirection;
    
    // Animations
    Animator animator;
    [SerializeField] private LineRenderer _aimRenderer;
    [SerializeField] private LineRenderer _lineRenderer;
    // Mouse tracking
    private Vector2 mousePosition;
    private Camera _camera;

    void Start()
    {
        _camera = Camera.main;
        // Initialize player stats
        currentHealth = maxHealth;

        // Set sprite orientation
        transform.up = Vector3.up;
        
        //Set the animator
        animator = GetComponent<Animator>();
        
        //Set the RigidBody2D
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Update mouse position
        if (_camera != null) mousePosition = _camera.ScreenToWorldPoint(Input.mousePosition);
        //mousePosition.z = 0f;

        // Update animator
        UpdateAnimator();
        
        // Aim weapon
        Aim();
        
    }

    void FixedUpdate()
    {
        // Move player
        Move();

        
        
        //Set the Facing Direction
        //This works because we're looking at the XY plane with the camera viewing it from the Z-axis
        facingDirection = firePoint.up;
    }

    void Move()
    {
        // Move player based on input
        var moveX = Input.GetAxisRaw("Horizontal");
        var moveY = Input.GetAxisRaw("Vertical");
        
        Vector2 movement = new Vector2(moveX, moveY) * (moveSpeed * Time.deltaTime);
        rb.MovePosition(rb.position + movement);
        
        // Calculate direction vector from player to mouse
        Vector2 direction = mousePosition - rb.position;

        // Calculate angle between direction and player's facing direction
        float angle = Vector2.SignedAngle(Vector2.up, direction.normalized) - transform.eulerAngles.z;

        // Rotate player to face mouse
        transform.Rotate(Vector3.forward, angle);
        
    }

    void Aim()
    {
        // Aim weapon towards mouse position
        _aimRenderer.SetPosition(0, firePoint.position);
        _aimRenderer.SetPosition(1, firePoint.position + mainFireDistance * new Vector3(facingDirection.x, facingDirection.y, 0));

        // Visually represent the line when holding down the corresponding button
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            _aimRenderer.enabled = true;
        }
        else if(Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1))
        {
            _aimRenderer.enabled = false;
        }
    }

    public void FireMain()
    {
        // Check if we have any main ammo left
        if (mainFireAmmo > 0)
        {
            // Check if enough time has passed since last shot
            if (Time.time >= nextMainFireTime)
            {
                // Set the time for next shot
                nextMainFireTime = Time.deltaTime + mainFireCooldown;

                // Shoot a raycast in the direction the player is facing
                RaycastHit2D hit = Physics2D.Raycast(firePoint.position, facingDirection, mainFireDistance, enemyLayerMask);
                _lineRenderer.SetPosition(0, firePoint.position);
                
                //Check if the raycast hit an enemy
                if (hit.collider != null)
                {
                    
                    // Get the enemy component from the hit object
                    Enemy enemy = hit.collider.GetComponentInParent<Enemy>();
                    
                    // Check if the enemy component was found
                    if (enemy != null)
                    {
                        // Damage the enemy
                        enemy.TakeDamage(mainDamage);
                    
                        // Play the hit sound effect
                        //hitSound.Play();
                    }
                    _lineRenderer.SetPosition(1, hit.point);
                }
                else
                {
                    _lineRenderer.SetPosition(1, firePoint.position + mainFireDistance * new Vector3(facingDirection.x, facingDirection.y, 0));
                }
    
                //Draw the line to represent the shot
                StartCoroutine(drawLine());
                
                // Decrement main ammo
                mainFireAmmo--;
            }
        }
    }
    


    public IEnumerator FireAlt()
    {
        // Fire alt weapon if cooldown is over and there is ammo
        // Check if enough time has passed since last alt fire
        if (Time.time >= nextAltFireTime)
        {
            // Instantiate the rocket projectile at the fire point position and rotation
            GameObject rocket = Instantiate(altProjectile, rocketPoint.position, rocketPoint.rotation);
        
            // Set the rocket's velocity in the direction the player is facing
            Rigidbody2D rocketRb = rocket.GetComponent<Rigidbody2D>();
            rocketRb.velocity = transform.up * rocketSpeed;

            // Store the rocket's instantiation time
            float rocketInstantiationTime = Time.time;

            // Update the next alt fire time to the current time plus the alt fire cooldown
            nextAltFireTime = Time.time + altFireCooldown;

            // Play the alt fire sound
            //altFireAudio.Play();

            // Keep track of the rocket's expiration time
            float rocketExpirationTime = rocketInstantiationTime + rocketDuration;

            // Keep track of whether the rocket has already exploded
            

            // Loop until the rocket expires or explodes
            while (Time.time <= rocketExpirationTime && !rocketExploded)
            {
                // Check if the rocket has been destroyed
                if (rocket == null)
                {
                    break;
                }


                
                // Wait for the next fixed update
                yield return new WaitForFixedUpdate();
            }

            // If the rocket hasn't exploded yet, explode it at its expiration time
            if (!rocketExploded)
            {
                rocketExplodeOnContact = true;
                rocketExplodedOnEnemy = false;
                yield return new WaitForSeconds(rocketDuration - (Time.time - rocketInstantiationTime));
            }
        }
    }

    void UpdateAnimator()
    {
        // Update animator parameters based on player state
        
        // Aim weapon main
        if (Input.GetMouseButtonDown(0))
        {
            animator.SetBool("AimMain", true);
        }
        // Fire weapon main
        if (Input.GetMouseButtonUp(0))
        {
            // Play the shooting animation
            animator.SetTrigger("ShootMain");
            animator.SetBool("AimMain", false);
            
        }
        
        //Aim weapon alt
        if (Input.GetMouseButtonDown(1))
        {
            animator.SetBool("AimAlt", true);
        }
        //Fire weapon alt
        if (Input.GetMouseButtonUp(1))
        {
            // Play the shooting animation
            animator.SetTrigger("ShootAlt");
            animator.SetBool("AimAlt", false);
            
        }
    }

    public void TakeDamage(int damage)
    {
        // Reduce health by given damage
        currentHealth -= damage;
    }

    void Die()
    {
        // Trigger death animation and destroy object
        // ...
    }

    IEnumerator drawLine()
    {
        _lineRenderer.enabled = true;
        yield return new WaitForSeconds(0.05f);
        _lineRenderer.enabled = false;
    }

    public void ExplodeRocket(GameObject rocket)
    {
        // Check if the rocket should explode based on contact with enemies or player input
        if (Input.GetMouseButtonDown(1) || rocketExplodeOnContact && rocketExplodedOnEnemy)
        {
            // Set the rocket's explosion radius to its max value
            float explosionRadius = maxExplosionRadius;

            // Find all colliders within the explosion radius and deal damage to any enemies
            Collider2D[] colliders = Physics2D.OverlapCircleAll(rocket.transform.position, explosionRadius, enemyLayerMask);
            foreach (Collider2D collider in colliders)
            {
                Enemy enemy = collider.GetComponentInParent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(altDamage);
                }
            }

            // Destroy the rocket and the explosion effect
            Destroy(rocket);
            //Destroy(explosion, altFireExplosionDuration);

            // Set the rocket as exploded to exit the loop
            rocketExploded = true;
        }
    }
        
}
