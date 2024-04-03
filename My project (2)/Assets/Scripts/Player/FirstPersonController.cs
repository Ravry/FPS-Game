using System;
using System.Xml;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputAction;

[RequireComponent(typeof(Rigidbody))]
public class FirstPersonController : MonoBehaviour
{
    public bool CanMove { get; private set; } = true;

    [Header("Movement Parameters")]
    [SerializeField] private Transform playerOrientation;
    //walking
    [SerializeField] public float walkSpeed = 10f;
    [SerializeField] public float sprintSpeed = 20f;
    [SerializeField] private float groundDrag = 4f;
    //jumping
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float airMultiplier = .2f;
    [SerializeField] private float playerHeight = 2f;
    [SerializeField] private LayerMask groundLayerMask;

    [Header("Grappling Parameters")]
    [SerializeField] private GameObject grapplingHookPrefab;
    [SerializeField] private Transform grapplingStartPoint;
    [SerializeField] private float maxGrappleDistance;
    [SerializeField] private float grappleDelayTime;
    [SerializeField] private float grapplingCd;
    [SerializeField] private float overshootYAxis;
    [SerializeField] private LayerMask grappableLayerMask;
    [SerializeField] private LineRenderer grapplingLineRenderer;
    [SerializeField] private int ropeQuality = 5;

    [Header("UI Elements")]
    [SerializeField] private Image crosshairImage;

    private Camera camera;
    [HideInInspector] public Rigidbody rigidbody;

    private InputMaster controls;

    private Vector2 inputVector;
    private Vector3 moveDirection;

    private float rotationX = 0;
    private float rotationY = 0;

    [HideInInspector] public bool isGrounded = false;
    private bool isFreeze = false;

    [HideInInspector] public float speed;

    private Vector3 grapplePoint;
    private float grapplingCdTimer;
    private float startTime = 0;
    private bool isGrappling;
    private bool activeGrappling;
    private Vector3 ropeDir;
    private Transform grapplingHookTransform;

    void Awake()
    {
        camera = Camera.main;
        rigidbody = this.GetComponent<Rigidbody>();
        rigidbody.freezeRotation = true;
        controls = new InputMaster();
        speed = walkSpeed;
    }

    private void OnEnable()
    {
        controls.Enable();
        controls.Player.Movement.performed += OnMovementPerformed;
        controls.Player.Movement.canceled += OnMovementCancelled;

        controls.Player.Sprint.performed += OnSprintPerformed;
        controls.Player.Sprint.canceled += OnSprintCancelled;
        
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

    private void OnSprintPerformed(CallbackContext value)
    {
        speed = sprintSpeed;
    }

    private void OnSprintCancelled(CallbackContext value)
    {
        speed = walkSpeed;
    }

    private void OnJumpPerformed(CallbackContext value)
    {
        if (isGrounded)
            rigidbody.AddForce(Vector3.up * jumpHeight, ForceMode.Impulse);
    }

    public Vector3 CalculateJumpVelocity(Vector3 startPoint, Vector3 endPoint, float trajectoryHeight)
    {
        float gravity = Physics.gravity.y;
        float displacementY = endPoint.y - startPoint.y;
        Vector3 displacementXZ = new Vector3(endPoint.x - startPoint.x, 0f, endPoint.z - startPoint.z);

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * trajectoryHeight);
        Vector3 velocityXZ = displacementXZ / (Mathf.Sqrt(-2 * trajectoryHeight / gravity)
            + Mathf.Sqrt(2 * (displacementY - trajectoryHeight) / gravity));

        return velocityXZ + velocityY;
    }

    private void JumpToPosition(Vector3 targetPosition, float trajectoryHeight)
    {
        activeGrappling = true;
        velocityToSet = CalculateJumpVelocity(transform.position, targetPosition, trajectoryHeight);
        Invoke(nameof(SetVelocity), .1f);
    }

    private Vector3 velocityToSet;
    private void SetVelocity()
    {
        rigidbody.velocity = velocityToSet;
    }

    private void StartGrapple()
    {
        if (grapplingCdTimer > 0 || isGrappling)
            return;

        isGrappling = true;
        grapplingLineRenderer.positionCount = (ropeQuality + 1);
        ropeDir = camera.transform.up;
        startTime = Time.time;
        StartCoroutine(AudioManager.instance.PlaySoundForSeconds("hook_rope", grappleDelayTime));
        if (Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit rayHit, maxGrappleDistance))
        {
            if ((grappableLayerMask & (1 << rayHit.transform.gameObject.layer)) != 0)
            {
                StartCoroutine(AudioManager.instance.PlaySoundAfterSeconds("hook_hit", grappleDelayTime));
                isFreeze = true;
                grapplePoint = rayHit.point;
                Invoke(nameof(ExecuteGrapple), grappleDelayTime);
            }
            else
            {
                grapplePoint = camera.transform.position + camera.transform.forward * maxGrappleDistance;
                Invoke(nameof(StopGrapple), grappleDelayTime);
            }
        }
        else
        {
            grapplePoint = camera.transform.position + camera.transform.forward * maxGrappleDistance;
            Invoke(nameof(StopGrapple), grappleDelayTime);
        }

        grapplingLineRenderer.enabled = true;

        for (int i = 1; i <= ropeQuality; i++)
        {
            float t = i / (float)ropeQuality;
            grapplingLineRenderer.SetPosition(i, Vector3.Lerp(grapplingStartPoint.position, grapplePoint, t));
        }
    }
    
    private void ExecuteGrapple()
    {
        grapplingHookTransform = Instantiate(grapplingHookPrefab, grapplePoint, Quaternion.identity).transform;
        isFreeze = false;
        Vector3 lowestPoint = transform.position;
        float grapplePointRelativeYPos = grapplePoint.y - lowestPoint.y;
        float highestPointOnArc = grapplePointRelativeYPos + overshootYAxis;
        if (grapplePointRelativeYPos < 0)
            highestPointOnArc = overshootYAxis;
        rigidbody.drag = 0;
        JumpToPosition(grapplePoint, highestPointOnArc);
        Invoke(nameof(StopGrapple), 1f);
    }
    
    private void StopGrapple()
    {
        if (grapplingHookTransform != null)
            Destroy(grapplingHookTransform.gameObject);
        isGrappling = false;
        isFreeze = false;
        activeGrappling = false;
        grapplingCdTimer = grapplingCd;
        grapplingLineRenderer.enabled = false;
    }

    private void OnFirePerformed(CallbackContext value)
    {
        StartGrapple();
    }

    private void Update()
    {

        if (Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit rayHit, maxGrappleDistance))
        {
            if ((grappableLayerMask & (1 << rayHit.transform.gameObject.layer)) != 0)
            {
                crosshairImage.color = Color.blue;
            }
            else
                crosshairImage.color = Color.black;
        }
        else
            crosshairImage.color = Color.black;

        isGrounded = Physics.Raycast(playerOrientation.position, Vector3.down, (playerHeight / 2f) + .2f, groundLayerMask);

        if (isFreeze)
            rigidbody.velocity = new Vector3(0, rigidbody.velocity.y, 0);

        if (grapplingCdTimer > 0)
            grapplingCdTimer -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if (activeGrappling) return;

        transform.forward = playerOrientation.forward;

        moveDirection = transform.right * inputVector.x * speed +
                        transform.forward * inputVector.y * speed;

        Vector3 flatVelocity = new Vector3(rigidbody.velocity.x, 0f, rigidbody.velocity.z);
        if (flatVelocity.magnitude > speed)
        {
            Vector3 limitedVelocity = flatVelocity.normalized * speed;
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

    private void LateUpdate()
    {
        if (isGrappling)
        {
            if (grapplingHookTransform != null)
            {
                grapplingHookTransform.position = grapplePoint;
                grapplingHookTransform.forward = camera.transform.forward;
            }
            grapplingLineRenderer.SetPosition(0, grapplingStartPoint.position);

            float currentTime = Time.time;
            float time = currentTime - startTime;
            float processed = time / grappleDelayTime;

            if (processed < 1f)
            {
                for (int i = 1; i <= ropeQuality; i++)
                {
                    float t = i / (float)ropeQuality;
                    Vector3 ropeOffset = ropeDir * ((1f - t) * Mathf.Sin((t * 4 * Mathf.PI)));


                    grapplingLineRenderer.SetPosition(i,
                                                      Vector3.Lerp(grapplingStartPoint.position, grapplePoint, t * processed) +
                                                      ropeOffset);
                }
            }
            else
            {
                grapplingLineRenderer.positionCount = 2;
                grapplingLineRenderer.SetPosition(0, grapplingStartPoint.position);
                grapplingLineRenderer.SetPosition(1, grapplePoint);
            }
        }
    }

    private void OnDisable()
    {
        controls.Player.Movement.performed -= OnMovementPerformed;
        controls.Player.Movement.canceled -= OnMovementCancelled;

        controls.Player.Sprint.performed -= OnSprintPerformed;
        controls.Player.Sprint.canceled -= OnSprintCancelled;

        controls.Player.Jump.performed -= OnJumpPerformed;

        controls.Player.Fire.performed -= OnFirePerformed;
        controls.Disable();
    }
}