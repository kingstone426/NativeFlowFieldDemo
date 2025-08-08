using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class ReadmeGenerator : Generator
{
    public GameObject WalkerPrefab;
    public bool GenerateWalkers = true;

    public float Delay = 0.3f;
    public int MinValue = -3;
    public int MaxValue = 15;

    private float lastTime;
    private readonly Vector3[] walkerSpawnPositions =
    {
        new(1, 0, 0),
        new(0, 0, 5),
        new(6, 0, 1),
        new(5, 0, 6),
    };
    private readonly List<GameObject> walkers = new();
    private FlowFieldConfig config;

    private void Awake() => config = GetComponent<FlowFieldConfig>();

    public override void Generate(NativeArray<float> inputField, int width, int height, string seed)
    {
        float W = float.MinValue;   // Walkable
        float O = float.MaxValue;   // Obstacle
        float T = 0;                // Target

        // Let's populate the input field with some map data
        NativeArray<float>.Copy(new float[]
        {
            O, W, W, W, W, W, O,
            W, O, O, W, O, O, W,
            W, O, W, W, W, O, W,
            W, W, W, T, W, W, W,
            W, O, W, W, W, O, W,
            W, O, O, W, O, O, W,
            O, W, W, W, W, W, O,
        }, inputField);
    }

    private void Update()
    {
        if (Time.time - lastTime < Delay)
        {
            return;
        }

        lastTime = Time.time;

        if (GenerateWalkers)
        {
            UpdateWalkers();
        } else
        {
            UpdateBakeIterations();
        }
    }

    private void UpdateWalkers()
    {
        if (!config.FlowField.NextIndices.IsCreated)
        {
            return;
        }

        if (walkers.Count < walkerSpawnPositions.Length)
        {
            var go = Instantiate(WalkerPrefab, walkerSpawnPositions[walkers.Count], Quaternion.identity);
            go.name = "Walker";
            walkers.Add(go);
        }

        for (var index = 0; index < walkers.Count; index++)
        {
            var walker = walkers[index];
            var pos = new float2(walker.transform.position.x, walker.transform.position.z);
            var cell = (int2)math.round(pos);
            var cellIndex = cell.x + cell.y * config.FlowField.Width;
            var nextIndex = config.FlowField.NextIndices[cellIndex];
            if (nextIndex == cellIndex)
            {
                walker.transform.position = walkerSpawnPositions[index];
            }
            else
            {
                cellIndex = config.FlowField.NextIndices[cellIndex];
                cell.x = cellIndex % config.FlowField.Width;
                cell.y = cellIndex / config.FlowField.Width;
                walker.transform.position = new Vector3(cell.x, 0, cell.y);
            }
        }
    }

    private void UpdateBakeIterations()
    {
        var value = config.BakeOptions.Iterations;
        value++;
        var cycled = ((value - MinValue) % (MaxValue - MinValue) + (MaxValue - MinValue)) % (MaxValue - MinValue) + MinValue;
        config.BakeOptions.Iterations = cycled;
        config.UpdateConfig();
    }
}

