using UnityEngine;

[CreateAssetMenu(fileName = "GeneratorData", menuName = "WITCHBOUND/Data/Generator Data")]
public class GeneratorData : ScriptableObject
{
    public string generatorId = "Gen_01";
    public float repairTime = 60f;
    public AudioClip idleSound;
    public AudioClip repairingSound;
    public AudioClip completedSound;
    public AudioClip interruptedSound;
    public GameObject vfxPrefab;
}
