using FlowFieldAI;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial class MoveSystem : SystemBase
{
    private const float Speed = 2.5f;
    private static readonly float3 Up = new (0,1,0);
    private static readonly float TurnDuration = 0.25f;
    private EntityQuery flowFieldQuery;

    private static int Flatten(int2 pos, int width) => pos.y * width + pos.x;
    private static int2 UnFlatten(int index, int width) => new(index % width, index / width);

    protected override void OnCreate()
    {
        base.OnCreate();

        flowFieldQuery = new EntityQueryBuilder(Allocator.Temp).
            WithAll<NativeFlowField, FlowConfig>().
            Build(this);
    }

    [BurstCompile]
    protected unsafe override void OnUpdate()
    {
        var entities = flowFieldQuery.ToEntityArray(Allocator.Temp);

        foreach (var flowFieldEntity in entities)
        {
            var nativeFlowField = EntityManager.GetComponentData<NativeFlowField>(flowFieldEntity);

            if (nativeFlowField.NextIndices.IsCreated == false)
            {
                // Cost map not created - this is normal during application startup
                continue;
            }

            // Query agents using that flow field ID
            var query = GetEntityQuery(
                ComponentType.ReadOnly<FlowFieldRef>(),
                ComponentType.ReadWrite<LocalTransform>(),
                ComponentType.ReadWrite<AgentComponent>()
            );
            query.SetSharedComponentFilter(new FlowFieldRef { FlowFieldEntity = flowFieldEntity });

            // Using IJobEntity with an unsafe pointer instead of Entities.ForEach,
            // because of a bug with AsyncGPUReadback.RequestIntoNativeArray
            // https://discussions.unity.com/t/asyncgpureadback-requestintonativearray-causes-invalidoperationexception-on-nativearray/818225/76
            Dependency = new MoveJob
            {
                FlowField = (int*)nativeFlowField.NextIndices.GetUnsafeReadOnlyPtr(),
                Width = nativeFlowField.Width,
                Height = nativeFlowField.Height,
                DeltaTime = SystemAPI.Time.DeltaTime,
                Time = (float)SystemAPI.Time.ElapsedTime
            }.ScheduleParallel(query, Dependency);
        }
    }

    [BurstCompile]
    private unsafe partial struct MoveJob : IJobEntity
    {
        [ReadOnly][NativeDisableUnsafePtrRestriction] public int* FlowField;
        public int Width;
        public int Height;
        public float DeltaTime;
        public float Time;

        private void Execute(ref LocalTransform transform, ref AgentComponent agent)
        {
            var pos = transform.Position.xz;
            var targetTile = agent.TargetTile;
            var moveTimeRemaining = DeltaTime;

            if (!IsValidTarget(targetTile, Width, Height))
            {
                targetTile = CalculateTargetTile(pos, FlowField, Width);
            }

            while (moveTimeRemaining > 0)
            {
                var delta = targetTile - pos;
                var timeToReachTarget = math.length(delta) / Speed;

                if (delta.x==0 && delta.y==0)   // Destination reached
                {
                    targetTile = AgentSpawnerSystem.InvalidTarget;  // Invalidate target so that it is recomputed next frame
                    break;
                }

                if (timeToReachTarget > moveTimeRemaining)  // Will not reach target this frame, move towards it and break
                {
                    pos += delta * (moveTimeRemaining / timeToReachTarget);
                    break;
                }

                // Reach target, decrease time remaining, calculate new target and continue
                moveTimeRemaining -= timeToReachTarget;
                pos = targetTile;
                targetTile = CalculateTargetTile(pos, FlowField, Width);
            }

            if (!agent.TargetTile.Equals(targetTile))
            {
                agent.TargetTile = targetTile;
                agent.LastTimeTargetWasUpdated = Time;
            }

            var finalDelta = pos.x0y() - transform.Position;
            if (!finalDelta.Equals(float3.zero))
            {
                var timePassed = Time - agent.LastTimeTargetWasUpdated;
                var t = Mathf.Clamp01(timePassed / TurnDuration);

                transform.Rotation = math.slerp(
                    transform.Rotation.value,
                    quaternion.LookRotation(finalDelta, Up).value,
                    t);

                transform.Position = pos.x0y();
            }
        }
    }

    private unsafe static int2 CalculateTargetTile(float2 pos, int* flowField, int width)
    {
        var currentTile = (int2)math.round(pos);
        var target = flowField[Flatten(currentTile, width)];
        if (target < 0)
        {
            return currentTile;   // Nowhere to go
        }

        return UnFlatten(target, width);
    }

    private static bool IsValidTarget(int2 targetTile, int width, int height) =>
        targetTile.x >= 0 &&
        targetTile.x < width &&
        targetTile.y >= 0 &&
        targetTile.y < height;
}
