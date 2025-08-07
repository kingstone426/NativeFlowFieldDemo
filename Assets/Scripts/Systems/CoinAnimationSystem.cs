using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial class SpinnerSystem : SystemBase
{
    protected override void OnUpdate() => Dependency = new CoinAnimationJob
        {
            Time = (float)SystemAPI.Time.ElapsedTime
        }.ScheduleParallel(Dependency);

    [BurstCompile]
    private partial struct CoinAnimationJob : IJobEntity
    {
        public float Time;

        private void Execute(SpinnerComponent spinner, ref LocalTransform transform)
        {
            var pos = transform.Position;
            transform.Rotation = quaternion.EulerXYZ(math.PIHALF, spinner.SpinSpeed*Time, 0);
            transform.Position = new float3(pos.x, spinner.BobOffset + spinner.BobAmplitude*math.sin(spinner.BobSpeed*Time), pos.z);
        }
    }
}

