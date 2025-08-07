using System.Collections.Generic;
using FlowFieldAI;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class MazeGenerator : Generator
{
    public override void Generate(NativeArray<float> inputField, int width, int height, string seed)
    {
        var rand = new System.Random(seed.GetStableHashCode());

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                inputField[x + y * width] = NativeFlowField.ObstacleCell; // Obstacle
            }
        }

        var validNeighbors = new List<int2>();
        var backtrack = new Stack<int2>();
        var pos = new int2(rand.Next((width-1)/2), rand.Next((height-1)/2));
        pos = new int2(2 * pos.x + 1, 2*pos.y + 1);
        inputField[pos.x + pos.y * width] = NativeFlowField.FreeCell; // Free

        while (true)
        {
            backtrack.Push(pos);

            GetValidDirections(inputField, pos, width, height, validNeighbors);
            if (validNeighbors.Count == 0)
            {
                var breakOuter = false;
                while (backtrack.Count > 0 && !breakOuter)
                {
                    pos = backtrack.Pop();

                    GetValidDirections(inputField, pos, width, height, validNeighbors);
                    if (validNeighbors.Count > 0)
                    {
                        breakOuter = true;
                    }
                }

                if (validNeighbors.Count == 0)
                {
                    return;
                }
            }

            var neighbor = validNeighbors[rand.Next(validNeighbors.Count)];
            pos += neighbor;
            inputField[pos.x + pos.y * width] = NativeFlowField.FreeCell; // Free
            pos += neighbor;
            inputField[pos.x + pos.y * width] = NativeFlowField.FreeCell; // Free
        }
    }

    private static readonly int2[] Directions =
    {
        new(-1, 0),
        new(1, 0),
        new(0, 1),
        new(0, -1),
    };

    private static void GetValidDirections(NativeArray<float> inputField, int2 pos, int width, int height, List<int2> validDirections)
    {
        validDirections.Clear();

        foreach (var direction in Directions)
        {
            var neighborPos = pos + 2*direction;
            if (neighborPos.x < 1 || neighborPos.x >= width - 1 || neighborPos.y < 1 || neighborPos.y >= height - 1)
            {
                continue;
            }

            if (inputField[neighborPos.x + neighborPos.y * width] >= NativeFlowField.ObstacleCell)
            {
                validDirections.Add(direction);
            }
        }
    }
}

