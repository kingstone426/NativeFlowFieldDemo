using Unity.Collections;
using UnityEngine;

public class ReadmeGenerator : Generator
{
    public float Delay = 0.3f;
    public int MinValue = -3;
    public int MaxValue = 15;

    private float lastTime;

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
        var config = GetComponent<FlowFieldConfig>();
        var value = config.BakeOptions.Iterations;
        value++;
        var cycled = ((value - MinValue) % (MaxValue - MinValue) + (MaxValue - MinValue)) % (MaxValue - MinValue) + MinValue;
        config.BakeOptions.Iterations = cycled;
        config.UpdateConfig();
    }
}

