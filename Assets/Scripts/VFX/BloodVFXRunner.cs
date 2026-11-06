using UnityEngine;

/// <summary>
/// Drives the aging of <see cref="BloodVFX"/> ground stains. It is added to the
/// effect root only after the first stain spawns, so a run where nobody bleeds
/// pays nothing for it.
/// </summary>
public class BloodVFXRunner : MonoBehaviour
{
    private void Update()
    {
        BloodVFX.TickDecals();
    }
}
