using System.Threading;
using FlowFieldAI;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial class BakeSystem : SystemBase
{
    private EntityQuery flowFieldQuery;

    protected override void OnCreate()
    {
        base.OnCreate();

        flowFieldQuery = new EntityQueryBuilder(Allocator.Temp).
            WithAll<NativeFlowField, FlowConfig>().
            Build(this);
    }

    protected override void OnUpdate()
    {
        var entities = flowFieldQuery.ToEntityArray(Allocator.Temp);

        foreach (var entity in entities)
        {
            var flowField = EntityManager.GetComponentData<NativeFlowField>(entity);
            var flowConfig = EntityManager.GetComponentData<FlowConfig>(entity);

            // Recreate input field using tile map and target (coin) entities
            flowConfig.InputField.CopyFrom(flowConfig.Terrain);
            new PlotCoinPositions
            {
                Width = flowConfig.Width,
                Height = flowConfig.Height,
                Output = flowConfig.InputField
            }.ScheduleParallel(Dependency).Complete();

            // Bake flow field
            flowField.Bake(flowConfig.InputField, flowConfig.BakeOptions);
        }
    }

    [BurstCompile]
    public unsafe partial struct PlotCoinPositions : IJobEntity
    {
        public int Width;
        public int Height;
        [NativeDisableParallelForRestriction] public NativeArray<float> Output;

        private void Execute(in LocalTransform transform, in CoinTag _)
        {
            var pos = (int2)math.round(transform.Position.xz);
            pos.x = math.clamp(pos.x, 0, Width - 1);
            pos.y = math.clamp(pos.y, 0, Height - 1);
            var index = pos.x + pos.y * Width;
            var ptr = (float*)Output.GetUnsafePtr() + index;
            Interlocked.Exchange(ref *ptr, 0.0f);
        }
    }
}

