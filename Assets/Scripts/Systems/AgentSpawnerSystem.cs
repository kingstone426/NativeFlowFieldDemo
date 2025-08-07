using System;
using FlowFieldAI;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = Unity.Mathematics.Random;

[UpdateInGroup(typeof(InitializationSystemGroup))]
[BurstCompile]
public partial class AgentSpawnerSystem : SystemBase
{
    public static readonly int2 InvalidTarget = new (-1, -1);

    private Entity prefab;
    private Entity visualPrefabHolder;
    private EntityQuery prefabQuery;
    private EntityQuery visualPrefabQuery;
    private EntityQuery flowConfigQuery;

    protected override void OnCreate()
    {
        base.OnCreate();

        prefabQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(AgentComponent), },
            Options = EntityQueryOptions.IncludePrefab
        });
        RequireForUpdate(prefabQuery);

        visualPrefabQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(VisualPrefabAssets), },
            Options = EntityQueryOptions.IncludePrefab
        });
        RequireForUpdate(visualPrefabQuery);

        flowConfigQuery = new EntityQueryBuilder(Allocator.Temp).
            WithAny<FlowConfig>().
            Build(this);
        RequireForUpdate(flowConfigQuery);
    }

    protected override void OnStartRunning()
    {
        prefab = prefabQuery.GetSingletonEntity();
        visualPrefabHolder = visualPrefabQuery.GetSingletonEntity();
    }

    protected override void OnUpdate()
    {
        var entities = flowConfigQuery.ToEntityArray(Allocator.Temp);

        foreach (var flowFieldEntity in entities)
        {
            var flowConfig = EntityManager.GetComponentData<FlowConfig>(flowFieldEntity);

            SpawnAgents(flowConfig, flowFieldEntity);
        }

        this.Enabled = false;
    }

    private void SpawnAgents(FlowConfig flowConfig, Entity flowFieldEntity)
    {
        var size = new float2(flowConfig.Width, flowConfig.Height);

        var rand = new Random(6789);   // TODO: Seed

        var obstacleMap = flowConfig.Terrain.AsReadOnly();

        var visualPrefab = EntityManager.GetComponentData<VisualPrefabAssets>(visualPrefabHolder).AgentPrefab;

        var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

        for (var i = 0; i < flowConfig.AgentSpawnCount; i++)
        {
            var entity = commandBuffer.Instantiate(prefab);
            GetRandomFreeTile(ref obstacleMap, ref size, ref rand, out var tile);
            commandBuffer.SetComponent(entity, new LocalTransform
            {
                Position = tile.x0y(),
                Scale = 1,
                Rotation = quaternion.identity
            });
            commandBuffer.SetComponent(entity, new AgentComponent
            {
                TargetTile = InvalidTarget
            });

            var gameObject = Object.Instantiate(visualPrefab);
            gameObject.name = visualPrefab.name;
            commandBuffer.AddComponent(entity, new VisualProxy{Proxy = gameObject,});
            commandBuffer.AddComponent(entity, new VisualProxySync());

            commandBuffer.AddSharedComponent(entity, new FlowFieldRef {FlowFieldEntity = flowFieldEntity});
            commandBuffer.SetName(entity, $"Agent #{i}");
        }

        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();
    }

    [BurstCompile]
    public static void GetRandomFreeTile(ref NativeArray<float>.ReadOnly distanceMap, ref float2 size, ref Random rand, out int2 tile)
    {
        var panic = 0;
        while (panic++ < 1000)
        {
            var position = rand.NextFloat2() * size;
            tile = (int2)math.floor(position);
            var cost = distanceMap[tile.x + tile.y * (int)size.x];
            if (cost < NativeFlowField.ObstacleCell)
            {
                return;
            }
        }

        throw new Exception("Could not find free tile");
    }
}
