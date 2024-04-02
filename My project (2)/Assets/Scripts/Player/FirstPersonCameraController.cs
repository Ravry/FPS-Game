using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonCameraController : MonoBehaviour
{
    [Header("Look Parameters")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform playerOrientation;
    [SerializeField] private Vector3 cameraOffset = Vector3.up;
    [SerializeField, Range(1, 10)] private float mouseSensX = 2.0f;
    [SerializeField, Range(1, 10)] private float mouseSensY = 2.0f;
    [SerializeField, Range(1, 90)] private float upperLookLimit = 60f;
    [SerializeField, Range(-90, 1)] private float lowerLookLimit = -60f;
    [SerializeField] private bool invertMouseY = true;

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
    }
}