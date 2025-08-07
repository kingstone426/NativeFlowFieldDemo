
using Unity.Collections;
using UnityEngine;

public abstract class Generator : MonoBehaviour
{
    public abstract void Generate(NativeArray<float> inputField, int width, int height, string seed);
}


