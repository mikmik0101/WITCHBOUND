using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "WITCHBOUND/Config/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("Generator Settings")]
    public int totalGenerators = 10;
    public float generatorRepairTime = 60f;
    
    [Header("Witch Spawning")]
    public int startingWitches = 1;
    public int maxWitches = 6;
    public int[] witchSpawnThresholds = { 0, 2, 4, 6, 8, 10 };
    
    [Header("Player Settings")]
    public float playerMoveSpeed = 5f;
    public float playerSprintSpeed = 8f;
    public float playerHealth = 100f;
    
    [Header("Survivor AI")]
    public float survivorMoveSpeed = 4f;
    public float survivorRepairSpeed = 1f;
    
    [Header("Gameplay")]
    public float escapeGateOpenTime = 5f;
    public float rescueTime = 3f;
}
