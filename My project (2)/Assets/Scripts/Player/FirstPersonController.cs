using System;
using UnityEngine;
using static UnityEngine.InputSystem.InputAction;

[RequireComponent(typeof(Rigidbody))]
public class FirstPersonController : MonoBehaviour
{
    public bool CanMove { get; private set; } = true;

    [Header("Movement Parameters")]
    [SerializeField] private Transform playerOrientation;
    //walking
    [SerializeField] private float walkSpeed = 10f;
    [SerializeField] private float groundDrag = 4f;
    //jumping
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float airMultiplier = .2f;
    [SerializeField] private float playerHeight = 2f;
    [SerializeField] private LayerMask groundLayerMask;

    private Camera camera;
    private Rigidbody rigidbody;

    private InputMaster controls;

    private Vector2 inputVector;
    private Vector3 moveDirection;

    private float rotationX = 0;
    private float rotationY = 0;

    private bool isGrounded = false;
    
    void Awake()
    {
        camera = Camera.main;
        rigidbody = this.GetComponent<Rigidbody>();
        rigidbody.freezeRotation = true;
        controls = new InputMaster();
    }

    private void OnEnable()
    {
        controls.Enable();
        controls.Player.Movement.performed += OnMovementPerformed;
        controls.Player.Movement.canceled += OnMovementCancelled;
        controls.Player.Jump.performed += OnJumpPerformed;
        controls.Player.Fire.performed += OnFirePerformed;
    }

    private void OnMovementPerformed(CallbackContext value)
    {
        inputVector = value.ReadValue<Vector2>().normalized;
    }

    private void OnMovementCancelled(CallbackContext value)
    {
        inputVector = Vector2.zero;
    }

    private void OnJumpPerformed(CallbackContext value)
    {
        if (isGrounded)
            rigidbody.AddForce(Vector3.up * jumpHeight, ForceMode.Impulse);
    }
    private void OnFirePerformed(CallbackContext value)
    {

    }

    private void Update()
    {
        isGrounded = Physics.Raycast(playerOrientation.position, Vector3.down, (playerHeight / 2f) + .2f, groundLayerMask);
    }

    private void FixedUpdate()
    {
        transform.forward = playerOrientation.forward;

        moveDirection = transform.right * inputVector.x * walkSpeed +
                        transform.forward * inputVector.y * walkSpeed;

        Vector3 flatVelocity = new Vector3(rigidbody.velocity.x, 0f, rigidbody.velocity.z);
        if (flatVelocity.magnitude > walkSpeed)
        {
            Vector3 limitedVelocity = flatVelocity.normalized * walkSpeed;
            rigidbody.velocity = new Vector3(limitedVelocity.x, rigidbody.velocity.y, limitedVelocity.z);
        }

        if (isGrounded)
        {
            rigidbody.drag = groundDrag;
            rigidbody.AddForce(moveDirection, ForceMode.Force);
        }
        else
        {
            rigidbody.drag = 0;
            rigidbody.AddForce(moveDirection * airMultiplier, ForceMode.Force);
        }
    }

    private void OnDisable()
    {
        controls.Disable();
        controls.Player.Movement.performed -= OnMovementPerformed;
        controls.Player.Movement.canceled -= OnMovementCancelled;
        controls.Player.Jump.performed -= OnJumpPerformed;
        controls.Player.Fire.performed -= OnFirePerformed;
    }
}