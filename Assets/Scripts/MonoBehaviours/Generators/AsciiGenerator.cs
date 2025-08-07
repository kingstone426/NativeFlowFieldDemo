using System;
using System.Linq;
using FlowFieldAI;
using Unity.Collections;
using UnityEngine;

public class AsciiGenerator : Generator
{
    [TextArea(20, 20)]
    public string Ascii;

    public override void Generate(NativeArray<float> inputField, int width, int height, string seed)
    {
        var lines = Ascii.Split('\n');
        var asciiWidth = lines[0].Length;
        var asciiHeight = lines.Length;
        if (lines.Any(line => line.Length != asciiWidth))
        {
            throw new ArgumentException("Ascii string has different width in lines", nameof(Ascii));
        }

        for (var y=0; y<asciiHeight; y++)
        {
            for (var x=0; x<asciiWidth; x++)
            {
                var dst = x+(width-asciiWidth)/2 + (asciiHeight-y+(height-asciiHeight)/2) * width;

                var c = lines[y][x];

                inputField[dst] = c switch
                {
                    '░' => NativeFlowField.FreeCell,
                    '█' => NativeFlowField.ObstacleCell,
                    '0' => 0,
                    _ => throw new ArgumentException($"Invalid ObstacleMap char: {c} in string: {Ascii}", $"{nameof(Ascii)} at ({x},{y})"),
                };
            }
        }
    }
}

