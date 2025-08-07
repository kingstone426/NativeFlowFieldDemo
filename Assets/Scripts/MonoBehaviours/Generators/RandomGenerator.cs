using FlowFieldAI;
using Unity.Collections;
using UnityEngine;

public class RandomGenerator : Generator
{
    public float ObstacleRate = 0.4f;

    public override void Generate(NativeArray<float> inputField, int width, int height, string seed)
    {
        var rand = new System.Random(seed.GetStableHashCode());

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var index = x + y * width;
                if (y==0 || x==0 || y==height-1 || x==width-1)
                {
                    inputField[index] = NativeFlowField.ObstacleCell; // Wall
                }
                else if (rand.NextDouble() < ObstacleRate)
                {
                    inputField[index] = NativeFlowField.ObstacleCell; // Obstacle
                }
                else
                {
                    inputField[index] = NativeFlowField.FreeCell; // Free
                }
            }
        }
    }
}
