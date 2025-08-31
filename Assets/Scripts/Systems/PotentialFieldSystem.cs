using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[BurstCompile]
public partial class PotentialFieldSystem : SystemBase
{
    public NativeParallelMultiHashMap<int2, Entity> PotentialField => potentialField;
    private NativeParallelMultiHashMap<int2, Entity> potentialField;

    protected override void OnCreate()
    {
        base.OnCreate();

        potentialField = new NativeParallelMultiHashMap<int2, Entity>(1000, Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        PotentialField.Dispose();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        PotentialField.Clear();
        Dependency = new PopulatePotentialFieldJob{ PotentialField = PotentialField.AsParallelWriter(), }.ScheduleParallel(Dependency);
    }


    [BurstCompile]
    public partial struct PopulatePotentialFieldJob : IJobEntity
    {
        public NativeParallelMultiHashMap<int2, Entity>.ParallelWriter PotentialField;

        private void Execute(Entity entity, in LocalTransform transform, in WalkerComponent _)
        {
            var pos = (int2)math.round(transform.Position.xz);
            PotentialField.Add(pos, entity);
        }
    }
}

