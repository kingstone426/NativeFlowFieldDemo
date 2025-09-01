using FlowFieldAI;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[BurstCompile]
public partial class MouseDrawSystem : SystemBase
{
    private EntityQuery flowFieldQuery;

    protected override void OnCreate()
    {
        base.OnCreate();

        flowFieldQuery = new EntityQueryBuilder(Allocator.Temp).
            WithAll<FlowConfig>().
            Build(this);
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1))
        {
            return;
        }

        var entities = flowFieldQuery.ToEntityArray(Allocator.Temp);

        foreach (var entity in entities)
        {
            var flowConfig = EntityManager.GetComponentData<FlowConfig>(entity);

            // TODO: Adapt for mismatching aspect ratios

            var mousePos = Input.mousePosition;
            var fpos = new float2(flowConfig.Width*mousePos.x/Screen.width, flowConfig.Height*mousePos.y/Screen.height);

            for (var y=-1; y<=1; y++)
            for (var x=-1; x<=1; x++)
            {
                var pos = new int2((int)(fpos.x + 0.5f*x), (int)(fpos.y + 0.5f*y));
                flowConfig.Terrain[pos.x + pos.y * flowConfig.Width] = Input.GetMouseButton(0)
                    ? NativeFlowField.ObstacleCell
                    : NativeFlowField.FreeCell;
            }
        }
    }
}
