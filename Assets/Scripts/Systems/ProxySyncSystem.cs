using Unity.Entities;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ProxyCullingSystem))]
public partial class ProxySyncSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Dependency.Complete();
        new ProxySyncJob().Run();
    }

    private partial struct ProxySyncJob : IJobEntity
    {
        private void Execute(EnabledRefRO<VisualProxySync> _,in LocalTransform transform, VisualProxy proxy)
        {
            proxy.Proxy.transform.position = transform.Position;
            proxy.Proxy.transform.rotation = transform.Rotation;
        }
    }
}
