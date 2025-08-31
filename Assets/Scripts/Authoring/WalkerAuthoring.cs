using Unity.Entities;
using UnityEngine;

public class WalkerAuthoring : MonoBehaviour {}

public class WalkerBaker : Baker<WalkerAuthoring>
{
    public override void Bake(WalkerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<WalkerComponent>(entity);
        AddComponent<Prefab>(entity);
    }
}
