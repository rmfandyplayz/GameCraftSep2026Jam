using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class AntManager : MonoBehaviour
{
    [SerializeField] private Transform CamTargetTransform;
    [SerializeField] private Transform CameraTransform;
    [SerializeField] private Transform CursorTransform;
    [SerializeField] private Transform CursorCircleTransform;
    private Camera playerCamera;

    [SerializeField] private float MoveSpeed;
    [SerializeField] private float TurnSensitivity;
    [SerializeField] private float DirectDist;
    [SerializeField] private float TimeTillMaxDirectDist;

    private float directRadAmount;

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

    private Renderer innerCursorRenderer;
    protected MaterialPropertyBlock inCursorMatPropBlock;
    
    [SerializeField] private float idleTransparency;
    [SerializeField] private float directTransparency;
    [SerializeField] private Color OuterCircleColor;
    [SerializeField] private Color InnerCircleColor;
    
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
        innerCursorRenderer = CursorCircleTransform.GetComponent<Renderer>();
        inCursorMatPropBlock = new();
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

        CursorCircleTransform.localScale = Vector3.one * directRadAmount;

        if (isDirecting)
        {
            directRadAmount = Mathf.Clamp(directRadAmount + Time.deltaTime / TimeTillMaxDirectDist, 0, 1);
            DirectAnts();
        }
        else
        {
            directRadAmount = 0;
        }

        
        cursorRenderer.GetPropertyBlock(cursorMatPropBlock);
        cursorMatPropBlock.SetColor(BaseColorPropID, OuterCircleColor * new Color(1,1,1, Mathf.Lerp(idleTransparency, directTransparency, directRadAmount)));
        cursorRenderer.SetPropertyBlock(cursorMatPropBlock);

        innerCursorRenderer.GetPropertyBlock(inCursorMatPropBlock);
        inCursorMatPropBlock.SetColor(BaseColorPropID, InnerCircleColor * new Color(1,1,1, Mathf.Lerp(idleTransparency, directTransparency, directRadAmount)));
        innerCursorRenderer.SetPropertyBlock(inCursorMatPropBlock);
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
            if ((ant.transform.position - cursorWorldPos).magnitude < Mathf.Lerp(0, DirectDist, directRadAmount))
            {
                ant.Direct(cursorWorldPos);
            }
        }
    }
}
