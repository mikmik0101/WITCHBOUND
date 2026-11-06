using UnityEngine;

[CreateAssetMenu(fileName = "WitchData", menuName = "WITCHBOUND/Data/Witch Data")]
public class WitchData : ScriptableObject
{
    public string witchName = "Witch";
    [TextArea] public string description = "";
    public GameObject prefab;
    public Texture2D faceTexture;
    public float moveSpeed = 3f;
    public float chaseSpeed = 7f;
    public float detectionRange = 20f;
    public float fieldOfView = 90f;
    public float hearingRange = 15f;
    public float attackDamage = 25f;
    public float attackCooldown = 2f;
    public WitchType witchType = WitchType.Humanoid;
    public AudioClip[] audioClips;
    public GameObject vfxPrefab;
    public int spawnThreshold = 0;
}

public enum WitchType
{
    Humanoid,
    Winged,
    Hound,
    Crawler,
    Tall,
    Final
}
