using UnityEngine;
using Unity.Entities;

public class VisualProxyAuthoring : MonoBehaviour
{
    public GameObject VisualAgentPrefab;

    private class VisualProxyBaker : Baker<VisualProxyAuthoring>
    {
        public override void Bake(VisualProxyAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent<Prefab>(entity);
            AddComponentObject(entity, new VisualPrefabAssets
            {
                AgentPrefab = authoring.VisualAgentPrefab
            });
        }
    }
}
