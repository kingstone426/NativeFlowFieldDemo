using FlowFieldAI;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(MoveSystem))]
[BurstCompile]
public partial class PickCoinSystem : SystemBase
{
    private CoinSpawnerSystem coinSystem;
    private NativeList<Entity> coinsPicked;
    private EntityQuery flowConfigQuery;

    protected override void OnCreate()
    {
        base.OnCreate();
        coinSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<CoinSpawnerSystem>();
        coinsPicked = new NativeList<Entity>(100, Allocator.Persistent);
        flowConfigQuery = new EntityQueryBuilder(Allocator.Temp).
            WithAny<FlowConfig>().
            Build(this);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        coinsPicked.Dispose();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        var entities = flowConfigQuery.ToEntityArray(Allocator.Temp);

        foreach (var entity in entities)
        {
            var flowConfig = EntityManager.GetComponentData<FlowConfig>(entity);

            coinsPicked.Clear();

            Dependency = new DetectCoinCollisionsJob
            {
                CoinLookup = coinSystem.CoinEntityByTileLookup,
                CoinsPicked = coinsPicked.AsParallelWriter()
            }.ScheduleParallel(Dependency);

            Dependency.Complete();

            if (coinsPicked.Length == 0)
            {
                return;
            }

            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            new RepositionCoinsJob
            {
                CoinLookup = coinSystem.CoinEntityByTileLookup,
                CoinsPicked = coinsPicked,
                TransformLookup = GetComponentLookup<LocalTransform>(),
                Size = new float2(flowConfig.Width, flowConfig.Height),
                CommandBuffer = commandBuffer,
                Seed = (uint)(UnityEngine.Random.value * (uint.MaxValue - 1) + 1),
                ObstacleMap = flowConfig.Terrain.AsReadOnly()
            }.Schedule(coinsPicked.Length, Dependency).Complete();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }

    [BurstCompile]
    private partial struct DetectCoinCollisionsJob : IJobEntity
    {
        [ReadOnly] public NativeParallelMultiHashMap<int2, Entity> CoinLookup;
        public NativeList<Entity>.ParallelWriter CoinsPicked;
        private const float CoinRadius = 1f;

        private void Execute(in LocalTransform transform, in AgentComponent agent)
        {
            var currentTile = (int2)math.round(transform.Position.xz);
            if (math.distancesq(currentTile, transform.Position.xz) > CoinRadius * CoinRadius)
            {
                return;
            }

            if (!CoinLookup.TryGetFirstValue(currentTile, out var coinEntity, out var iter))
            {
                return;
            }

            CoinsPicked.AddNoResize(coinEntity);

            while (CoinLookup.TryGetNextValue(out coinEntity, ref iter))
            {
                CoinsPicked.AddNoResize(coinEntity);
            }
        }
    }

    [BurstCompile]
    private struct RepositionCoinsJob : IJobFor
    {
        public NativeParallelMultiHashMap<int2, Entity> CoinLookup;
        public NativeList<Entity> CoinsPicked;
        public ComponentLookup<LocalTransform> TransformLookup;
        public float2 Size;
        public EntityCommandBuffer CommandBuffer;
        public uint Seed;
        public NativeArray<float>.ReadOnly ObstacleMap;

        public void Execute(int index)
        {
            var coinEntity = CoinsPicked[index];
            var coinTransform = TransformLookup[coinEntity];
            var tile = (int2)math.round(coinTransform.Position.xz);
            CoinLookup.Remove(tile);
            var rand = Random.CreateFromIndex((uint)(Seed + index));
            AgentSpawnerSystem.GetRandomFreeTile(ref ObstacleMap, ref Size, ref rand, out tile);
            CoinLookup.Add(tile, coinEntity);
            CommandBuffer.SetComponent(coinEntity, new LocalTransform
            {
                Position = tile.x0y(),
                Scale = 1,
                Rotation = quaternion.identity
            });
        }
    }
}

