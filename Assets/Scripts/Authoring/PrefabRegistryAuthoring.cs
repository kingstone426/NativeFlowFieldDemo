using Unity.Entities;
using UnityEngine;

public class PrefabRegistryAuthoring : MonoBehaviour
{
    public GameObject[] Prefabs;

    public struct PrefabReference : IBufferElementData
    {
        public Entity PrefabEntity;
    }

    public class PrefabRegistryBaker : Baker<PrefabRegistryAuthoring>
    {
        public override void Bake(PrefabRegistryAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            var buffer = AddBuffer<PrefabReference>(entity);

            foreach (var prefab in authoring.Prefabs)
            {
                if (prefab == null)
                {
                    continue;
                }

                var prefabEntity = GetEntity(prefab, TransformUsageFlags.None);
                buffer.Add(new PrefabReference { PrefabEntity = prefabEntity });
            }
        }
    }
}
