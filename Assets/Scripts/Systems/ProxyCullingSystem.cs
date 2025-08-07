using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(MoveSystem))]
public partial class ProxyCullingSystem : SystemBase
{
    private const float MaxOrthoVisibility = 200;
    private const float ViewportPadding = 2;
    private Camera MainCamera
    {
        get
        {
            if (!camera)
            {
                camera = Camera.main;
            }

            return camera;
        }
    }
    private Camera camera;

    protected override void OnUpdate()
    {
        var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
        Dependency.Complete();
        new ProxyCullingJob
        {
            ProjectionMatrix = MainCamera.transform.worldToLocalMatrix,
            CommandBuffer = commandBuffer,
            OrthoHeight = MainCamera.orthographicSize,
            OrthoWidth = MainCamera.aspect * MainCamera.orthographicSize
        }.Run();
        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();
    }

    private partial struct ProxyCullingJob : IJobEntity
    {
        public float4x4 ProjectionMatrix;
        public float OrthoWidth;
        public float OrthoHeight;
        public EntityCommandBuffer CommandBuffer;

        private void Execute(Entity entity, in LocalTransform transform, VisualProxy proxy)
        {
            var projected = math.mul(ProjectionMatrix, new float4(transform.Position, 1)).xyz;

            var shouldBeActive = math.abs(projected.x) <= OrthoWidth + ViewportPadding
                && math.abs(projected.y) <= OrthoHeight + ViewportPadding
                && OrthoHeight < MaxOrthoVisibility;

            if (proxy.Proxy.gameObject.activeSelf != shouldBeActive)
            {
                proxy.Proxy.gameObject.SetActive(shouldBeActive);
                CommandBuffer.SetComponentEnabled<VisualProxySync>(entity, shouldBeActive);
            }
        }
    }
}

