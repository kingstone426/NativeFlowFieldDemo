using Unity.Entities;
using UnityEngine;

public class AgentPrefabAuthoring : MonoBehaviour {}

public class AgentPrefabBaker : Baker<AgentPrefabAuthoring>
{
    public override void Bake(AgentPrefabAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<AgentComponent>(entity);
        AddComponent<Prefab>(entity);
    }
}
