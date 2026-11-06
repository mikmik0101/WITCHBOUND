using UnityEngine;
using System.Collections;

public static class SynthesizedAudioGenerator
{
    private const int SampleRate = 22050;
    
    // Generate a simple tone
    public static AudioClip GenerateTone(float frequency, float duration, float volume = 0.5f, int waveType = 0)
    {
        int samples = (int)(SampleRate * duration);
        float[] data = new float[samples];
        
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SampleRate;
            float envelope = Mathf.Exp(-t * 3f); // Decay
            float value = 0f;
            
            switch (waveType)
            {
                case 0: // Sine
                    value = Mathf.Sin(2f * Mathf.PI * frequency * t);
                    break;
                case 1: // Square
                    value = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * frequency * t));
                    break;
                case 2: // Sawtooth
                    value = 2f * (t * frequency - Mathf.Floor(t * frequency + 0.5f));
                    break;
                case 3: // Noise
                    value = Random.Range(-1f, 1f);
                    break;
            }
            
            data[i] = value * volume * envelope;
        }
        
        var clip = AudioClip.Create("SynthTone", samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
    
    // Generate footsteps
    public static AudioClip GenerateFootstep(float pitch = 1f)
    {
        int samples = (int)(SampleRate * 0.15f);
        float[] data = new float[samples];
        
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SampleRate;
            float envelope = Mathf.Exp(-t * 20f);
            float value = Random.Range(-1f, 1f) * envelope * 0.3f;
            value += Mathf.Sin(2f * Mathf.PI * 80f * pitch * t) * envelope * 0.2f;
            data[i] = value;
        }
        
        var clip = AudioClip.Create("Footstep", samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
    
    // Generate wind
    public static AudioClip GenerateWind(float duration = 5f)
    {
        int samples = (int)(SampleRate * duration);
        float[] data = new float[samples];
        
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SampleRate;
            float value = Random.Range(-0.3f, 0.3f);
            value += Mathf.Sin(2f * Mathf.PI * 0.5f * t) * 0.1f;
            value += Mathf.Sin(2f * Mathf.PI * 0.2f * t) * 0.05f;
            data[i] = value;
        }
        
        var clip = AudioClip.Create("Wind", samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
    
    // Generate electrical hum
    public static AudioClip GenerateElectricalHum(float duration = 3f)
    {
        int samples = (int)(SampleRate * duration);
        float[] data = new float[samples];
        
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SampleRate;
            float value = Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.15f;
            value += Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.08f;
            value += Random.Range(-0.02f, 0.02f);
            data[i] = value;
        }
        
        var clip = AudioClip.Create("ElectricalHum", samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
    
    // Generate heartbeat
    public static AudioClip GenerateHeartbeat()
    {
        int samples = (int)(SampleRate * 1f);
        float[] data = new float[samples];
        
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SampleRate;
            float value = 0f;
            
            // Two beats per second
            float beat1 = Mathf.Exp(-Mathf.Abs(t - 0.2f) * 30f);
            float beat2 = Mathf.Exp(-Mathf.Abs(t - 0.6f) * 30f);
            
            value = (beat1 + beat2) * 0.3f;
            value *= Mathf.Sin(2f * Mathf.PI * 50f * t);
            
            data[i] = value;
        }
        
        var clip = AudioClip.Create("Heartbeat", samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
    
    // Generate witch proximity sound
    public static AudioClip GenerateWitchProximity(float frequency = 200f)
    {
        int samples = (int)(SampleRate * 2f);
        float[] data = new float[samples];
        
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SampleRate;
            float envelope = 1f;
            float value = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.2f;
            value += Mathf.Sin(2f * Mathf.PI * (frequency * 1.5f) * t) * 0.1f;
            value += Random.Range(-0.05f, 0.05f);
            data[i] = value * envelope;
        }
        
        var clip = AudioClip.Create("WitchProximity", samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
    
    // Generate damage sound
    public static AudioClip GenerateDamage()
    {
        int samples = (int)(SampleRate * 0.3f);
        float[] data = new float[samples];
        
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SampleRate;
            float envelope = Mathf.Exp(-t * 10f);
            float value = Random.Range(-1f, 1f) * envelope * 0.5f;
            value += Mathf.Sin(2f * Mathf.PI * 150f * t) * envelope * 0.3f;
            data[i] = value;
        }
        
        var clip = AudioClip.Create("Damage", samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
    
    // Generate skill activation
    public static AudioClip GenerateSkillActivation(float pitch = 1f)
    {
        int samples = (int)(SampleRate * 0.5f);
        float[] data = new float[samples];
        
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SampleRate;
            float envelope = Mathf.Exp(-t * 5f);
            float value = Mathf.Sin(2f * Mathf.PI * 440f * pitch * t) * envelope * 0.4f;
            value += Mathf.Sin(2f * Mathf.PI * 880f * pitch * t) * envelope * 0.2f;
            data[i] = value;
        }
        
        var clip = AudioClip.Create("SkillActivation", samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
    
    // Generate completion sound
    public static AudioClip GenerateCompletion()
    {
        int samples = (int)(SampleRate * 1f);
        float[] data = new float[samples];
        
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SampleRate;
            float envelope = Mathf.Exp(-t * 3f);
            float freq = 440f + t * 220f; // Rising pitch
            float value = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.4f;
            value += Mathf.Sin(2f * Mathf.PI * (freq * 1.5f) * t) * envelope * 0.2f;
            data[i] = value;
        }
        
        var clip = AudioClip.Create("Completion", samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
    
    // Generate victory sound
    public static AudioClip GenerateVictory()
    {
        int samples = (int)(SampleRate * 2f);
        float[] data = new float[samples];
        
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SampleRate;
            float envelope = Mathf.Exp(-t * 1.5f);
            float freq = 523.25f; // C5
            if (t > 0.5f) freq = 659.25f; // E5
            if (t > 1f) freq = 783.99f; // G5
            float value = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.4f;
            data[i] = value;
        }
        
        var clip = AudioClip.Create("Victory", samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
    
    // Generate defeat sound
    public static AudioClip GenerateDefeat()
    {
        int samples = (int)(SampleRate * 2f);
        float[] data = new float[samples];
        
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SampleRate;
            float envelope = Mathf.Exp(-t * 1.5f);
            float freq = 392f; // G4
            if (t > 0.5f) freq = 349.23f; // F4
            if (t > 1f) freq = 293.66f; // D4
            float value = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.4f;
            data[i] = value;
        }
        
        var clip = AudioClip.Create("Defeat", samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
