using Unity.Entities;
using Unity.Mathematics;

public struct AgentComponent : IComponentData
{
    public int2 TargetTile;
    public float LastTimeTargetWasUpdated;
}
