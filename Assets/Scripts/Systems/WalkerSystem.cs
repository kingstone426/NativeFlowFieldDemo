using FlowFieldAI;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[BurstCompile]
public partial class WalkerSystem : SystemBase
{
    private const float Speed = 5f;
    private NativeList<Entity> walkersToRemove;
    private EntityQuery flowFieldQuery;
    private PotentialFieldSystem potentialFieldSystem;


    private static readonly int2 Right = new(1, 0);
    private static readonly int2 Up = new (0, 1);

    private static int Flatten(int2 pos, int width) => pos.y * width + pos.x;
    private static int2 UnFlatten(int index, int width) => new(index % width, index / width);

    protected override void OnCreate()
    {
        base.OnCreate();

        walkersToRemove = new NativeList<Entity>(100, Allocator.Persistent);

        flowFieldQuery = new EntityQueryBuilder(Allocator.Temp).
            WithAll<NativeFlowField, FlowConfig>().
            Build(this);

        potentialFieldSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PotentialFieldSystem>();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        walkersToRemove.Dispose();
    }

    [BurstCompile]
    protected unsafe override void OnUpdate()
    {
        var entities = flowFieldQuery.ToEntityArray(Allocator.Temp);

        foreach (var flowFieldEntity in entities)
        {
            var nativeFlowField = EntityManager.GetComponentData<NativeFlowField>(flowFieldEntity);
            var flowConfig = EntityManager.GetComponentData<FlowConfig>(flowFieldEntity);

            if (nativeFlowField.NextIndices.IsCreated == false)
            {
                // Cost map not created - this is normal during application startup
                continue;
            }

            // Query agents using that flow field
            var query = GetEntityQuery(
                ComponentType.ReadOnly<FlowFieldRef>(),
                ComponentType.ReadWrite<LocalTransform>(),
                ComponentType.ReadWrite<WalkerComponent>()
            );
            query.SetSharedComponentFilter(new FlowFieldRef { FlowFieldEntity = flowFieldEntity });

            // Using IJobEntity with an unsafe pointer instead of Entities.ForEach,
            // because of a bug with AsyncGPUReadback.RequestIntoNativeArray
            // https://discussions.unity.com/t/asyncgpureadback-requestintonativearray-causes-invalidoperationexception-on-nativearray/818225/76
            new WalkJob
            {
                FlowField = (int*)nativeFlowField.NextIndices.GetUnsafeReadOnlyPtr(),
                PotentialField = potentialFieldSystem.PotentialField,
                WalkersToRemove = walkersToRemove.AsParallelWriter(),
                Width = nativeFlowField.Width,
                Height = nativeFlowField.Height,
                DeltaTime = SystemAPI.Time.DeltaTime,
                WalkerVelocitySmoothingFactor = flowConfig.WalkerVelocitySmoothingFactor,
            }.ScheduleParallel(query, Dependency).Complete();

            if (walkersToRemove.Length == 0)
            {
                return;
            }

            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            foreach (var walker in walkersToRemove)
            {
                commandBuffer.DestroyEntity(walker);
            }
            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
            walkersToRemove.Clear();
        }
    }

    [BurstCompile]
    private unsafe partial struct WalkJob : IJobEntity
    {
        [ReadOnly][NativeDisableUnsafePtrRestriction] public int* FlowField;
        [ReadOnly] public NativeParallelMultiHashMap<int2, Entity> PotentialField;
        public NativeList<Entity>.ParallelWriter WalkersToRemove;
        public int Width;
        public int Height;
        public float DeltaTime;
        public float WalkerVelocitySmoothingFactor;

        private void Execute(Entity entity, ref LocalTransform transform, ref WalkerComponent walker)
        {
            var pos = transform.Position.xz;

            var targetTile = CalculateTargetTile(pos, FlowField, Width);
            if (!IsValidTarget(targetTile, Width, Height))
            {
                return;
            }

            if (math.distancesq(pos, targetTile) < 0.0001f)
            {
                WalkersToRemove.AddNoResize(entity);
                return;
            }

            var speed = Speed;
            if (math.lengthsq(walker.Velocity)>0.001f )
            {
                var crowdPos = pos + math.normalize(walker.Velocity);
                if (crowdPos.x >= Width || crowdPos.x < 0 ||
                    crowdPos.y >= Height || crowdPos.y < 0)
                {
                    return;
                }

                var i = (int2)math.floor(crowdPos);
                var f = crowdPos - i;

                var block = new float2x2(
                    PotentialField.CountValuesForKey(i), PotentialField.CountValuesForKey(i + Right),
                    PotentialField.CountValuesForKey(i + Up), PotentialField.CountValuesForKey(i + Right + Up)
                );
                var crowd = block.c0.x * (1 - f.x) * (1 - f.y) +
                            block.c1.x * f.x * (1 - f.y) +
                            block.c0.y * (1 - f.x) * f.y +
                            block.c1.y * f.x * f.y;

                speed = crowd > 1 ? Speed / crowd : Speed;
            }

            var direction = math.normalize(targetTile - pos);
            var candidateVelocity = direction * speed;

            walker.Velocity = math.lerp(walker.Velocity, candidateVelocity, WalkerVelocitySmoothingFactor);

            pos += walker.Velocity * DeltaTime;
            transform.Position = pos.x0y();
        }
    }

    private unsafe static int2 CalculateTargetTile(float2 pos, int* flowField, int width)
    {
        var currentTile = (int2)math.round(pos);
        var target = flowField[Flatten(currentTile, width)];
        if (target < 0)
        {
            return currentTile;
        }

        return UnFlatten(target, width);
    }

    private static bool IsValidTarget(int2 targetTile, int width, int height) =>
        targetTile.x >= 0 &&
        targetTile.x < width &&
        targetTile.y >= 0 &&
        targetTile.y < height;
}
