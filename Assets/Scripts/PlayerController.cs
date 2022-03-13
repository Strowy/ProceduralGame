using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed;
    public float rotationSpeed;
    public float jumpForce;
    public float gravMultiplier;
    public CharacterController controller;

    private Vector3 moveDirection;
    private float verticalMove;
    
    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
        verticalMove = 0;
    }

    // Update is called once per frame
    void Update()
    {

        // Applying gravity. Will cause continuous downward acceleration when in midair
        if (!controller.isGrounded) { verticalMove += Physics.gravity.y * gravMultiplier * Time.deltaTime; }

        // Negate fall speed when impacting the ground
        if (controller.isGrounded && verticalMove < 0) { verticalMove = 0; }
        
        // Can only jump if already touching the ground
        if (controller.isGrounded && (Input.GetButtonDown("Jump") || Input.GetButton("Jump"))) { verticalMove = jumpForce; }

        this.transform.Rotate(new Vector3(0, Input.GetAxis("Horizontal") * rotationSpeed * Time.deltaTime, 0));

        moveDirection = transform.TransformDirection(Vector3.forward);
        moveDirection = moveDirection * Input.GetAxis("Vertical") * moveSpeed;
        moveDirection.y = verticalMove;
        controller.Move(moveDirection * Time.deltaTime);

        
    }
}
