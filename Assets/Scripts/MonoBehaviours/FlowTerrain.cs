using Unity.Cinemachine;
using Unity.Collections;
using UnityEngine;

public class FlowTerrain : MonoBehaviour
{
    public int Width = 100;
    public int Height = 100;
    public string Seed = "seed";

    public NativeArray<float> Terrain => terrain;
    private NativeArray<float> terrain;

    private void Awake()
    {
        terrain = new NativeArray<float>(Width * Height, Allocator.Persistent);

        foreach (var generator in GetComponents<Generator>())
        {
            generator.Generate(terrain, Width, Height, Seed);
        }

        foreach (var cam in GetComponentsInChildren<CinemachineCamera>())
        {
            cam.Lens.OrthographicSize = Height / 2f;
        }
    }

    private void OnDestroy() => terrain.Dispose();
}
