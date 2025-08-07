using FlowFieldAI;
using Unity.Collections;
using Unity.Entities;

public class FlowConfig : IComponentData
{
    public readonly int Width;
    public readonly int Height;
    public BakeOptions BakeOptions;
    public NativeArray<float> InputField;
    public NativeArray<float> Terrain;
    public int AgentSpawnCount;

    public FlowConfig() {}

    public FlowConfig(int width, int height, NativeArray<float> inputField, NativeArray<float> terrain, BakeOptions bakeOptions, int agentSpawnCount)
    {
        Width = width;
        Height = height;
        InputField = inputField;
        Terrain = terrain;
        BakeOptions = bakeOptions;
        AgentSpawnCount = agentSpawnCount;
    }
}
