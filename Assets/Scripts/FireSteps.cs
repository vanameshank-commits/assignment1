using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FireSteps : MonoBehaviour
{
    [Header("Fire Settings")]
    public GameObject firePrefab;
    public Transform spawnPoint; // Drag your Empty GameObject here
    public float stepDistance = 1.2f;
    public float fireLifetime = 2.5f;
    public int initialPoolSize = 10;

    private Vector3 lastStepPosition;
    private CharacterController controller;
    private Queue<GameObject> firePool = new Queue<GameObject>();

    void Start()
    {
        controller = GetComponent<CharacterController>();
        lastStepPosition = transform.position;

        // Pre-spawn the fire objects
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewFireObject();
        }
    }

    void Update()
    {
        if (Vector3.Distance(transform.position, lastStepPosition) >= stepDistance)
        {
            if (controller.isGrounded)
            {
                SpawnFire();
            }
            else
            {
                lastStepPosition = transform.position;
            }
        }
    }

    void CreateNewFireObject()
    {
        GameObject fire = Instantiate(firePrefab);
        fire.transform.SetParent(null); // Keep them in the world, not attached to the player
        fire.SetActive(false);
        firePool.Enqueue(fire);
    }

    void SpawnFire()
    {
        lastStepPosition = transform.position;

        if (firePool.Count == 0)
        {
            CreateNewFireObject();
        }

        GameObject fire = firePool.Dequeue();

        // Use the visual spawn point instead of math. Fallback to player center if empty.
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;

        fire.transform.position = spawnPos;
        fire.transform.rotation = transform.rotation;
        fire.SetActive(true);

        StartCoroutine(ReturnToPool(fire, fireLifetime));
    }

    private IEnumerator ReturnToPool(GameObject fire, float delay)
    {
        yield return new WaitForSeconds(delay);
        fire.SetActive(false);
        firePool.Enqueue(fire);
    }
}