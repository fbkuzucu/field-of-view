using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChaController : MonoBehaviour
{
    //variables which are changeable by users
    [SerializeField] float moveSpeed = 15;
    [SerializeField] Vector3 rotationSpeed = new Vector3(0, 80, 0);

    Rigidbody chaRigidbody;
    Vector2 inputDirection;
    Vector3 velocity;

    void Start()
    {
        chaRigidbody = GetComponent<Rigidbody>();
    }

    void Update()
    {
        velocity = transform.forward * moveSpeed * Time.deltaTime;
        //get inputs from keyboard
        Vector2 inputs = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        //used normalized to make smoothly movement
        inputDirection = inputs.normalized;
    }

    void FixedUpdate()
    {
        //using for rotation
        Quaternion rot = Quaternion.Euler(inputDirection.x * rotationSpeed * Time.fixedDeltaTime);
        chaRigidbody.MoveRotation(chaRigidbody.rotation * rot);
        //using for direct movement
        chaRigidbody.MovePosition(chaRigidbody.position + velocity * inputDirection.y * Time.fixedDeltaTime);
    }
}
