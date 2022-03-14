using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorGenerator
{
    private ColorSettings settings;

    private Texture2D texture;
    private const int textureResolution = 50;
    private const int emissionMultiplier = 8192;

    public void UpdateSettings(ColorSettings colorSettings)
    {
        settings = colorSettings;

        if (texture == null)
        {
            texture = new Texture2D(textureResolution, 1);
        }
    }

    public void UpdateElevation(MinMax elevationMinMax)
    {
        settings.planetMaterial.SetVector("_ElevationMinMax", new Vector4(elevationMinMax.Min, elevationMinMax.Max, 0, 0));
    }

    public void Updatecolors()
    {
        Color[] colors = new Color[textureResolution];
        for (int i = 0; i < textureResolution; i++)
        {
            colors[i] = settings.gradient.Evaluate(i / (textureResolution - 1f));
        }
        texture.SetPixels(colors);
        texture.Apply();
        settings.planetMaterial.SetTexture("_PlanetTexture", texture);
        settings.planetMaterial.SetFloat("_EmissionStrength", (settings.emissionStrength * emissionMultiplier));
    }
}
