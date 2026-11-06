using UnityEngine;

[CreateAssetMenu(fileName = "SurvivorData", menuName = "WITCHBOUND/Data/Survivor Data")]
public class SurvivorData : ScriptableObject
{
    public string survivorName = "Survivor";
    [TextArea] public string description = "";
    public GameObject prefab;
    public Sprite portrait;
    public float baseHealth = 100f;
    public float moveSpeed = 5f;
    public SkillData startingSkill;
    public float courage = 0.5f;
    public float repairPriority = 0.5f;
    public float rescuePriority = 0.5f;
    public AudioClip[] voiceClips;
}
