using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.WasapiAudio.Scripts.Unity;

public class PlanetaryAdjuster : AudioVisualizationEffect
{
    [Header("Planet settings")]
    public List<Planet> planets = new List<Planet>();
    public float tintBalancer = 1f;
    public float minValBalancer = 0.25f;
    public float maxTint = 0.9f;
    public float baseRadius = 3.5f;
    private float emissiveValue;
    private float rotationSpeed = 1;
    private float rangeMultiplier = 0;
    private float yRot, zRot;

    [Header("Audio settings")]
    public bool enableAudio;
    private int sampleSize = 128;
    public float[] samples;
    private float rmsValue;
    [Range(0, 20)]
    public float rmsMultiplier = 10f;
    private float rangeCd = 1f;

    private void Start()
    {
        sampleSize = WasapiAudioSource.SpectrumSize;
        samples = new float[sampleSize];
    }

    private void Update()
    {
        emissiveValue = rangeMultiplier;
        rangeCd -= Time.deltaTime * 2f;
        yRot += rotationSpeed * Time.deltaTime * 5;
        zRot += rotationSpeed * Time.deltaTime * 5;
        if (rangeMultiplier > 0) rangeMultiplier -= Time.deltaTime;
        if (rotationSpeed > 1) rotationSpeed -= Time.deltaTime * 2;
        else rotationSpeed += Mathf.Pow(rmsValue, 5);

        if (rangeMultiplier <= 0 || rangeCd <= 0)
        {
            rangeMultiplier = rmsValue * rmsValue;
            rangeCd = (1 - rangeMultiplier) * 0.1f;
        }

        if (enableAudio)
        {
            AnalyzeSound();
        }

        foreach (Planet planet in planets)
        {
            planet.transform.eulerAngles = new Vector3(0, yRot, zRot);
        }
    }

    private void FixedUpdate()
    {
        // adjust extremely specific planet settings for the best visualization experience
        foreach(Planet planet in planets)
        {
            float tint = Mathf.Clamp(emissiveValue * tintBalancer, 0, maxTint);
            planet.colorSettings.emissionStrength = emissiveValue * emissiveValue;
            planet.colorSettings.biomeColorSettings.biomes[1].tintPercent = tint;
            planet.shapeSettings.noiseLayers[0].noiseSettings.simpleNoiseSettings.minValue = tint * minValBalancer;
            planet.shapeSettings.noiseLayers[0].noiseSettings.simpleNoiseSettings.center += new Vector3(emissiveValue, 0, 0) * 0.1f;
            planet.shapeSettings.noiseLayers[1].noiseSettings.simpleNoiseSettings.center += new Vector3(0, emissiveValue, 0) * 0.01f;
            planet.shapeSettings.noiseLayers[2].noiseSettings.ridgidNoiseSettings.center += new Vector3(0, 0, emissiveValue) * 0.001f;
            planet.shapeSettings.planetRadius = baseRadius + tint;
            planet.GeneratePlanet();
        }
    }

    private void AnalyzeSound()
    {
        samples = GetSpectrumData();

        // Get RMS
        float sum = 0;
        for (int i = 0; i < sampleSize; i++)
        {
            sum += samples[i] * samples[i];
        }
        rmsValue = Mathf.Sqrt(sum / sampleSize) * rmsMultiplier;
    }
}
