using System;
using FlowFieldAI;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

[UpdateInGroup(typeof(InitializationSystemGroup))]
[BurstCompile]
public partial class WalkerSpawnerSystem : SystemBase
{
    private Entity prefab;
    private EntityQuery prefabQuery;
    private EntityQuery walkerQuery;
    private EntityQuery flowConfigQuery;
    private Random rand;
    private double nextSpawnTime;

    protected override void OnCreate()
    {
        base.OnCreate();

        prefabQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(WalkerComponent), },
            Options = EntityQueryOptions.IncludePrefab
        });
        RequireForUpdate(prefabQuery);

        walkerQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(WalkerComponent), },
            None = new ComponentType[] { typeof(Prefab) },
        });

        flowConfigQuery = new EntityQueryBuilder(Allocator.Temp).
            WithAny<FlowConfig>().
            Build(this);
        RequireForUpdate(flowConfigQuery);

        rand = new Random(6789);   // TODO: Seed
    }

    protected override void OnStartRunning()
    {
        prefab = prefabQuery.GetSingletonEntity();
    }

    protected override void OnUpdate()
    {
        if (SystemAPI.Time.ElapsedTime < nextSpawnTime)
        {
            return;
        }

        var entities = flowConfigQuery.ToEntityArray(Allocator.Temp);

        foreach (var flowFieldEntity in entities)
        {
            var flowConfig = EntityManager.GetComponentData<FlowConfig>(flowFieldEntity);

            var spawnCount = flowConfig.WalkerSpawnCount - walkerQuery.CalculateEntityCount();
            if (spawnCount <= 0)
            {
                continue;
            }

            SpawnWalkers(flowConfig, flowFieldEntity, 1);

            nextSpawnTime = SystemAPI.Time.ElapsedTime + rand.NextFloat(0, 2f/spawnCount);
        }
    }

    private void SpawnWalkers(FlowConfig flowConfig, Entity flowFieldEntity, int spawnCount)
    {
        var size = new float2(flowConfig.Width, flowConfig.Height);

        var obstacleMap = flowConfig.Terrain.AsReadOnly();

        var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

        for (var i = 0; i < spawnCount; i++)
        {
            var entity = commandBuffer.Instantiate(prefab);
            GetRandomFreeBorderTile(ref obstacleMap, ref size, ref rand, out var tile);
            commandBuffer.SetComponent(entity, new LocalTransform
            {
                Position = tile.x0y(),
                Scale = 1,
                Rotation = quaternion.identity
            });
            commandBuffer.SetComponent(entity, new WalkerComponent());

            commandBuffer.AddSharedComponent(entity, new FlowFieldRef {FlowFieldEntity = flowFieldEntity});
            commandBuffer.SetName(entity, $"Walker #{i}");
        }

        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();
    }


    [BurstCompile]
    private static void GetRandomFreeBorderTile(ref NativeArray<float>.ReadOnly obstacleMap, ref float2 size, ref Random rand, out int2 tile)
    {
        var panic = 0;
        while (panic++ < 1000)
        {

            var position = rand.NextFloat() * size;
            if (rand.NextBool())
            {
                position.x = rand.NextBool() ? 0 : size.x - 1;
            }
            else
            {
                position.y = rand.NextBool() ? 0 : size.y - 1;
            }
            tile = (int2)math.floor(position);
            var cost = obstacleMap[tile.x + tile.y * (int)size.x];
            if (cost < NativeFlowField.ObstacleCell)
            {
                return;
            }
        }

        throw new Exception("Could not find free border tile");
    }
}
