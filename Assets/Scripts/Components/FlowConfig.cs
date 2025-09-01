using FlowFieldAI;
using Unity.Collections;
using Unity.Entities;

public class FlowConfig : IComponentData
{
    public readonly int Width;
    public readonly int Height;
    public BakeOptions BakeOptions;
    public NativeArray<float> Terrain;
    public int AgentSpawnCount;
    public int WalkerSpawnCount;
    public float WalkerVelocitySmoothingFactor;

    public FlowConfig() {}

    public FlowConfig(int width, int height, NativeArray<float> terrain, BakeOptions bakeOptions, int agentSpawnCount, int walkerSpawnCount, float walkerVelocitySmoothingFactor) : this()
    {
        Width = width;
        Height = height;
        Terrain = terrain;
        BakeOptions = bakeOptions;
        AgentSpawnCount = agentSpawnCount;
        WalkerSpawnCount = walkerSpawnCount;
        WalkerVelocitySmoothingFactor = walkerVelocitySmoothingFactor;
    }
}
