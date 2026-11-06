using UnityEngine;

/// <summary>
/// Pulses the generator's distance beacon and disables it when repaired.
/// Must live in its own file (name must match the class) so Unity can create a
/// valid MonoScript for it - otherwise saving it into a scene corrupts the level.
/// </summary>
public class GeneratorBeacon : MonoBehaviour
{
    public Renderer orb;
    public Light beaconLight;
    public bool active = true;

    private Material _orbMat;
    private float _base = 1.4f;

    private void Start()
    {
        if (orb != null) _orbMat = orb.sharedMaterial;
    }

    private void Update()
    {
        if (!active || beaconLight == null) return;
        float pulse = 0.55f + 0.45f * Mathf.Sin(Time.time * 2.4f);
        beaconLight.intensity = _base * pulse;
        if (_orbMat != null && _orbMat.HasProperty("_Color"))
            _orbMat.color = new Color(1f, 0.55f, 0.10f) * (0.6f + 0.6f * pulse);
    }

    public void Stop()
    {
        active = false;
        if (beaconLight != null) beaconLight.enabled = false;
        if (orb != null) orb.enabled = false;
    }
}
