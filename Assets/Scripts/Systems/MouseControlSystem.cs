using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
[DisableAutoCreation]
public partial class MouseControlSystem : SystemBase
{
    private Entity prefab;
    private EntityQuery prefabQuery;
    private bool spawned;

    protected override void OnCreate()
    {
        base.OnCreate();

        prefabQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(CoinTag), typeof(Prefab) },
            Options = EntityQueryOptions.IncludePrefab
        });

        RequireForUpdate(prefabQuery);
    }

    protected override void OnStartRunning() => prefab = prefabQuery.GetSingletonEntity();

    private void Spawn()
    {
        var entity = EntityManager.Instantiate(prefab);
        EntityManager.AddComponent<MouseTag>(entity);
        spawned = true;
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        if (!spawned)
        {
            Spawn();
        }

        if (!Input.GetMouseButton(0))
        {
            return;
        }

        var mousePos = Input.mousePosition;
        var pos = new int2(Mathf.RoundToInt(mousePos.x), Mathf.RoundToInt(mousePos.y));

        Dependency = Entities
            .WithBurst()
            .ForEach((ref LocalTransform transform, in MouseTag _) =>
            {
                transform.Position = pos.x0y();
            }).ScheduleParallel(Dependency);
    }
}
