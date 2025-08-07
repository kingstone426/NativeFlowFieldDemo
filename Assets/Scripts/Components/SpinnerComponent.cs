using Unity.Entities;

public struct SpinnerComponent : IComponentData
{
    public float SpinSpeed;
    public float BobSpeed;
    public float BobOffset;
    public float BobAmplitude;
}
