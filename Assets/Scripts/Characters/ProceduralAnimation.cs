using UnityEngine;

public class ProceduralAnimation : MonoBehaviour
{
    [Header("Settings")]
    public float bobSpeed = 2f;
    public float bobAmount = 0.05f;
    public float swayAmount = 0.02f;
    public float armSwingAmount = 0.3f;
    public float legSwingAmount = 0.4f;
    
    [Header("State")]
    public bool isWalking = false;
    public bool isRunning = false;
    public bool isAttacking = false;
    
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform[] arms;
    private Transform[] legs;
    private Transform head;
    private Transform torso;
    
    private float animTime = 0f;
    
    private void Start()
    {
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
        
        // Find body parts
        head = transform.Find("Head");
        torso = transform.Find("Torso");
        arms = new Transform[] { transform.Find("LeftArm"), transform.Find("RightArm") };
        legs = new Transform[] { transform.Find("LeftLeg"), transform.Find("RightLeg") };
    }
    
    private void Update()
    {
        animTime += Time.deltaTime;
        
        float speed = isRunning ? 2f : (isWalking ? 1f : 0f);
        float frequency = isRunning ? 8f : (isWalking ? 4f : 0f);
        
        // Bob
        float bob = Mathf.Sin(animTime * frequency) * bobAmount * speed;
        transform.localPosition = originalPosition + new Vector3(0, bob, 0);
        
        // Sway
        float sway = Mathf.Sin(animTime * frequency * 0.5f) * swayAmount * speed;
        transform.localRotation = originalRotation * Quaternion.Euler(0, sway, 0);
        
        // Arm swing
        if (arms != null && arms.Length >= 2)
        {
            if (arms[0] != null)
                arms[0].localRotation = Quaternion.Euler(Mathf.Sin(animTime * frequency) * armSwingAmount * 30f * speed, 0, 0);
            if (arms[1] != null)
                arms[1].localRotation = Quaternion.Euler(-Mathf.Sin(animTime * frequency) * armSwingAmount * 30f * speed, 0, 0);
        }
        
        // Leg swing
        if (legs != null && legs.Length >= 2)
        {
            if (legs[0] != null)
                legs[0].localRotation = Quaternion.Euler(-Mathf.Sin(animTime * frequency) * legSwingAmount * 40f * speed, 0, 0);
            if (legs[1] != null)
                legs[1].localRotation = Quaternion.Euler(Mathf.Sin(animTime * frequency) * legSwingAmount * 40f * speed, 0, 0);
        }
        
        // Idle breathing when not moving
        if (!isWalking && !isRunning)
        {
            float breathe = Mathf.Sin(animTime * 1.5f) * 0.01f;
            if (torso != null)
                torso.localScale = new Vector3(torso.localScale.x, torso.localScale.y + breathe, torso.localScale.z);
        }
    }
    
    public void SetWalking(bool walking)
    {
        isWalking = walking;
    }
    
    public void SetRunning(bool running)
    {
        isRunning = running;
    }
    
    public void SetAttacking(bool attacking)
    {
        isAttacking = attacking;
    }
}
