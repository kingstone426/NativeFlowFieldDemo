using FlowFieldAI;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class FlowFieldConfig : MonoBehaviour
{
    public FlowTerrain FlowTerrain;
    public MeshRenderer HeatMapRenderer;
    public bool UseHeatMap => HeatMapRenderer != null;

    public BakeOptions BakeOptions = BakeOptions.Default;
    public int AgentSpawnCount;
    public int WalkerSpawnCount;
    public float WalkerVelocitySmoothingFactor = 1f;
    public NativeFlowField FlowField { get; private set; }

    private Entity entity;
    private EntityManager entityManager;

    private static class ShaderProperties
    {
        public static readonly int MainTex = Shader.PropertyToID("_MainTex");
    }

    private void Awake()
    {
        FlowField = new NativeFlowField(FlowTerrain.Width, FlowTerrain.Height, UseHeatMap);

        var flowConfig = GetConfig();

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        entity = entityManager.CreateEntity();
        entityManager.SetName(entity, name);
        entityManager.AddComponentData(entity, FlowField);
        entityManager.AddComponentData(entity, flowConfig);

        if (UseHeatMap)
        {
            HeatMapRenderer.material.SetTexture(ShaderProperties.MainTex, FlowField.HeatMap);
            HeatMapRenderer.transform.localScale = new Vector3(FlowTerrain.Width, FlowTerrain.Height, 1);
            HeatMapRenderer.transform.localPosition = new Vector3((FlowTerrain.Width-1)/2f, 0, (FlowTerrain.Height-1)/2f);
        }
    }

    private void OnDestroy()
    {
        FlowField.Dispose();
    }

    private void OnValidate() => UpdateConfig();

    public void UpdateConfig()
    {
        if (entityManager == default)
        {
            return;
        }

        entityManager.SetComponentData(entity, GetConfig());
    }

    private FlowConfig GetConfig() =>
        new(
            width: FlowTerrain.Width,
            height: FlowTerrain.Height,
            terrain: FlowTerrain.Terrain,
            bakeOptions: BakeOptions,
            agentSpawnCount: AgentSpawnCount,
            walkerSpawnCount: WalkerSpawnCount,
            walkerVelocitySmoothingFactor: WalkerVelocitySmoothingFactor
        );
}

