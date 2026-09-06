using System;
using System.Linq;
using UnityEngine;

public class AntUIHandler : MonoBehaviour
{
    [SerializeField] private API_UI uiAPI;

    private AntNest nest;
    private Camera playerCam;

    private void Start()
    {
        nest = FindFirstObjectByType<AntNest>();
        playerCam = FindFirstObjectByType<Camera>();
        
        uiAPI.PauseRequested += Pause;
        uiAPI.ResumeRequested += Unpause;
        
    }

    private void Pause()
    {
        Time.timeScale = 0;
    }

    private void Unpause()
    {
        Time.timeScale = 1;
    }

    public static bool IsPointInFrustum(Camera camera, Vector3 point)
    {
        // Convert world point to normalized viewport coordinates
        Vector3 viewportPoint = camera.WorldToViewportPoint(point);

        // Check if X and Y are within the screen boundaries, 
        // and Z is between the near and far clipping planes.
        return viewportPoint.x >= 0f && viewportPoint.x <= 1f &&
               viewportPoint.y >= 0f && viewportPoint.y <= 1f &&
               viewportPoint.z >= camera.nearClipPlane && 
               viewportPoint.z <= camera.farClipPlane;
    }
    
    private void Update()
    {
        int antsInCam = nest.GetAnts().Count(ant => IsPointInFrustum(playerCam, ant.transform.position));

        uiAPI.UpdateAntCount(antsInCam, nest.GetAntCount());
        uiAPI.UpdateFoodCount(Mathf.RoundToInt(nest.foodCount * 10));
    }
}
