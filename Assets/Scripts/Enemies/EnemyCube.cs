﻿using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections;

public class EnemyCube : Enemy {

    public Transform[] WayPoints;

    private Rigidbody rbd;
    private int currentPointIndex = 0;
    private Transform currentPoint { get { return WayPoints[currentPointIndex]; } }
    private NavMeshAgent agent;
	
	void Start ()
    {
        rbd = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        agent.SetDestination(currentPoint.position);

        StartCoroutine(Shoot());
    }

    void Update()
    {
        if (agent.remainingDistance < 1)
        {
            if (++currentPointIndex >= WayPoints.Length)
            {
                currentPointIndex = 0;
            }
            agent.SetDestination(currentPoint.position);
        }
    }

    void OnTriggerEnter(Collider col)
    {
        if (col == currentPoint.GetComponent<Collider>())
        {
            currentPointIndex++;
            if (currentPointIndex >= WayPoints.Length)
            {
                currentPointIndex = 0;
            }
        }
    }

    IEnumerator Shoot()
    {
        yield return new WaitForSeconds(3);
        //agent.SetDestination(transform.position + new Vector3(UnityEngine.Random.r))
        Projectile.Fire<Fireball>(10f, transform.position + Vector3.up * 2f, Quaternion.FromToRotation(transform.position, Player.Instance.transform.position), gameObject);
        StartCoroutine(Shoot());
    }

    protected override void OnDied()
    {
        Destroy(this);
        Destroy(GetComponent<NavMeshAgent>());
        Destroy(GetComponent<BoxCollider>());
        
        Array.ForEach(GetComponentsInChildren<BoxCollider>(), col => col.enabled = true);
        Array.ForEach(GetComponentsInChildren<Rigidbody>(), r => {
            r.isKinematic = false;
            r.AddExplosionForce(100f, transform.position, 10f);
        });
    }
}
