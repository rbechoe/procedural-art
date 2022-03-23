using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleNoiseFilter : INoiseFilter
{
    private Noise noise = new Noise();
    private NoiseSettings.SimpleNoiseSettings noiseSettings;

    public SimpleNoiseFilter(NoiseSettings.SimpleNoiseSettings settings)
    {
        noiseSettings = settings;
    }

    public float Evaluate(Vector3 point)
    {
        float noiseValue = 0;
        float frequency = noiseSettings.baseRoughness;
        float amplitude = 1;

        for (int i = 0; i < noiseSettings.numLayers; i++)
        {
            float v = noise.Evaluate(point * frequency + noiseSettings.center);
            noiseValue += (v + 1) * .5f * amplitude;
            frequency *= noiseSettings.roughness;
            amplitude *= noiseSettings.persistence;
        }

        noiseValue = noiseValue - noiseSettings.minValue;
        return noiseValue * noiseSettings.strength;
    }
}
