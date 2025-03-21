using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketCollider : MonoBehaviour
{
    private PlayerController pc;

    private void Awake()
    {
        pc = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        Debug.Log(col);
        pc.rocketExplodeOnContact = true;
        if (col.CompareTag("Enemy"))
        {
            pc.rocketExplodedOnEnemy = true;
        }
    }

    private void Update()
    {
        pc.ExplodeRocket(this.gameObject);
    }


}
