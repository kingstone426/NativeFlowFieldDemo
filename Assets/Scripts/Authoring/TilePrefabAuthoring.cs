using Unity.Entities;
using UnityEngine;

public class TilePrefabAuthoring : MonoBehaviour {}

public class TilePrefabBaker : Baker<TilePrefabAuthoring>
{
    public override void Bake(TilePrefabAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<TileTag>(entity);
        AddComponent<Prefab>(entity);
    }
}
