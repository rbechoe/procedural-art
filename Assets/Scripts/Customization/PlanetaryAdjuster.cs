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
    private float yRot, zRot;
    private float emissiveValue;
    private float rotationSpeed = 1;
    private float rangeMultiplier = 0;

    [Header("Audio settings")]
    public bool enableAudio;
    public bool recordMic;
    private int sampleSize = 128;
    public float micNormalizer = 5f;
    public float[] stereoSamples;
    public float[] micSamples;
    private float rmsValue;
    [Range(0, 100)]
    public float rmsMultiplier = 10f;
    private float rangeCd = 1f;
    private MicInput micInput;
    private AudioClip record;

    private void Start()
    {
        micInput = gameObject.GetComponent<MicInput>();
        record = gameObject.GetComponent<AudioSource>().clip;
        sampleSize = WasapiAudioSource.SpectrumSize;
        stereoSamples = new float[sampleSize];
        micSamples = new float[sampleSize];
    }

    private void Update()
    {
        emissiveValue = rangeMultiplier;
        rangeCd -= Time.deltaTime * 2f;
        yRot += rotationSpeed * Time.deltaTime * 5;
        zRot += rotationSpeed * Time.deltaTime * 5;
        if (rangeMultiplier > 0) rangeMultiplier -= Time.deltaTime;
        if (rotationSpeed > 1) rotationSpeed -= Time.deltaTime * 2;
        else
        {
            rotationSpeed += Mathf.Pow(rmsValue, 5);
            rotationSpeed = Mathf.Clamp(rotationSpeed, 0, 15);
        }

        if (rangeMultiplier <= 0 || rangeCd <= 0)
        {
            rangeMultiplier = rmsValue * rmsValue;
            rangeCd = (1 - rangeMultiplier) * 0.1f;
        }

        if (rotationSpeed > 1) rotationSpeed -= Time.deltaTime * 2;
        else
        {
            rotationSpeed += Mathf.Pow(rmsValue, 5);
            rotationSpeed = Mathf.Clamp(rotationSpeed, 0, 15);
        }

        if (enableAudio)
        {
            if (recordMic)
            {
                AnalyzeMic();
            }
            else
            {
                AnalyzeSound();
            }
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

            // x
            planet.shapeSettings.noiseLayers[0].noiseSettings.simpleNoiseSettings.center += new Vector3(emissiveValue, 0, 0) * 0.1f;
            if (planet.shapeSettings.noiseLayers[0].noiseSettings.simpleNoiseSettings.center.x > 360)
                planet.shapeSettings.noiseLayers[0].noiseSettings.simpleNoiseSettings.center = Vector3.zero;
            // y
            planet.shapeSettings.noiseLayers[1].noiseSettings.simpleNoiseSettings.center += new Vector3(0, emissiveValue, 0) * 0.01f;
            if (planet.shapeSettings.noiseLayers[1].noiseSettings.simpleNoiseSettings.center.y > 360)
                planet.shapeSettings.noiseLayers[1].noiseSettings.simpleNoiseSettings.center = Vector3.zero;
            // z
            planet.shapeSettings.noiseLayers[2].noiseSettings.ridgidNoiseSettings.center += new Vector3(0, 0, emissiveValue) * 0.001f;
            if (planet.shapeSettings.noiseLayers[2].noiseSettings.ridgidNoiseSettings.center.z > 360)
                planet.shapeSettings.noiseLayers[2].noiseSettings.ridgidNoiseSettings.center = Vector3.zero;

            planet.shapeSettings.planetRadius = baseRadius + tint;
            planet.GeneratePlanet();
        }
    }

    private void AnalyzeSound()
    {
        stereoSamples = GetSpectrumData();

        rmsValue = Mathf.Sqrt(CalculateRMS(stereoSamples) / sampleSize) * rmsMultiplier / micNormalizer;
    }

    private void AnalyzeMic()
    {
        micSamples = new float[sampleSize];
        int micPosition = Microphone.GetPosition(null) - (sampleSize + 1);
        if (micPosition < 0)
        {
            return;
        }
        record.GetData(micSamples, micPosition);

        rmsValue = Mathf.Sqrt(CalculateRMS(micSamples) / sampleSize) * rmsMultiplier;
    }

    private float CalculateRMS(float[] samples)
    {
        float sum = 0;
        for (int i = 0; i < sampleSize; ++i)
        {
            sum += samples[i] * samples[i];
        }

        return sum;
    }
}
