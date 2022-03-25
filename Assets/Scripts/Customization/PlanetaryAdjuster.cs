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
    private float rotationSpeed = 1;
    private float rangeMultiplier = 0;

    [Header("Audio settings")]
    public Gradient emissionColor;
    public bool enableAudio;
    public bool recordMic;
    public bool enableEpilepticEpisode;
    public float micNormalizer = 5f;
    public float[] stereoSamples;
    public float[] micSamples;
    [Range(0, 100)]
    public float rmsMultiplier = 10f;
    public float stereoMultiplier = 5f;
    public float seizureTreshold = 0.4f;
    private int sampleSize = 128;
    private float rangeCd = 1f;
    private AudioClip record;

    [Header("Readable RMS")]
    public float currentRms; // used for basic calcs and rotation
    public float rmsValueMic; // used for all for mic
    public float rmsValueStereo; // used for size
    [Header("Readable Spectrum")]
    public float subBass; // emission
    public float bass; // color
    public float lowMidrange; // layer 1
    public float midrange; // layer 2
    public float upperMidrange; // layer 3
    public float presence; // flip emission (after fixing land gradient)
    public float brilliance; // tint
    private float emissionMultiplier = 1;

    private void Start()
    {
        record = gameObject.GetComponent<AudioSource>().clip;
        sampleSize = WasapiAudioSource.SpectrumSize;
        stereoSamples = new float[sampleSize];
        micSamples = new float[sampleSize];
    }

    private void Update()
    {
        rangeCd -= Time.deltaTime * 2f;
        yRot += rotationSpeed * Time.deltaTime * 5;
        zRot += rotationSpeed * Time.deltaTime * 5;
        if (rangeMultiplier > 0) rangeMultiplier -= Time.deltaTime;
        if (rotationSpeed > 1) rotationSpeed -= Time.deltaTime * 2;
        else
        {
            rotationSpeed += Mathf.Pow(currentRms, 5);
            rotationSpeed = Mathf.Clamp(rotationSpeed, 0, 15);
        }

        if (rangeMultiplier <= 0 || rangeCd <= 0)
        {
            rangeMultiplier = currentRms * currentRms;
            rangeCd = (1 - rangeMultiplier) * 0.1f;
        }

        if (rotationSpeed > 1) rotationSpeed -= Time.deltaTime * 2;
        else
        {
            rotationSpeed += Mathf.Pow(currentRms, 5);
            rotationSpeed = Mathf.Clamp(rotationSpeed, 0, 15);
        }

        if (enableAudio)
        {
            if (recordMic)
            {
                currentRms = rmsValueMic;
            }
            else
            {
                currentRms = rmsValueStereo;
            }
        }

        foreach (Planet planet in planets)
        {
            planet.transform.eulerAngles = new Vector3(0, yRot, zRot);
        }
    }

    private void FixedUpdate()
    {
        if (!enableAudio) return;

        // fill audio spectrum
        AnalyzeMic();
        AnalyzeSound();

        // visualize based on spectrum
        if (recordMic)
        {
            PlanetaryMic();
        }
        else
        {
            PlanteraryStereo();
        }
    }

    private void AnalyzeSound()
    {
        stereoSamples = GetSpectrumData();

        subBass = 0;
        bass = 0;
        lowMidrange = 0;
        midrange = 0;
        upperMidrange = 0;
        presence = 0;
        brilliance = 0;

        int frequencyBand = sampleSize / 8;
        for (int i = 0; i < frequencyBand; i++)
        {
            subBass         += stereoSamples[i];
            bass            += stereoSamples[i + frequencyBand];
            lowMidrange     += stereoSamples[i + frequencyBand * 2];
            midrange        += stereoSamples[i + frequencyBand * 3];
            upperMidrange   += stereoSamples[i + frequencyBand * 4];
            presence        += stereoSamples[i + frequencyBand * 5];
            brilliance      += stereoSamples[i + frequencyBand * 6];
            brilliance      += stereoSamples[i + frequencyBand * 7];
        }

        subBass *= subBass * stereoMultiplier;
        bass *= bass * stereoMultiplier;
        lowMidrange *= lowMidrange * stereoMultiplier;
        midrange *= midrange * stereoMultiplier;
        upperMidrange *= upperMidrange * stereoMultiplier;
        presence *= stereoMultiplier;
        brilliance *= brilliance * stereoMultiplier;

        if (enableEpilepticEpisode)
            emissionMultiplier = (presence > seizureTreshold) ? -1f : 1f; // flip emission
        else
            emissionMultiplier = 1;

        rmsValueStereo = Mathf.Sqrt(CalculateRMS(stereoSamples) / sampleSize) * rmsMultiplier;
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

        rmsValueMic = Mathf.Sqrt(CalculateRMS(micSamples) / sampleSize) * rmsMultiplier / micNormalizer;
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

    private void PlanetaryMic()
    {
        // adjust extremely specific planet settings for the best visualization experience
        foreach (Planet planet in planets)
        {
            float tint = Mathf.Clamp(rangeMultiplier * tintBalancer, 0, maxTint);
            planet.colorSettings.emissionStrength = rangeMultiplier * rangeMultiplier;
            planet.colorSettings.biomeColorSettings.biomes[1].tintPercent = tint;
            planet.shapeSettings.noiseLayers[0].noiseSettings.simpleNoiseSettings.minValue = tint * minValBalancer;

            // x
            planet.shapeSettings.noiseLayers[0].noiseSettings.simpleNoiseSettings.center += new Vector3(rangeMultiplier, 0, 0) * 0.1f;
            if (planet.shapeSettings.noiseLayers[0].noiseSettings.simpleNoiseSettings.center.x > 360)
                planet.shapeSettings.noiseLayers[0].noiseSettings.simpleNoiseSettings.center = Vector3.zero;
            // y
            planet.shapeSettings.noiseLayers[1].noiseSettings.simpleNoiseSettings.center += new Vector3(0, rangeMultiplier, 0) * 0.01f;
            if (planet.shapeSettings.noiseLayers[1].noiseSettings.simpleNoiseSettings.center.y > 360)
                planet.shapeSettings.noiseLayers[1].noiseSettings.simpleNoiseSettings.center = Vector3.zero;
            // z
            planet.shapeSettings.noiseLayers[2].noiseSettings.ridgidNoiseSettings.center += new Vector3(0, 0, rangeMultiplier) * 0.001f;
            if (planet.shapeSettings.noiseLayers[2].noiseSettings.ridgidNoiseSettings.center.z > 360)
                planet.shapeSettings.noiseLayers[2].noiseSettings.ridgidNoiseSettings.center = Vector3.zero;

            planet.shapeSettings.planetRadius = baseRadius + rmsValueStereo;
            planet.GeneratePlanet();
        }
    }

    public Color chosenCol;
    private void PlanteraryStereo()
    {
        // adjust extremely specific planet settings for the best visualization experience
        foreach (Planet planet in planets)
        {
            float tint = Mathf.Clamp(brilliance * tintBalancer, 0, maxTint);
            planet.colorSettings.emissionStrength = subBass * subBass * emissionMultiplier;
            chosenCol = emissionColor.Evaluate(bass * 2);
            planet.colorSettings.emissionColor = chosenCol;
            planet.colorSettings.biomeColorSettings.biomes[1].tintPercent = tint;
            planet.shapeSettings.noiseLayers[0].noiseSettings.simpleNoiseSettings.minValue = tint * minValBalancer;

            // x
            planet.shapeSettings.noiseLayers[0].noiseSettings.simpleNoiseSettings.center += new Vector3(lowMidrange, 0, 0) * 0.1f;
            if (planet.shapeSettings.noiseLayers[0].noiseSettings.simpleNoiseSettings.center.x > 360)
                planet.shapeSettings.noiseLayers[0].noiseSettings.simpleNoiseSettings.center = Vector3.zero;
            // y
            planet.shapeSettings.noiseLayers[1].noiseSettings.simpleNoiseSettings.center += new Vector3(0, midrange, 0) * 0.01f;
            if (planet.shapeSettings.noiseLayers[1].noiseSettings.simpleNoiseSettings.center.y > 360)
                planet.shapeSettings.noiseLayers[1].noiseSettings.simpleNoiseSettings.center = Vector3.zero;
            // z
            planet.shapeSettings.noiseLayers[2].noiseSettings.ridgidNoiseSettings.center += new Vector3(0, 0, upperMidrange) * 0.001f;
            if (planet.shapeSettings.noiseLayers[2].noiseSettings.ridgidNoiseSettings.center.z > 360)
                planet.shapeSettings.noiseLayers[2].noiseSettings.ridgidNoiseSettings.center = Vector3.zero;

            planet.shapeSettings.planetRadius = baseRadius + tint;
            planet.GeneratePlanet();
        }
    }
}
