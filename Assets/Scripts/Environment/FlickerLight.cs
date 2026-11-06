using UnityEngine;

/// <summary>
/// Cheap, seeded light flicker. Uses two Perlin channels so the intensity
/// wavers organically and occasionally drops out, without any allocation or
/// coroutines per frame.
/// </summary>
public class FlickerLight : MonoBehaviour
{
    public Light target;
    public float baseIntensity = 1f;
    public float minIntensity = 0.15f;
    public float speed = 6f;
    public int seed = 7;
    public bool hardDropouts = true;

    private float timeOffset;
    private float noiseSeedA;
    private float noiseSeedB;

    private void Start()
    {
        timeOffset = (seed % 997) * 0.137f;
        noiseSeedA = (seed % 113) * 3.71f;
        noiseSeedB = (seed % 79) * 7.19f + 11.3f;

        if (target == null) target = GetComponent<Light>();
    }

    private void Update()
    {
        if (target == null) return;

        float t = Time.time * (0.15f + speed * 0.12f) + timeOffset;
        float nA = Mathf.PerlinNoise(noiseSeedA, t);
        float nB = Mathf.PerlinNoise(noiseSeedB, t * 2.7f);
        float v = Mathf.Lerp(minIntensity, baseIntensity, Mathf.Clamp01(nA * nB * 1.9f));

        if (hardDropouts && nB < 0.28f)
            v *= 0.25f;

        target.intensity = v;
    }
}
