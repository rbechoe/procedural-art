using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.WasapiAudio.Scripts.Unity;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

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
    public Volume volume;
    public Gradient emissionColor;
    public bool enableAudio;
    public bool enableEpilepticEpisode;
    public float[] stereoSamples;
    [Range(0, 100)]
    public float rmsMultiplier = 10f;
    public float seizureTreshold = 0.4f;
    private int sampleSize = 128;
    private float rangeCd = 1f;

    [Header("Readable RMS")]
    public float currentRms; // used for basic calcs and rotation
    public float rmsValueStereo; // used for size

    [Header("Balancers")]
    public float baseMultiplier = 0.5f;
    public float peakBass;
    public float stereoMultiplier = 5f;

    [Header("Readable Spectrum")]
    public float subBass; // emission
    public float bass; // color
    public float lowMidrange; // layer 1
    public float midrange; // layer 2
    public float upperMidrange; // layer 3
    public float presence; // flip emission (after fixing land gradient)
    public float brilliance; // tint
    private float emissionMultiplier = 1;
    private Color chosenCol;

    private void Start()
    {
        sampleSize = WasapiAudioSource.SpectrumSize;
        stereoSamples = new float[sampleSize];
    }

    private void Update()
    {
        // planet orientation
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

        float newSpeed = Mathf.Pow(currentRms, 5);
        if (rotationSpeed > newSpeed) rotationSpeed -= Time.deltaTime * 2;
        else
        {
            rotationSpeed += newSpeed;
            rotationSpeed = Mathf.Clamp(rotationSpeed, 0, 15);
        }

        // hotkeys
        if (Input.GetKeyUp(KeyCode.M))
        {
            enableEpilepticEpisode = !enableEpilepticEpisode;
        }
        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }

        // update planets
        foreach (Planet planet in planets)
        {
            planet.transform.eulerAngles = new Vector3(0, yRot, zRot);
        }

        // smooth peak values
        if (peakBass > 0.25f) peakBass -= Time.deltaTime;
        stereoMultiplier = baseMultiplier / peakBass;

        currentRms = rmsValueStereo;
    }

    private void FixedUpdate()
    {
        AnalyzeSound();
        PlanteraryStereo();
    }

    // spread audio spectrum over 7 bands
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

        subBass *= stereoMultiplier;
        bass *= stereoMultiplier;
        lowMidrange *= stereoMultiplier;
        midrange *= stereoMultiplier;
        upperMidrange *= stereoMultiplier;
        presence *= stereoMultiplier;
        brilliance *= stereoMultiplier;

        if (peakBass < subBass) peakBass = subBass;

        if (enableEpilepticEpisode)
            emissionMultiplier = (presence > seizureTreshold) ? -1f : 1f; // flip emission
        else
            emissionMultiplier = 1;

        rmsValueStereo = Mathf.Sqrt(CalculateRMS(stereoSamples) / sampleSize) * rmsMultiplier;
    }

    // calculate the rms
    private float CalculateRMS(float[] samples)
    {
        float sum = 0;
        for (int i = 0; i < sampleSize; ++i)
        {
            sum += samples[i] * samples[i];
        }

        return sum;
    }

    // adjust the planet based on the audio spectrum from the system
    private void PlanteraryStereo()
    {
        // adjust extremely specific planet settings for the best visualization experience
        foreach (Planet planet in planets)
        {
            float tint = Mathf.Clamp(brilliance * tintBalancer, 0, maxTint);
            chosenCol = emissionColor.Evaluate(bass * 2);
            Bloom bloom; 
            volume.profile.TryGet(out bloom);
            ColorParameter col = new ColorParameter(chosenCol);
            bloom.tint.SetValue(col);
            planet.colorSettings.emissionColor = chosenCol;
            planet.colorSettings.emissionStrength = subBass * emissionMultiplier;
            planet.colorSettings.biomeColorSettings.biomes[1].tintPercent = tint;
            planet.shapeSettings.noiseLayers[0].noiseSettings.simpleNoiseSettings.minValue = tint * minValBalancer;

            // x
            planet.shapeSettings.noiseLayers[0].noiseSettings.simpleNoiseSettings.center += new Vector3(lowMidrange + midrange + upperMidrange, 0, 0) * 0.1f;
            if (planet.shapeSettings.noiseLayers[0].noiseSettings.simpleNoiseSettings.center.x > 360)
                planet.shapeSettings.noiseLayers[0].noiseSettings.simpleNoiseSettings.center = Vector3.zero;
            // y
            planet.shapeSettings.noiseLayers[1].noiseSettings.simpleNoiseSettings.center += new Vector3(0, midrange + upperMidrange, 0) * 0.1f;
            if (planet.shapeSettings.noiseLayers[1].noiseSettings.simpleNoiseSettings.center.y > 360)
                planet.shapeSettings.noiseLayers[1].noiseSettings.simpleNoiseSettings.center = Vector3.zero;
            // z
            planet.shapeSettings.noiseLayers[2].noiseSettings.ridgidNoiseSettings.center += new Vector3(0, 0, upperMidrange) * 0.1f;
            if (planet.shapeSettings.noiseLayers[2].noiseSettings.ridgidNoiseSettings.center.z > 360)
                planet.shapeSettings.noiseLayers[2].noiseSettings.ridgidNoiseSettings.center = Vector3.zero;

            planet.shapeSettings.planetRadius = baseRadius + tint;
            planet.GeneratePlanet();
        }
    }
}
