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
    [SerializeField] private float DirectDist;

    private Vector2 moveInput;
    private Vector2 rotateInput;
    private Vector2 cursorScreenPos;
    private bool isDirecting;

    private float camYaw;
    private float camPitch;

    private Vector3 cursorWorldPos;

    private AntNest nest;
    
    private Renderer cursorRenderer;
    protected MaterialPropertyBlock cursorMatPropBlock;
    [SerializeField] private float idleTransparency;
    [SerializeField] private float directTransparency;
    [SerializeField] private float directFadeSpeed;
    private float directFade;
    
    private static readonly int BaseColorPropID = Shader.PropertyToID("_BaseColor");
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        camPitch = CamTargetTransform.rotation.eulerAngles.x;
        camYaw = CamTargetTransform.rotation.eulerAngles.y;

        playerCamera = CameraTransform.GetComponent<Camera>();

        nest = FindAnyObjectByType<AntNest>();
        
        cursorRenderer = CursorTransform.GetComponent<Renderer>();
        cursorMatPropBlock = new();
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

    public void DirectInput(InputAction.CallbackContext context)
    {
        isDirecting = context.ReadValueAsButton();
    }

    // Update is called once per frame
    private void Update()
    {
        MoveCamera();
        TurnCamera();
        GetCursorWorldPosition();

        CursorTransform.position = cursorWorldPos + new Vector3(0, .01f, 0);
        CursorTransform.localScale = Vector3.one * (DirectDist * .2f);

        if (isDirecting)
        {
            DirectAnts();
            directFade += directFadeSpeed * Time.deltaTime;
        }
        else
        {
            directFade -= directFadeSpeed * Time.deltaTime;
        }

        directFade = Math.Clamp(directFade, 0, 1);
        
        cursorRenderer.GetPropertyBlock(cursorMatPropBlock);
        cursorMatPropBlock.SetColor(BaseColorPropID, new Color(1,1,1, Mathf.Lerp(idleTransparency, directTransparency, directFade)));
        cursorRenderer.SetPropertyBlock(cursorMatPropBlock);
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

    private void DirectAnts()
    {
        foreach (Ant ant in nest.GetAnts())
        {
            if ((ant.transform.position - cursorWorldPos).magnitude < DirectDist)
            {
                ant.Direct(cursorWorldPos);
            }
        }
    }
}
