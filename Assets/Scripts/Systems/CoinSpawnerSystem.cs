using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

[UpdateInGroup(typeof(InitializationSystemGroup))]
[BurstCompile]
public partial class CoinSpawnerSystem : SystemBase
{
    private const int SpawnCount = 0;

    private Entity prefab;
    private EntityQuery prefabQuery;
    private EntityQuery flowConfigQuery;

    public NativeParallelMultiHashMap<int2, Entity> CoinEntityByTileLookup => coinEntityByTileLookup;
    private NativeParallelMultiHashMap<int2, Entity> coinEntityByTileLookup;

    protected override void OnCreate()
    {
        base.OnCreate();

        prefabQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(CoinTag), typeof(Prefab) },
            Options = EntityQueryOptions.IncludePrefab
        });

        RequireForUpdate(prefabQuery);

        flowConfigQuery = new EntityQueryBuilder(Allocator.Temp).
            WithAny<FlowConfig>().
            Build(this);
        RequireForUpdate(flowConfigQuery);

        coinEntityByTileLookup = new NativeParallelMultiHashMap<int2, Entity>(SpawnCount+1, Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        coinEntityByTileLookup.Dispose();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        var entities = flowConfigQuery.ToEntityArray(Allocator.Temp);

        foreach (var entity in entities)
        {
            var flowConfig = EntityManager.GetComponentData<FlowConfig>(entity);

            SpawnInParallel(flowConfig);
        }

        PopulateLookupInParallel();

        this.Enabled = false;
    }

    [BurstCompile]
    private void SpawnInParallel(FlowConfig flowConfig)
    {
        prefab = prefabQuery.GetSingletonEntity();
        var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
        var size = new float2(flowConfig.Width, flowConfig.Height);
        var instantiateJob = new InstantiateJob
        {
            CommandBuffer = commandBuffer.AsParallelWriter(),
            Prefab = prefab,
            Size = size,
            ObstacleMap = flowConfig.Terrain.AsReadOnly()
        };
        instantiateJob.ScheduleParallelByRef(SpawnCount, 64, default).Complete();
        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();
    }

    [BurstCompile]
    private void PopulateLookupInParallel()
    {
        var populateJob = new PopulateMapJob{ Map = coinEntityByTileLookup.AsParallelWriter(), };
        populateJob.ScheduleParallel(Dependency).Complete();
    }

    [BurstCompile]
    public struct InstantiateJob : IJobFor
    {
        public EntityCommandBuffer.ParallelWriter CommandBuffer;
        public Entity Prefab;
        public float2 Size;
        [ReadOnly] public NativeArray<float>.ReadOnly ObstacleMap;

        public void Execute(int i)
        {
            var entity = CommandBuffer.Instantiate(i, Prefab);
            var rand = Random.CreateFromIndex((uint)i);
            AgentSpawnerSystem.GetRandomFreeTile(ref ObstacleMap, ref Size, ref rand, out var tile);
            CommandBuffer.SetComponent(i, entity, new LocalTransform
            {
                Position = tile.x0y(),
                Scale = 1,
                Rotation = quaternion.identity
            });
            CommandBuffer.SetName(i, entity, $"Coin #{i}");
        }
    }

    [BurstCompile]
    public partial struct PopulateMapJob : IJobEntity
    {
        public NativeParallelMultiHashMap<int2, Entity>.ParallelWriter Map;

        private void Execute(Entity entity, in LocalTransform transform, in CoinTag _)
        {
            var pos = (int2)math.round(transform.Position.xz);
            Map.Add(pos, entity);
        }
    }
}
