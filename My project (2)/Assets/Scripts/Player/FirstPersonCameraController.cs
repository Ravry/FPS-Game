using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonCameraController : MonoBehaviour
{
    [Header("Look Parameters")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform playerOrientation;
    [SerializeField] private FirstPersonController playerController;
    [SerializeField] private Vector3 cameraOffset = Vector3.up;
    [SerializeField, Range(1, 10)] private float mouseSensX = 2.0f;
    [SerializeField, Range(1, 10)] private float mouseSensY = 2.0f;
    [SerializeField, Range(1, 90)] private float upperLookLimit = 60f;
    [SerializeField, Range(-90, 1)] private float lowerLookLimit = -60f;
    [SerializeField] private bool invertMouseY = true;

    [Header("Headbobbing Parameters")]
    [SerializeField] private bool headBobbingEnabled = true;
    [SerializeField] private float headBobbingAmplitude = 1f;
    [SerializeField] private float headBobbingFrequency = 1f;
    [SerializeField] private float headBobbingRunningMultiplier = 1.5f;
    [SerializeField] private float linearInterpolationMultiplier = 10f;

    private Vector2 mouseInput;
    private float rotationX, rotationY;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        rotationX += (invertMouseY) ? (-mouseInput.y * mouseSensX) : (mouseInput.y * mouseSensX);
        rotationY += mouseInput.x * mouseSensY;
        rotationX = Mathf.Clamp(rotationX, lowerLookLimit, upperLookLimit);
        transform.rotation = Quaternion.Euler(rotationX, rotationY, 0);
        playerOrientation.rotation = Quaternion.Euler(0, rotationY, 0);
    }

    private void LateUpdate()
    {
        transform.position = playerTransform.position + cameraOffset;
        if (headBobbingEnabled)
        {
            if(playerController.isGrounded && new Vector3(playerController.rigidbody.velocity.x, 0, playerController.rigidbody.velocity.z).magnitude > 1f)
            {
                float multiplier = (playerController.speed == playerController.walkSpeed) ? 1f : headBobbingRunningMultiplier;
                Vector3 desiredPosition =transform.position + transform.right * ((headBobbingAmplitude * multiplier) * Mathf.Cos(Time.time * (headBobbingFrequency * multiplier) / 2)) +
                                      Vector3.up * ((headBobbingAmplitude * multiplier) * Mathf.Sin(Time.time * (headBobbingFrequency * multiplier)));
                transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * linearInterpolationMultiplier);
            }
        }
    }
}