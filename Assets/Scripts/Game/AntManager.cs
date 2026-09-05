using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class AntManager : MonoBehaviour
{
    [SerializeField] private Transform CamTargetTransform;
    [SerializeField] private Transform CameraTransform;
    [SerializeField] private Transform CursorTransform;
    private Camera playerCamera;

    [SerializeField] private float MoveSpeed;
    [SerializeField] private float TurnSensitivity;

    private Vector2 moveInput;
    private Vector2 rotateInput;
    private Vector2 cursorScreenPos;

    private float camYaw;
    private float camPitch;

    private Vector3 cursorWorldPos;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        camPitch = CamTargetTransform.rotation.eulerAngles.x;
        camYaw = CamTargetTransform.rotation.eulerAngles.y;

        playerCamera = CameraTransform.GetComponent<Camera>();
    }

    public void MoveCameraInput(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void RotateCameraInput(InputAction.CallbackContext context)
    {
        rotateInput = context.ReadValue<Vector2>();
    }
    
    public void CursorInput(InputAction.CallbackContext context)
    {
        cursorScreenPos = context.ReadValue<Vector2>();
    }

    // Update is called once per frame
    private void Update()
    {
        MoveCamera();
        TurnCamera();
        GetCursorWorldPosition();

        CursorTransform.position = cursorWorldPos + new Vector3(0, .01f, 0);
    }

    private void MoveCamera()
    {
        var moveVec = new Vector3(moveInput.x, 0, moveInput.y);
        Quaternion baseCamAngle = CameraTransform.rotation;
        Quaternion adjustedCamAngle = Quaternion.Euler(0, baseCamAngle.eulerAngles.y, baseCamAngle.eulerAngles.z);

        Vector3 adjustedMoveVec = adjustedCamAngle * moveVec;

        CamTargetTransform.position += adjustedMoveVec * (MoveSpeed * Time.deltaTime);
    }

    private void TurnCamera()
    {
        camYaw += rotateInput.x * TurnSensitivity * Time.deltaTime;

        camPitch -= rotateInput.y * TurnSensitivity * Time.deltaTime;
        camPitch = Math.Clamp(camPitch, 10, 80);

        CamTargetTransform.rotation =
            Quaternion.Euler(camPitch, camYaw, 0);
    }

    private Vector3 GetCursorWorldPosition()
    {
        Ray ray = playerCamera.ScreenPointToRay(cursorScreenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 999))
        {
            cursorWorldPos = hit.point;
        }

        return cursorWorldPos;
    }
}
