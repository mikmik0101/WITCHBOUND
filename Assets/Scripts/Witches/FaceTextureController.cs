using UnityEngine;

public class FaceTextureController : MonoBehaviour
{
    [Header("References")]
    public Renderer faceRenderer;
    public Material faceMaterial;
    
    private Material faceMaterialInstance;
    private Texture2D currentFaceTexture;
    
    private void Awake()
    {
        if (faceRenderer == null)
            faceRenderer = GetComponentInChildren<Renderer>();
        
        if (faceMaterial != null && faceRenderer != null)
        {
            faceMaterialInstance = new Material(faceMaterial);
            faceRenderer.material = faceMaterialInstance;
        }
    }
    
    public void SetFaceTexture(Texture2D faceTexture)
    {
        if (faceTexture == null)
        {
            Debug.LogWarning("FaceTextureController: Attempted to set null face texture");
            return;
        }
        
        currentFaceTexture = faceTexture;
        
        if (faceMaterialInstance != null)
        {
            faceMaterialInstance.SetTexture("_MainTex", faceTexture);
            faceMaterialInstance.SetTexture("_BaseMap", faceTexture); // URP
        }
        else if (faceRenderer != null && faceRenderer.material != null)
        {
            faceRenderer.material.SetTexture("_MainTex", faceTexture);
            faceRenderer.material.SetTexture("_BaseMap", faceTexture);
        }
        else
        {
            Debug.LogWarning("FaceTextureController: No valid material to apply face texture");
        }
    }
    
    public Texture2D GetCurrentFaceTexture()
    {
        return currentFaceTexture;
    }
    
    public void ResetToDefault()
    {
        if (faceMaterial != null && faceMaterialInstance != null)
        {
            Texture2D defaultTex = faceMaterial.GetTexture("_MainTex") as Texture2D;
            if (defaultTex != null)
            {
                SetFaceTexture(defaultTex);
            }
        }
    }
    
    /// <summary>
    /// Resolves a character's picture through the shared CharacterImageLibrary
    /// and applies it, so this helper can never become a second image source.
    /// </summary>
    public static void ApplyFaceForName(GameObject witchObject, string displayName, bool isWitch)
    {
        if (witchObject == null || string.IsNullOrEmpty(displayName)) return;

        WitchType type = isWitch ? RuntimeCharacterFactory.WitchTypeFromName(displayName) : WitchType.Humanoid;
        var image = CharacterImageLibrary.Get(null, displayName, type);
        ApplyFaceTexture(witchObject, image.texture);
        CharacterImageLibrary.LogApplied("face controller", displayName);
    }

    // Static utility for easy application
    public static void ApplyFaceTexture(GameObject witchObject, Texture2D faceTexture)
    {
        var controller = witchObject.GetComponent<FaceTextureController>();
        if (controller == null)
            controller = witchObject.AddComponent<FaceTextureController>();
        
        controller.SetFaceTexture(faceTexture);
    }
}
