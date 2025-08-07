using Unity.Entities;
using UnityEngine;

public class CoinPrefabAuthoring : MonoBehaviour
{
    public float SpinSpeed=1;
    public float BobSpeed=1;
    public float BobOffset=1;
    public float BobAmplitude=1;
}

public class CoinPrefabBaker : Baker<CoinPrefabAuthoring>
{
    public override void Bake(CoinPrefabAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<CoinTag>(entity);
        AddComponent<Prefab>(entity);

        AddComponent(entity, new SpinnerComponent
        {
            SpinSpeed = authoring.SpinSpeed,
            BobSpeed = authoring.BobSpeed,
            BobOffset = authoring.BobOffset,
            BobAmplitude = authoring.BobAmplitude
        });
    }
}
