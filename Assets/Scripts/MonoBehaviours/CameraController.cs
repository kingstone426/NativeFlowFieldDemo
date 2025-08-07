using Unity.Cinemachine;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public MeshRenderer HeatMap;
    public float BlendDuration;
    public CinemachineCamera ActiveCamera => virtualCameras[activeCameraIndex];

    private int activeCameraIndex;
    private List<CinemachineCamera> virtualCameras;

    private float heatMapAlphaVelocity;
    private float heatMapMaxCostVelocity;

    private static class ShaderProperties
    {
        public static readonly int MainTex = Shader.PropertyToID("_MainTex");
        public static readonly int Alpha = Shader.PropertyToID("Alpha");
        public static readonly int MaxCost = Shader.PropertyToID("MaxCost");
    }

    private void Awake()
    {
        virtualCameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None)
            .OrderBy(c => -c.Priority.Value)
            .ToList();

        for (var i = 0; i < virtualCameras.Count; i++)
        {
            virtualCameras[i].enabled = i == activeCameraIndex;
        }

        /*
        var distanceSystem = World
            .DefaultGameObjectInjectionWorld
            .GetOrCreateSystemManaged<DistanceSystem>();

        HeatMap.material.SetTexture(ShaderProperties.MainTex, distanceSystem.FlowField.HeatMap);

        HeatMap.transform.localScale = new Vector3(DistanceSystem.Width, DistanceSystem.Height, 1);
        */

        //UpdateHeatMap(true);
    }

    private void Update()
    {
        //UpdateHeatMap();

        UpdateCamera();
    }

    private void UpdateCamera()
    {
        var newCameraIndex = activeCameraIndex;

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            newCameraIndex++;
        } else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            newCameraIndex--;
        }

        newCameraIndex = Mathf.Clamp(newCameraIndex, 0, virtualCameras.Count-1);

        if (newCameraIndex == activeCameraIndex)
        {
            return;
        }

        activeCameraIndex = newCameraIndex;

        for (var i = 0; i < virtualCameras.Count; i++)
        {
            virtualCameras[i].enabled = i == activeCameraIndex;
        }
    }

    private void UpdateHeatMap(bool snap=false)
    {
        var target = virtualCameras[activeCameraIndex].GetComponent<BlendSettings>();

        if (snap)
        {
            HeatMap.material.SetFloat(ShaderProperties.Alpha, target.HeatMapAlpha);
            HeatMap.material.SetFloat(ShaderProperties.MaxCost, target.HeatMapMaxCost);
        }
        else
        {
            var heatMapAlpha = Mathf.SmoothDamp(
                HeatMap.material.GetFloat(ShaderProperties.Alpha),
                target.HeatMapAlpha,
                ref heatMapAlphaVelocity,
                BlendDuration);
            HeatMap.material.SetFloat(ShaderProperties.Alpha, heatMapAlpha);

            var heatMapMaxCost = Mathf.SmoothDamp(
                HeatMap.material.GetFloat(ShaderProperties.MaxCost),
                target.HeatMapMaxCost,
                ref heatMapMaxCostVelocity,
                BlendDuration);
            HeatMap.material.SetFloat(ShaderProperties.MaxCost, heatMapMaxCost);
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            HeatMap.gameObject.SetActive(!HeatMap.gameObject.activeSelf);
        }
    }
}

