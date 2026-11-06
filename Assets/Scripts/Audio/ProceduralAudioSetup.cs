using UnityEngine;

public class ProceduralAudioSetup : MonoBehaviour
{
    [Header("Auto-setup")]
    public bool setupOnStart = true;
    
    private void Awake()
    {
        if (setupOnStart)
        {
            SetupAudio();
        }
    }
    
    [ContextMenu("Setup Audio")]
    public void SetupAudio()
    {
        var audioManager = AudioManager.Instance;
        if (audioManager == null) return;
        
        // Generate and assign audio clips
        audioManager.ambientTracks = new AudioClip[] { SynthesizedAudioGenerator.GenerateWind(5f) };
        audioManager.ambientLoops = new AudioClip[] { SynthesizedAudioGenerator.GenerateElectricalHum(3f) };
        audioManager.chaseMusic = SynthesizedAudioGenerator.GenerateTone(220f, 2f, 0.3f, 1);
        audioManager.escapeMusic = SynthesizedAudioGenerator.GenerateTone(440f, 2f, 0.3f, 0);
        
        // Register SFX
        audioManager.RegisterSFX("footstep", SynthesizedAudioGenerator.GenerateFootstep(1f));
        audioManager.RegisterSFX("sprint", SynthesizedAudioGenerator.GenerateFootstep(1.5f));
        audioManager.RegisterSFX("damage", SynthesizedAudioGenerator.GenerateDamage());
        audioManager.RegisterSFX("heartbeat", SynthesizedAudioGenerator.GenerateHeartbeat());
        audioManager.RegisterSFX("witchProximity", SynthesizedAudioGenerator.GenerateWitchProximity(200f));
        audioManager.RegisterSFX("skillPhase", SynthesizedAudioGenerator.GenerateSkillActivation(1f));
        audioManager.RegisterSFX("skillInvis", SynthesizedAudioGenerator.GenerateSkillActivation(1.2f));
        audioManager.RegisterSFX("skillSpeed", SynthesizedAudioGenerator.GenerateSkillActivation(1.5f));
        audioManager.RegisterSFX("skillWeapon", SynthesizedAudioGenerator.GenerateSkillActivation(0.8f));
        audioManager.RegisterSFX("generatorComplete", SynthesizedAudioGenerator.GenerateCompletion());
        audioManager.RegisterSFX("victory", SynthesizedAudioGenerator.GenerateVictory());
        audioManager.RegisterSFX("defeat", SynthesizedAudioGenerator.GenerateDefeat());
        
        Debug.Log("Procedural audio setup complete!");
    }
}
